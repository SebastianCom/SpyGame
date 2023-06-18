using System.Collections.Generic;
using UnityEngine;

public class WaterBuoyancyHandler : MonoBehaviour
{

    public float ApproxBoxSphereRadius = 0.5f;
    public Vector3 CenterOfMassOverride;

    public WaterBody WaterBody { get; set; }
    public float Density { get; private set; }

    public Rigidbody Body { get; private set; }


    void Start ()
    {
        //Init components
        Body = GetComponent<Rigidbody>();

        if(CenterOfMassOverride.sqrMagnitude > 0.0f)
        {
            Body.centerOfMass = CenterOfMassOverride;
        }

        //Create list of buoyancy spheres
        m_BuoyancySpheres = new List<BuoyancySphere>();

        //Make buoyancy Spheres based on colliders
        Collider[] colliderList = GetComponentsInChildren<Collider>();

        foreach(Collider collider in colliderList)
        {
            System.Type colliderType = collider.GetType();

            if(colliderType == typeof(BoxCollider))
            {
                SetupApproxBoxSpheres((BoxCollider)collider);
            }
            else
            {
                DebugUtils.LogError("Unsupported collider type: {0}", colliderType.ToString());
            }

        }

    }

    //Doing the buoyancy physics in FixedUpdate.  This makes things more stable and consistant than using Update()
    //since the time step will always be the same.
    void FixedUpdate()
    {
       if(WaterBody == null)
       {
           return;
       }

       foreach(BuoyancySphere sphere in m_BuoyancySpheres)
       {
          sphere.ApplyForce(this);
       }
    }

    //This will draw the buoyancy spheres.  This really came in handy to debug problems with setting up the spheres
    void OnDrawGizmosSelected()
    {
        if(m_BuoyancySpheres == null)
        {
            return;
        }
        foreach (BuoyancySphere sphere in m_BuoyancySpheres)
        {
            sphere.OnDrawGizmosSelected();
        }

    }
    void Update ()
    {
		
	}

    void SetupApproxBoxSpheres(BoxCollider boxCollider)
    {
        Vector3 boxDimensions = boxCollider.size;
        boxDimensions.x *= boxCollider.transform.lossyScale.x;
        boxDimensions.y *= boxCollider.transform.lossyScale.y;
        boxDimensions.z *= boxCollider.transform.lossyScale.z;

        float volume = boxDimensions.x * boxDimensions.y * boxDimensions.z;
        Density = Body.mass / volume;

        float sphereDiameter = ApproxBoxSphereRadius * 2.0f;

        int maxX = Mathf.Max((int)(boxDimensions.x / sphereDiameter), 1);
        int maxY= Mathf.Max((int)(boxDimensions.y / sphereDiameter), 1);
        int maxZ = Mathf.Max((int)(boxDimensions.z / sphereDiameter), 1);


        Vector3 localMinCorner = boxCollider.center - 0.5f * boxDimensions;

        localMinCorner.x += ApproxBoxSphereRadius;
        localMinCorner.y += ApproxBoxSphereRadius;
        localMinCorner.z += ApproxBoxSphereRadius;

        for( int x = 0; x < maxX; ++x )
        {
            for (int y = 0; y < maxY; ++y)
            {
                for (int z = 0; z < maxZ; ++z)
                {
                    Vector3 offset = new Vector3(
                        sphereDiameter * x,
                        sphereDiameter * y,
                        sphereDiameter * z
                        );

                    Vector3 localCenter = localMinCorner + offset;

                    localCenter.x /= boxCollider.transform.lossyScale.x;
                    localCenter.y /= boxCollider.transform.lossyScale.y;
                    localCenter.z /= boxCollider.transform.lossyScale.z;

                    //Since the cube volume will be greater than the sphere volume inside of it
                    //we use this to calculate a multiplier to better approximate things.  
                    float cubeVolume = sphereDiameter * sphereDiameter * sphereDiameter;
                    float sphereVolume = MathUtils.CalcSphereVolume(ApproxBoxSphereRadius);

                    float volumeAdjustment = cubeVolume / sphereVolume;

                    //Create the buoyancy sphere
                    BuoyancySphere sphere = new BuoyancySphere(
                        boxCollider.gameObject,
                        ApproxBoxSphereRadius,
                        localCenter,
                        volumeAdjustment
                        );

                    m_BuoyancySpheres.Add(sphere);

                }
            }

        }
    }
    public void ApplyForce(float mass, Vector3 applyPt)
    {
        Vector3 normalForce = new Vector3(
            0.0f,
            -9.8f * mass,
            0.0f
            );

        Body.AddForceAtPosition(normalForce, applyPt);
    }

    List<BuoyancySphere> m_BuoyancySpheres;
}

class BuoyancySphere
{
    public BuoyancySphere(GameObject parent, float radius, Vector3 localCenter, float volumeAdjustment)
    {
        m_ParentObj = parent;

        m_Radius = radius;

        m_LocalPos = localCenter;

        m_VolumeAdjustment = volumeAdjustment;
    }

    public void ApplyForce(WaterBuoyancyHandler handler)
    {
        Vector3 worldPos = m_ParentObj.transform.TransformPoint(m_LocalPos);

        float sphereBottom = worldPos.y - m_Radius;

        float heightUnderWater = handler.WaterBody.WaterHeight - sphereBottom;
        heightUnderWater = Mathf.Clamp(heightUnderWater, 0.0f, 2.0f * m_Radius);

        float volumeUnderWater = MathUtils.CalcSphereCapVolume(m_Radius, heightUnderWater);

        volumeUnderWater *= m_VolumeAdjustment;

        float fluidWeight = volumeUnderWater * handler.WaterBody.Density * Physics.gravity.y;

        //Buoyancy Force.  This will be the opposite of the weight of the displaced fluid.
        float forceAmount = -fluidWeight;
        //Damping force
        Vector3 velocityAtPos = handler.Body.GetPointVelocity(worldPos);
        float dampingForce = 0.0f;

        if(velocityAtPos.y > 0.0f)
        {
            dampingForce = handler.WaterBody.BuoyancyDamping * velocityAtPos.y * velocityAtPos.y;
        }

        //Add the force to the object
        forceAmount = Mathf.Max(0.0f, forceAmount - dampingForce);

        Vector3 force = new Vector3(0.0f, forceAmount, 0.0f);

        handler.Body.AddForceAtPosition(force, worldPos);

    }

    public void OnDrawGizmosSelected()
    {
        Vector3 worldPos = m_ParentObj.transform.TransformPoint(m_LocalPos);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(worldPos, m_Radius);
    }

    float m_Radius;
    Vector3 m_LocalPos;
    float m_VolumeAdjustment;

    GameObject m_ParentObj;
}
