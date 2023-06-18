using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

class JumpAIState : AIState
{
    public JumpAIState(Player owningCharacter, AIPlayerController aiController, Type returnStateType)
        : base(owningCharacter, aiController)
    {
        m_ReturnStateType = returnStateType;
    }

    public override void Activate()
    {
        //Set up nav mesh agent
        AIController.NavAgent.stoppingDistance = AIController.ArriveAtDestinationDist;

        //Set debug time scale if needed
        if (AIController.DebugJumpTimeScale != 1.0f)
        {
            Time.timeScale = AIController.DebugJumpTimeScale;
        }

        //Disable movement on the player so that it doesn't conflict with what we're doing
        Owner.SetEnableMovement(false);

        //Get the jump start time
        m_JumpStartTime = Time.time;

        //Calculate jump info

        //Use this if you need to follow the off mesh link exactly
        //m_JumpStartPos = AIController.NavMeshAgent.currentOffMeshLinkData.startPos;
        //m_JumpStartPos.y += Owner.CenterHeightOffGround;

        //Starting the jump at the actual player position will eliminate a position snap at the start of the jump
        m_JumpStartPos = Owner.transform.position;

        m_JumpEndPos = AIController.NavAgent.currentOffMeshLinkData.endPos;
        m_JumpEndPos.y += Owner.CenterHeightOffGround;

        /*  Calculate the Jump duration and speeds needed for this jump.  These values will be used to calculate the 
            position of the character over time during the jump.
            
            We can calculate realistic looking values for these using the current gravity with these formulas:

                1)  HorizDist = StartHorizSpeed * t

                2)  Height = 0.5 * g * t^2 + StartVertSpeed * t
        
        
            We know these values:
                HorizDist    
                Height
                g

            We want to calculate:
                StartHorizSpeed
                StartVertSpeed
                t
                
            We can simplify our calculations by chosing a constant jump angle.  Jumping at 45 degrees will give the 
            farthest jump distance for a given jump speed.  Using a 45 degree angle is also nice because the horizontal 
            and vertical speeds will be the same.  (StartHorizSpeed = StartVertSpeed)

            
            We will calculate the jump time first.  To calculate the jump time we will rearrange formula 1:

                1.1) StartHorizSpeed = HorizDist / t

            
            Since StartHorizSpeed and StartVertSpeed are equal when jumping at a 45 degree angle we can substitute 
            formula 1.1 into forumla 2:

                3) Height = 0.5 * g * t^2 + (HorizDist / t) * t

            
            If we rearrange formula 3 for time we will get:

                t = (+/-)sqrt(2 * (Height - HorizDist) / g)


            Now that we now the time we can calculate the speeds by pluging t into formula 1.1 and we will have all
            of the values we need.
                    

            (Note you can use calculus to derrive formulas 1 and 2 or just look them up online:  
            https://en.wikipedia.org/wiki/Equations_of_motion)
        */

        //Calculate the jump distances
        Vector3 horizJumpOffset = m_JumpEndPos - m_JumpStartPos;
        horizJumpOffset.y = 0.0f;

        float horizJumpDist = horizJumpOffset.magnitude;

        float vertJumpDist = m_JumpEndPos.y - m_JumpStartPos.y;

        //Calculate the jump time
        //Depending on our values it's possible that the inside of the sqrt will be negative.  This means that the jump
        //is physically impossible. 
        float timeSqrd = 2.0f * (vertJumpDist - horizJumpDist) / Owner.GravityAccel;

        if (timeSqrd >= 0.0f)
        {
            m_JumpDuration = Mathf.Sqrt(timeSqrd);
        }
        else
        {
            //The jump is impossible in this case using our values.  We will just set them to default values, so that
            //the navigation won't break, but the jump won't look realistic.
            const float jumpErrorDefaultDuration = 1.0f;
            m_JumpDuration = jumpErrorDefaultDuration;
        }

        //Calculate the jump speeds
        m_HorizJumpSpeed = horizJumpDist / m_JumpDuration;

        m_VertJumpSpeed = m_HorizJumpSpeed;

        //Activate jump animation
        Owner.Animator.SetTrigger("JumpActivated");
    }

