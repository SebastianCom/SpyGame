using System.Text;

class FollowTargetAIState : AIState
{
    public FollowTargetAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        AIController.NavAgent.stoppingDistance = AIController.ArriveAtDestinationDist;
    }

    public override void Deactivate()
    {
    }

    public override void Update()
    {
        //Find target if there isn't one already
        if (AIController.Target == null)
        {
            //Search for objects in the radius that:
            //  -have the player tag
            //  -are not this AI's player
            //  -are visible to the AI
            AIController.Target = AIUtils.FindClosestObjectInRadius(
                Owner.transform.position,
                AIController.MaxSightRange,
                (obj) => (obj.tag == "Player" && obj != Owner.gameObject)
                );
        }

        if (AIController.ShouldAIJump())
        {
            AIController.SetState(new JumpAIState(Owner, AIController, GetType()));
        }
        else if (AIController.Target != null)
        {
            AIController.NavAgent.SetDestination(AIController.Target.transform.position);
        }
    }

    public override string GetName()
    {
        return "Follow Target State";
    }

    public override void GetDebugOutput(StringBuilder debugOutput)
    {
        debugOutput.AppendLine("Off mesh link info:");
        debugOutput.AppendFormat("    On off mesh link:  {0}\n", AIController.NavAgent.isOnOffMeshLink);

        debugOutput.AppendFormat("   link type: {0}", AIController.NavAgent.currentOffMeshLinkData.linkType.ToString());
    }
}
