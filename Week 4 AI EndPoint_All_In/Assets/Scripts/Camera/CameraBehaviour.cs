using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is the base CameraBehaviour class.  Not every camera / camera behaviour is best suited for
//every situation in game.  By switching to different camera behaviours when needed we can custom tailor 
//the camera for each situation that makes sense
[Serializable]
public abstract class CameraBehaviour
{
    public float ObstacleCheckRadius = 0.5f;
    public Vector3 PlayerLocalObstructionMovePos = Vector3.zero;
    public CameraBehaviour()
    {

    }

    public virtual void Init(ThirdPersonCamera camera, Player player)
    {
        m_Camera = camera;
        m_Player = player;

        //Get player layer mask
        //
        //The layer masks are integers where each bit represents a layer.
        //If mask is passed into the raycast function only layers with a bit set to 1 will be hit.
        //Since we want to hit everything EXCEPT the player, we use the bitwise not operator (~) to
        //invert the bits of the mask we get back from the player.
        m_RaycastHitMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
    }

    public virtual void Activate()
    {
    }

    public virtual void Deactivate()
    {
    }

    public abstract void UpdateCamera();

    public abstract void UpdateRotation(float yawAmount, float pitchAmount);

    public abstract void SetFacingDirection(Vector3 direction);

    //The controls of the player are dependent on what the camera is doing, this lets us cusomize 
    //the controls in a limited fashion based on the current camera behaviour
    public virtual Vector3 GetControlRotation()
    {
        return m_Camera.transform.rotation.eulerAngles;
    }

    //Returns true if the controls should be based on the camera look direction
    public virtual bool UsesStandardControlRotation()
    {
        return true;
    }

    
    protected float HandleObstacles()
    {
        //Set up sphere cast
        Vector3 rayStart = m_Player.transform.TransformPoint(PlayerLocalObstructionMovePos);
        Vector3 rayEnd = m_Camera.transform.position;

        //Calculate the ray direction and return early if the ray has a length of zero
        Vector3 rayDir = rayEnd - rayStart;

        float rayDist = rayDir.magnitude;
        if (rayDist <= 0.0f)
        {
            return 0.0f;
        }

        rayDir /= rayDist;

        //Get all objects that intersect with the ray, so we can process them all.
        //Note: the objects returned by this function are not sorted.
        RaycastHit[] hitInfos = Physics.SphereCastAll(rayStart, ObstacleCheckRadius, rayDir, rayDist, m_RaycastHitMask);
        if (hitInfos.Length <= 0)
        {
            return rayDist;
        }

        //Process each obstacle
        float minMoveUpDist = float.MaxValue;
        foreach (RaycastHit hitInfo in hitInfos)
        {
            minMoveUpDist = Mathf.Min(minMoveUpDist, hitInfo.distance);
        }

        if (minMoveUpDist < float.MaxValue)
        {
            m_Camera.transform.position = rayStart + rayDir * minMoveUpDist;
        }

        //Debug.DrawLine(rayStart, rayEnd, Color.red);

        return minMoveUpDist;
    }

    protected ThirdPersonCamera m_Camera;

    protected Player m_Player;

    int m_RaycastHitMask;
}
