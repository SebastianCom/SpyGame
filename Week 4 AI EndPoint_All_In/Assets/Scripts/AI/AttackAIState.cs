
using UnityEngine;

class AttackAIState : AIState
{
    public AttackAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        AIController.NavAgent.updateRotation = false;
    }

    public override void Deactivate()
    {
        AIController.NavAgent.updateRotation = true;

        AIController.UseItem = false;
    }

    public override void Update()
    {
        //Jump if needed
        if (AIController.ShouldAIJump())
        {
            AIController.SetState(new JumpAIState(Owner, AIController, GetType()));
        }

        //If you don't have a target, wander
        else if (AIController.Target == null)
        {
            AIController.SetState(new WanderAIState(Owner, AIController));

            return;
        }

        //If you can't see the object, go to last seen position
        else if (!AIController.CanSeeObject(AIController.Target))
        {
            AIController.SetState(new MoveToLastTargetPosAIState(Owner, AIController));

            return;
        }

  
        AIController.UpdateLastSeenTargetPos();

        //If you are close enough, attack.  Otherwise get closer.
        float distFromTargetSqrd = (AIController.Target.transform.position - Owner.transform.position).sqrMagnitude;

        if (distFromTargetSqrd <= AIController.MaxAttackRange * AIController.MaxAttackRange)
        {
            AIController.UseItem = true;
        }
        else
        {
            AIController.SetState(new MoveToAttackRangeAIState(Owner, AIController));
        }

        //Aim towards target
        UpdateAimDirection();


        if (AIController.bStartDeath && AIController.DeathTimer > 0.0f)
        {
            AIController.DeathTimer -= Time.deltaTime;
            GameObject.Find("Konstantin").GetComponent<AudioSource>().Play();
        }
        else if (AIController.bStartDeath && AIController.DeathTimer <= 0.0f)
        {
            AIController.bStartDeath = false;
            AIController.DeathTimer = 2.5f;

            AIController.AiCam.tag = "Untagged";
            GameObject.Find("Konstantin").GetComponent<Player>().PlayerCam.tag = "MainCamera";
            AIController.AiCam.gameObject.SetActive(false);
            GameObject.Find("Konstantin").transform.position = GameObject.Find("Konstantin").GetComponent<Player>().StartPosition;
            AIController.Target = null;

            AIController.bStopPatroling = false;

        }
    }

    public override string GetName()
    {
        return "Attack State";
    }

    private void UpdateAimDirection()
    {
        if (AIController.Target == null)
        {
            return;
        }

        AIController.AimPosition = AIController.Target.transform.position;
    }
}
