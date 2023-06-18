using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ObjectPool
{
    public Type PoolType;
    public string ObjectName = "";

    public float lastUsed = 0.0f;

    public int ObjectCount
    {
        get { return m_PoolGameObjects.Count; }
    }

    public GameObject GetObject(GameObject prefab)
    {
        GameObject retrievedObject = null;
        var disabledObjects = m_PoolGameObjects.Where(x => x.activeSelf == false);

        if(disabledObjects.Any())
        {
            retrievedObject = disabledObjects.First();
        }
        else
        {
            retrievedObject = CreateNewObject(prefab);
        }

        retrievedObject.gameObject.SetActive(true);
        lastUsed = Time.timeSinceLevelLoad;

		return retrievedObject;
	}
	public void RemoveDisabledObjects()
	{

		var disabledPoolObjects = m_PoolGameObjects.Where (poolObject => poolObject.gameObject.activeSelf == false).ToList ();

        m_PoolGameObjects = m_PoolGameObjects.Where (poolObject => poolObject.gameObject.activeSelf == true).ToList ();


		if(disabledPoolObjects != null)
		{
			foreach(GameObject poolObject in disabledPoolObjects)
			{
				GameObject.Destroy(poolObject);
			}

		}
		lastUsed = Time.timeSinceLevelLoad;
	}

    public void DestroyAll()
    {
        foreach (GameObject poolObject in m_PoolGameObjects)
        {
            GameObject.Destroy(poolObject);
        }

        m_PoolGameObjects.Clear();
    }

    public GameObject CreateNewObject(GameObject prefab)
    {
        GameObject returnObject = PoolManager.ObjectCreator(prefab);
        m_PoolGameObjects.Add(returnObject);

        return returnObject;
    }

    List<GameObject> m_PoolGameObjects = new List<GameObject>();
}