    public override void Deactivate()
    {
        Owner.SetEnableMovement(true);
        
        if (AIController.DebugJumpTimeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
        }

        //Calculate the ending velocity and set it 
        Vector3 jumpDir = m_JumpEndPos - m_JumpStartPos;
        jumpDir.y = 0.0f;
        jumpDir.Normalize();

        Vector3 landingVelocity = jumpDir * m_HorizJumpSpeed;
        landingVelocity.y = -m_VertJumpSpeed;

        Owner.Velocity = landingVelocity;

        //Clear jump animation trigger
        Owner.Animator.ResetTrigger("JumpActivated");
    }

    public override void Update()
    {
        //Calculate the jump end time
        float endTime = m_JumpStartTime + m_JumpDuration;

        //Return to the previous state if the jump is done
        if (Time.time >= endTime)
        {
            ReturnToPrevState();

            return;
        }
        
        //Draw a debug line showing the jump end points
        Debug.DrawLine(m_JumpStartPos, m_JumpEndPos, Color.magenta);

        //Calculate the current time in the jump
        float jumpTime = Time.time - m_JumpStartTime;

        //Use the time to calculate the current percentage of the jump
        float jumpPercent = jumpTime / m_JumpDuration;

        //Calcluate the positon of the agent using linear interpolation
        Vector3 newPos = Owner.transform.position;

        newPos = Vector3.Lerp(
            m_JumpStartPos,
            m_JumpEndPos,
            jumpPercent
            );

        //Calculate the height of the agent using formula 2 shown in the earlier comments
        float vertJumpHeight = 0.5f * Owner.GravityAccel * jumpTime * jumpTime + m_VertJumpSpeed * jumpTime;

        //Calculate and set the final position
        newPos.y = vertJumpHeight + m_JumpStartPos.y;

        Owner.transform.position = newPos;
    }

    public override Vector3 GetMoveInput()
    {
        return Vector3.zero;
    }

    public override string GetName()
    {
        return "Jump State";
    }

    public override void GetDebugOutput(StringBuilder debugOutput)
    {
        debugOutput.AppendLine("Off mesh link info:");
        debugOutput.AppendFormat("   On off mesh link:  {0}\n", AIController.NavAgent.isOnOffMeshLink);

        debugOutput.AppendFormat("   link type: {0}\n", AIController.NavAgent.currentOffMeshLinkData.linkType.ToString());


        debugOutput.AppendFormat("\nJump info:  Duration: {0}\n", m_JumpDuration);
    }

    void ReturnToPrevState()
    {
        //Notify the Nav system that you are done following the nav mesh link.  
        //The navigation will break if you miss this step
        AIController.NavAgent.CompleteOffMeshLink();

        //Return to the previous state before jumping
        if (m_ReturnStateType == typeof(FollowTargetAIState))
        {
            AIController.SetState(new FollowTargetAIState(Owner, AIController));
        }
        else if (m_ReturnStateType == typeof(MoveToAttackRangeAIState))
        {
            AIController.SetState(new MoveToAttackRangeAIState(Owner, AIController));
        }
        else if (m_ReturnStateType == typeof(MoveToLastTargetPosAIState))
        {
            AIController.SetState(new MoveToLastTargetPosAIState(Owner, AIController));
        }
        else if (m_ReturnStateType == typeof(WanderAIState))
        {
            AIController.SetState(new WanderAIState(Owner, AIController));
        }
        else if (m_ReturnStateType == typeof(AttackAIState))
        {
            AIController.SetState(new AttackAIState(Owner, AIController));
        }
        else
        {
            DebugUtils.LogError("Unhandled jump return state type: {0}", m_ReturnStateType);
        }
    }

    Vector3 m_JumpStartPos;
    Vector3 m_JumpEndPos;

    float m_JumpStartTime;
    float m_JumpDuration;

    float m_HorizJumpSpeed;
    float m_VertJumpSpeed;

    Type m_ReturnStateType;
}
