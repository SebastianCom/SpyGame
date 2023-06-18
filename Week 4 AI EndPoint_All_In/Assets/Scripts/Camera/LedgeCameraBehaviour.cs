using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LedgeCameraBehaviour : CameraBehaviour
{
    public float CameraPosEaseSpeed = 3.0f;
    public float LookPosEaseSpeed = 5.0f;

    public Vector3 PlayerLocalPivotPos = Vector3.zero;
    public Vector3 PlayerLocalLookPos = Vector3.zero;
    public float MaxDistFromPivot = 10.0f;

    public LedgeCameraBehaviour()
    {
        ObstacleCheckRadius = 0.3f;
    }
    public override void Activate()
    {
        base.Activate();

        if(m_Player != null)
        {
            m_GoalPos = m_Camera.transform.position;
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();

        if (m_Player != null)
        {
            m_GoalPos = m_Camera.transform.position;
        }
    }

    public override void UpdateCamera()
    {
        //Remember the last position the player was on ground
        if (m_Player.OnGround)
        {
            m_LastOnGroundPlayerY = m_Player.transform.position.y;
        }

        //Update position
        Vector3 worldPivotPos = m_Player.transform.TransformPoint(PlayerLocalPivotPos);
        worldPivotPos.y = m_LastOnGroundPlayerY + PlayerLocalPivotPos.y;

            //Convert back to normal coords
        Vector3 offsetFromPlayer = m_Camera.LedgeDir * MaxDistFromPivot;

        m_GoalPos = offsetFromPlayer + worldPivotPos;

            //Ease the camera pos towards the goal.  We will use Slerp easing horizontally so that the 
            //ease will rotate around the player properly, and the vertical direction will use a lerp ease.
        Vector3 newCameraPos = m_Camera.transform.position;

        newCameraPos = MathUtils.SlerpToHoriz(
            CameraPosEaseSpeed,
            newCameraPos,
            m_GoalPos,
            worldPivotPos,
            Time.deltaTime
            );

        newCameraPos.y = MathUtils.LerpTo(
            CameraPosEaseSpeed,
            newCameraPos.y,
            m_GoalPos.y,
            Time.deltaTime
            );

        m_Camera.transform.position = newCameraPos;

        //Deal with obstacles
        HandleObstacles();

        //Update Look Position
        Vector3 goalLookPos = m_Player.transform.TransformPoint(PlayerLocalLookPos);
        goalLookPos.y = PlayerLocalLookPos.y + m_LastOnGroundPlayerY;

        m_Camera.LookPos = MathUtils.LerpTo(
            LookPosEaseSpeed,
            m_Camera.LookPos,
            goalLookPos,
            Time.deltaTime
            );

        Vector3 lookDir = m_Camera.LookPos - m_Camera.transform.position;
        m_Camera.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    public override void UpdateRotation(float yawAmount, float pitchAmount)
    {
    }


    public override void SetFacingDirection(Vector3 direction)
    {
    }

    public override Vector3 GetControlRotation()
    {
        //This is important since the camera angle doesn't always match the ledge.  Here we 
        //are fixing the control rotation so that pressing right or left will always move 
        //along the edge. 
        return Quaternion.LookRotation(-m_Camera.LedgeDir).eulerAngles;
    }
    public override bool UsesStandardControlRotation()
    {
        return false;
    }

    Vector3 m_GoalPos;
    float m_LastOnGroundPlayerY;
   
}
