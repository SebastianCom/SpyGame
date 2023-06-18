using System;
using UnityEngine;

[Serializable]
public class FollowCameraBehaviour : CameraBehaviour
{
    public float CameraHorizPosEaseSpeed = 5.0f;
    public float CameraVertPosEaseSpeed = 4.0f;
    public float LookPosEaseSpeed = 5.0f;

    public Vector3 PlayerMaxDistLocalLookPos = new Vector3(0.0f, 1.66f, 0.0f);
    public Vector3 PlayerMinDistLocalLookPos = new Vector3(0.0f, 0.5f, 0.0f);
    public Vector3 PlayerLocalPivotPos = new Vector3(0.0f, 3.48f, 0.0f);
    public float YawRotateSpeed = 2.0f;
    public float PitchRotateSpeed = 1.0f;
    public float MaxVerticalAngle = 70.0f;

    public float InAirMinHeightFromPlayer = -1.0f;
    public float InAirMaxHeightFromPlayer = 6.0f;

    public float MaxDistFromPlayer = 6.0f;
    public float MinHorizDistFromPlayer = 6.0f;

    public float AutoRotateDelayTime = 1.0f;

    public FollowCameraBehaviour()
    {
        ObstacleCheckRadius = 0.3f;
    }

    public override void Activate()
    {
        base.Activate();

        m_GoalPos = m_Camera.transform.position;

        m_LastOnGroundPivotPosY = m_GoalPos.y;

        m_FollowingHeightInAir = true;

        m_AllowAutoRotate = false;
        m_TimeTillAutoRotate = AutoRotateDelayTime;
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }

    public override void UpdateRotation(float yawAmount, float pitchAmount)
    {
        m_YawInput = yawAmount;
        m_PitchInput = pitchAmount;
    }

    public override void SetFacingDirection(Vector3 direction)
    {
    }
    public override Vector3 GetControlRotation()
    {
        return base.GetControlRotation();
    }


    public override bool UsesStandardControlRotation()
    {
        return true;
    }

    public override void UpdateCamera()
    {
        //Calc pivot and distance info
        Vector3 worldPivotPos = m_Player.transform.TransformPoint(PlayerLocalPivotPos);
        Vector3 offsetFromPlayer = m_GoalPos - worldPivotPos;

        float distFromPlayer = offsetFromPlayer.magnitude;

        //If on ground remember the pivot height
        if (m_Player.OnGround)
        {
            m_LastOnGroundPivotPosY = worldPivotPos.y;
        }

        //Check if we should start updating the height.  
        float distFromOnGroundPosY = worldPivotPos.y - m_LastOnGroundPivotPosY;

        bool heightRequiresUpdate = false;
        if (m_Player.OnGround)
        {
            heightRequiresUpdate = true;
            m_FollowingHeightInAir = false;
        }
        else if (m_FollowingHeightInAir)
        {
            heightRequiresUpdate = true;
        }
        else if (distFromOnGroundPosY < InAirMinHeightFromPlayer || distFromOnGroundPosY > InAirMaxHeightFromPlayer)
        {
            heightRequiresUpdate = true;
            m_FollowingHeightInAir = true;
            
        }

        // Update Position of Camera
        {

            //Get rotation input
            Vector3 rotateAmount = new Vector3(m_PitchInput * PitchRotateSpeed, m_YawInput * YawRotateSpeed, 0.0f);

            //Don't do auto rotation if the player is manually rotating
            m_TimeTillAutoRotate -= Time.deltaTime;
            if (!MathUtils.AlmostEquals(rotateAmount.y, 0.0f))
            {
                m_AllowAutoRotate = false;
                m_TimeTillAutoRotate = AutoRotateDelayTime;
            }
            else if (m_TimeTillAutoRotate <= 0.0f)
            {
                m_AllowAutoRotate = true;
            }

            //Horizontal rotation
            Vector3 pivotRotation = m_Camera.PivotRotation;
            if (m_AllowAutoRotate)
            {
               Vector3 anglesFromPlayer = Quaternion.LookRotation(offsetFromPlayer).eulerAngles;
               pivotRotation.y = anglesFromPlayer.y;
            }
            else
            {
                pivotRotation.y += rotateAmount.y;
            }

            pivotRotation.y += m_Player.GroundAngularVelocity.y * Time.deltaTime;

            //Vertical rotation
            pivotRotation.x -= rotateAmount.x;
            pivotRotation.x = Mathf.Clamp(pivotRotation.x, -MaxVerticalAngle, MaxVerticalAngle);

            m_Camera.PivotRotation = pivotRotation;

            //Clamp the distance
            distFromPlayer = Mathf.Clamp(distFromPlayer, MinHorizDistFromPlayer, MaxDistFromPlayer);

            //Convert back to normal coords
            offsetFromPlayer = Quaternion.Euler(pivotRotation.x, pivotRotation.y, 0.0f) * Vector3.forward;
            offsetFromPlayer *= distFromPlayer;

            //Update pivot height if needed
            if (!heightRequiresUpdate)
            {
                worldPivotPos.y = m_LastOnGroundPivotPosY;
            }
            
            m_GoalPos = offsetFromPlayer + worldPivotPos;

 
            //Ease the camera pos towards the goal.  We will use Slerp easing horizontally so that the 
            //ease will rotate around the player properly, and the vertical direction will use a lerp ease.
            Vector3 newCameraPos = m_Camera.transform.position;

            newCameraPos = MathUtils.SlerpToHoriz(
                CameraHorizPosEaseSpeed,
                newCameraPos,
                m_GoalPos,
                worldPivotPos,
                Time.deltaTime
                );

            newCameraPos.y = MathUtils.LerpTo(
                CameraVertPosEaseSpeed,
                newCameraPos.y,
                m_GoalPos.y,
                Time.deltaTime
                );

            m_Camera.transform.position = newCameraPos;
        }

        //Deal with obstacles
        float moveUpDist = HandleObstacles();

        //Update Look Position
        {
            float lookPosPercent = moveUpDist / MaxDistFromPlayer;
            Vector3 localLookPos = Vector3.Lerp(PlayerMinDistLocalLookPos, PlayerMaxDistLocalLookPos, lookPosPercent);

            Vector3 goalLookPos = m_Player.transform.TransformPoint(localLookPos);

            if (!heightRequiresUpdate)
            {
                goalLookPos.y = m_Camera.LookPos.y;
            }

            m_Camera.LookPos = MathUtils.LerpTo(
                LookPosEaseSpeed,
                m_Camera.LookPos,
                goalLookPos,
                Time.deltaTime
                );

            Vector3 lookDir = m_Camera.LookPos - m_Camera.transform.position;

            
            m_Camera.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    Vector3 m_GoalPos;

    float m_LastOnGroundPivotPosY;

    bool m_FollowingHeightInAir;

    float m_YawInput;
    float m_PitchInput;

    float m_TimeTillAutoRotate;
    bool m_AllowAutoRotate;

}
