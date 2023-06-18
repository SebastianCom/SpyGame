using System;
using System.Collections.Generic;

class MoveToLastTargetPosAIState : AIState
{
    public float GoalDistFromDestination = 1.0f;

    public MoveToLastTargetPosAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        AIController.NavAgent.stoppingDistance = GoalDistFromDestination;

        AIController.NavAgent.SetDestination(AIController.LastSeenTargetPos);
    }

    public override void Deactivate()
    {
    }

    public override void Update()
    {
        bool validPath = AIController.NavAgent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathInvalid &&
            !AIController.NavAgent.pathPending;

        //Jump if needed
        if (AIController.ShouldAIJump())
        {
            AIController.SetState(new JumpAIState(Owner, AIController, GetType()));
        }

        //If you don't have a target, or no valid path wander
        if (AIController.Target == null || !validPath)
        {
            AIController.SetState(new WanderAIState(Owner, AIController));

            return;
        }

        //If you can see the target, move to attack range
        else if (AIController.CanSeeObject(AIController.Target))
        {
            AIController.SetState(new MoveToAttackRangeAIState(Owner, AIController));

            return;
        }

        //If you are close enough to the target, attack.  Otherwise, move towards the target.        
        if (AIController.NavAgent.remainingDistance <= GoalDistFromDestination)
        {
            AIController.SetState(new WanderAIState(Owner, AIController));
        }
    }

    public override string GetName()
    {
        return "Move to last seen target pos";
    }
}
