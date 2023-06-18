using System.Collections;
using UnityEngine;

public class WaterBody : MonoBehaviour
{
    public float Density = 1.0f;
    public float BuoyancyDamping = 1.0f;
	void Start ()
    {
        BoxCollider collider = GetComponent<BoxCollider>();

        WaterHeight = collider.bounds.max.y;

    }
	
	void Update ()
    {
		
	}

    void OnTriggerEnter(Collider collider)
    {
        WaterBuoyancyHandler buoyancyHandler = collider.GetComponent<WaterBuoyancyHandler>();
        if(buoyancyHandler != null)
        {
            buoyancyHandler.WaterBody = this;
        }

    }

    void OnTriggerExit(Collider collider)
    {
        WaterBuoyancyHandler buoyancyHandler = collider.GetComponent<WaterBuoyancyHandler>();
        if (buoyancyHandler != null)
        {
            buoyancyHandler.WaterBody = null;
        }
    }
    public float WaterHeight { get; private set; }
}
