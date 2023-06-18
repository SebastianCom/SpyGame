using System;
using System.Collections.Generic;
using UnityEngine;

public class AIUtils
{
    public delegate bool SearchFilterDelegate(GameObject obj);

    //Finds the closest object in a radius that matches the criteria of the searchFilter
    public static GameObject FindClosestObjectInRadius(
        Vector3 checkCenter,
        float checkRadius,
        SearchFilterDelegate searchFilter
        )
    {
        Collider[] overlapColliders = Physics.OverlapSphere(checkCenter, checkRadius);

        float minDistSqrd = float.MaxValue;
        GameObject closestObj = null;

        foreach (Collider collider in overlapColliders)
        {
            if (!searchFilter(collider.gameObject))
            {
                continue;
            }

            float distSqrd = (collider.transform.position - checkCenter).sqrMagnitude;
            if (distSqrd < minDistSqrd)
            {
                minDistSqrd = distSqrd;

                closestObj = collider.gameObject;
            }
        }

        return closestObj;
    }
}
