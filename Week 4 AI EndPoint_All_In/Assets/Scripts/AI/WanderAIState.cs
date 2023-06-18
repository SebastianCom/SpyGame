using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

class WanderAIState : AIState
{


    public WanderAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        m_WanderBounds = GameObject.Find("AIWonderBounds").GetComponent<Collider>().bounds;

        AIController.Target = null;

        AIController.NavAgent.stoppingDistance = AIController.ArriveAtDestinationDist;

        bHeadingToB = true;

        ChooseNewDirection();
    }

    public override void Deactivate()
    {
        AIController.NavAgent.updateRotation = false;
    }

    public override void Update()
    {
        //Jump if needed
        if (AIController.ShouldAIJump())
        {
            AIController.SetState(new JumpAIState(Owner, AIController, GetType()));

            return;
        }

        //Replace this with set target when in range 
        //Find target if there isn't one already
        if (AIController.Target == null)
        {
            //Search for objects in the radius that:
            //  -have the player tag
            //  -are not this AI's player
            //  -are visible to the AI
            //AIController.Target = AIUtils.FindClosestObjectInRadius(
            //    Owner.transform.position,
            //    AIController.MaxSightRange,
            //    (obj) => (obj.tag == "Player" && obj != Owner.gameObject && AIController.CanSeeObject(obj))
            //    );

            if(isPointInTheCone(GameObject.Find("Konstantin").transform.position))
            {
                AIController.Target = GameObject.Find("Konstantin");
                //Debug.Log(AIController.Target.name + " has been found");

                if(AIController.CanSeeObject(AIController.Target))
                {
                    Debug.Log("Konstantin has been found");

                    Camera.main.tag = "Untagged";
                    //Camera.main.enabled = false;
                    AIController.AiCam.gameObject.SetActive(true);
                    AIController.AiCam.tag = "MainCamera";
                    AIController.NavAgent.SetDestination(Owner.transform.position);
                    AIController.AimPosition = GameObject.Find("Konstantin").transform.position;
                    AIController.bStartDeath = true;
                    AIController.bStopPatroling = true;
                    //Change to timer that does this after camera switch
                    //Null out target when thats done
                    //GameObject.Find("Konstantin").transform.position = GameObject.Find("Konstantin").GetComponent<Player>().StartPosition;
                }
                else
                {
                    AIController.Target = null;
                }

            }
            if (bHeadingToB && AIController.bStopPatroling == false) 
            {
                if(Vector3.Distance(Owner.transform.position, AIController.PatrolPointB.transform.position) > 5.0f)
                {
                    AIController.NavAgent.SetDestination(AIController.PatrolPointB.transform.position);
                    AIController.AimPosition = AIController.PatrolPointB.transform.position;
                }
                else
                    bHeadingToB= false;
            }
            else if (!bHeadingToB && AIController.bStopPatroling == false)
            {
                if (Vector3.Distance(Owner.transform.position, AIController.PatrolPointA.transform.position) > 5.0f)
                {
                    AIController.NavAgent.SetDestination(AIController.PatrolPointA.transform.position);
                    AIController.AimPosition = AIController.PatrolPointA.transform.position;
                }
                else
                    bHeadingToB = true;
            }

        }



        //If you have a valid target move into attack range.
        if (AIController.Target != null)
        {
            AIController.SetState(new MoveToAttackRangeAIState(Owner, AIController));

            return;
        }

        bool recomputePath = AIController.NavAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid &&
            !AIController.NavAgent.pathPending;


        //Remove this line to get the AI to face the direction of the patrol point
        //AIController.AimPosition = AIController.NavAgent.desiredVelocity.normalized * 10.0f;

        //Continue in the same direction for a bit of time then choose a new direction
        if (m_TimeLeftTillChangeDirection > 0.0f || recomputePath)
        {
            m_TimeLeftTillChangeDirection -= Time.deltaTime;
        }
        else
        {
            ChooseNewDirection();
        }

    }

    bool isPointInTheCone(Vector3 thisPoint)
    {

        //this point = player 

        float offsetAngle = 45.0f;
        float offsetAngleInRadians = offsetAngle * (3.14f / 180f);

        Vector3 cameraDirection = Owner.transform.forward;
        Vector3 dir1 = thisPoint - Owner.transform.position;

        float Dot = Vector3.Dot(Vector3.Normalize(cameraDirection), Vector3.Normalize(dir1));
        float angleMade = Mathf.Acos(Dot);
        return angleMade < offsetAngleInRadians && Vector3.Distance(Owner.transform.position, thisPoint) <= 8.0f;
    }

    public override string GetName()
    {
        return "Wander State";
    }

    private void ChooseNewDirection()
    {
        //Choose a random position to go to
        Vector3 destination = new Vector3(
            UnityEngine.Random.Range(m_WanderBounds.min.x, m_WanderBounds.max.x),
            Owner.transform.position.y,
            UnityEngine.Random.Range(m_WanderBounds.min.z, m_WanderBounds.max.z)
            );

        //Set navigation destination
        AIController.NavAgent.SetDestination(destination);

        //update timer
        m_TimeLeftTillChangeDirection = UnityEngine.Random.Range(
            AIController.MinTimeToChangeDirection,
            AIController.MaxTimeToChangeDirection
            );
    }

    float m_TimeLeftTillChangeDirection;
    bool bHeadingToB;

  

    Bounds m_WanderBounds;
}
