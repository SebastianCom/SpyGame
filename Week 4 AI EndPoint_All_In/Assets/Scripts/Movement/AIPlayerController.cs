using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class AIPlayerController : MonoBehaviour, PlayerController
{
    //AI Settings.  These will be shared with all of the AI States
    public float MaxAttackRange = 15.0f;

    public float MaxSightRange = 50.0f;

    public float MinTimeToChangeDirection = 1.0f;
    public float MaxTimeToChangeDirection = 5.0f;
    public float WanderNavigateDist = 15.0f;

    public float ArriveAtDestinationDist = 2.0f;

    public int PreferedWeaponIndex = 0;

    public bool DebugFollowTarget = false;
    public bool UseNavMeshAgentMovement = false;

    public float DebugJumpTimeScale = 0.5f;

    Renderer Renderer;
    public int AiNumber;

    public Camera AiCam;

    public void Init(Player owner)
    {
        Owner = owner;

		m_SightCheckLayerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        m_ItemToSwitchTo = PreferedWeaponIndex;

        //Set up nav mesh   
        NavAgent = GetComponent<NavMeshAgent>();

        Renderer = GameObject.Find("TestSurface").GetComponent<Renderer>();
        Renderer.material.shader = Shader.Find("FieldOfView");

        //We want to use the actual player's movement instead of the nav mesh movement.  This will turn
        //off the nav mesh agent automatic movement.
        if (!UseNavMeshAgentMovement)
        {
            NavAgent.updatePosition = false;
        }

        //Set start state
        if (DebugFollowTarget)
        {
            SetState(new FollowTargetAIState(owner, this));
        }
        else
        {
            SetState(new WanderAIState(owner, this));
        }
    }

    public void UpdateControls()
    {
        //Update the state if you have one
        if (m_CurrentAIState != null)
        {

            //Vector3 dir1 = Vector3.forward * 10;
            //Vector3 dir2 = transform.forward * 10;

            //float dot = Vector3.Dot(dir1.normalized, dir2.normalized);

            //float angle = (Mathf.Acos(dot) * Mathf.Rad2Deg);


            m_CurrentAIState.Update();
        }

        UnityEngine.Debug.DrawLine(transform.position, transform.position + transform.forward * 5, Color.blue);

        //Since NavMeshAgent.updatePosition is false, the AI's position will not be automatically be
        //synchronized with the internal NavMeshAgent position.  This call will update the position
        //within the NavMeshAgent
        if (!UseNavMeshAgentMovement)
        {
            NavAgent.nextPosition = transform.position;
        }

        //Update debug info
        UpdateDebugDisplay();
    }

    public void SetState(AIState state)
    {
        //Deactivate your old state
        if (m_CurrentAIState != null)
        {
            m_CurrentAIState.Deactivate();
        }

        //switch to the new state
        m_CurrentAIState = state;

        //Activate the new state
        if (m_CurrentAIState != null)
        {
            m_CurrentAIState.Activate();
        }
    }

    public bool CanSeeObject(GameObject checkObj)
    {
        Vector3 checkDir = checkObj.transform.position - Owner.transform.position;

        float checkDist = checkDir.magnitude;
        if (checkDist <= 0.0f)
        {
            return true;
        }

        //Casting a ray towards the target.  The layer mask is set up so that it won't hit players.
        //This means if the raycast hits anything there will be an obstruction between.
        return !Physics.Raycast(Owner.transform.position, checkDir, checkDist, m_SightCheckLayerMask);
    }

    public void SetFacingDirection(Vector3 direction)
    {
        Owner.transform.rotation = Quaternion.LookRotation(direction);
    }

    public void AddLedgeDir(Vector3 ledgeDir)
    {
    }

    public bool ShouldAIJump()
    {
        //Check status of off mesh links
        return NavAgent.isOnOffMeshLink &&
            NavAgent.currentOffMeshLinkData.linkType != UnityEngine.AI.OffMeshLinkType.LinkTypeManual;
    }

    public void UpdateLastSeenTargetPos()
    {
        if (Target == null)
        {
            return;
        }

        LastSeenTargetPos = Target.transform.position;
    }

    public Player Owner { get; private set; }

    public GameObject Target { get; set; }

    public Vector3 AimPosition { get; set; }

    public Vector3 LastSeenTargetPos { get; private set; }

    public bool UseItem { get; set; }

    public UnityEngine.AI.NavMeshAgent NavAgent { get; private set; }


    public GameObject PatrolPointA;
    public GameObject PatrolPointB;

    #region Input Getting Functions

    public Vector3 GetControlRotation()
    {
        Vector3 lookDirection = AimPosition - transform.position;
        lookDirection.y = 0.0f;

        if (lookDirection.sqrMagnitude > MathUtils.CompareEpsilon)
        {
            return Quaternion.LookRotation(lookDirection).eulerAngles;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public Vector3 GetMoveInput()
    {
        if (m_CurrentAIState != null)
        {
            return m_CurrentAIState.GetMoveInput();
        }
        else
        {
            return Vector3.zero;
        }
    }

    public Vector3 GetLookInput()
    {
        return Vector3.zero;
    }

    public Vector3 GetAimTarget()
    {
        return AimPosition;
    }

    public bool IsJumping()
    {
        return false;
    }

    public bool IsFiring()
    {
        return UseItem;
    }

    public bool IsAiming()
    {
        return false;
    }

    public bool ToggleCrouch()
    {
        return false;
    }

    public bool SwitchToItem1()
    {
        return HandleItemSwitch(0);
    }

    public bool SwitchToItem2()
    {
        return HandleItemSwitch(1);
    }

    public bool SwitchToItem3()
    {
        return HandleItemSwitch(2);
    }

    public bool SwitchToItem4()
    {
        return HandleItemSwitch(3);
    }

    public bool SwitchToItem5()
    {
        return HandleItemSwitch(4);
    }

    public bool SwitchToItem6()
    {
        return HandleItemSwitch(5);
    }

    public bool SwitchToItem7()
    {
        return HandleItemSwitch(6);
    }

    public bool SwitchToItem8()
    {
        return HandleItemSwitch(7);
    }

    public bool SwitchToItem9()
    {
        return HandleItemSwitch(8);
    }

    public Vector3 GetLedgeDir()
    {
        return Vector3.zero;
    }

    #endregion


    #region Private Members

    [Conditional("UNITY_EDITOR")]
    void UpdateDebugDisplay()
    {
        //Display useful vectors
        UnityEngine.Debug.DrawLine(transform.position, NavAgent.destination);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + NavAgent.desiredVelocity, Color.red);

        //Display to the AI debug GUI
        AIDebugGUI debugGUI = Camera.main.GetComponent<AIDebugGUI>();
        if (debugGUI != null)
        {
            //Ignore this if the object isn't selected.  Note that this won't work properly if there is more
            //than one ai entity selected.
            if (Selection.Contains(gameObject))
            {
                StringBuilder debugOutput = new StringBuilder();

                //Output state
                debugOutput.Append("CurrentState = ");

                if (m_CurrentAIState != null)
                {
                    debugOutput.Append(m_CurrentAIState.GetName());
                }
                else
                {
                    debugOutput.Append("null");
                }

                //Ouput other debug info
                if (m_CurrentAIState != null)
                {
                    debugOutput.AppendLine();

                    m_CurrentAIState.GetDebugOutput(debugOutput);
                }

                //Set Debug String
                debugGUI.AIDebugDisplayMsg = debugOutput.ToString();
            }
        }
    }

    bool HandleItemSwitch(int indexToCheck)
    {
        if (indexToCheck == m_ItemToSwitchTo)
        {
            m_ItemToSwitchTo = InvalidWeaponIndex;

            return true;
        }
        else
        {
            return false;
        }
    }

    const int InvalidWeaponIndex = -1;

    AIState m_CurrentAIState;

    int m_SightCheckLayerMask;

    int m_ItemToSwitchTo;

    public float DeathTimer = 2.5f;
    public bool bStartDeath = false;
    public bool bStopPatroling = false;
    #endregion
}
