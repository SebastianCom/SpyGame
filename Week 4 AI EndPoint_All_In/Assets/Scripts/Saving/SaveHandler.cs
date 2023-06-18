using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;
using System.IO;
using System;

//This class will handle the saving and loading for objects.  This will be responsible for looping
//over the objects saveable components and calling their save/load functions.  It will also be responsible
//for creating saveIds for the object that can be used to uniquely identify objects when loading.
public class SaveHandler : MonoBehaviour 
{
    public bool AllowSave = true;
    public bool AssignSaveId = true;

    [UniqueIdentifier]
    public string SaveId;

    void Awake()
    {
        //If the object hasn't gotten a save id yet, do it now.  This will happen for dynamic objects.
        if (AssignSaveId)
        {
            Guid guid = Guid.NewGuid();
            SaveId = guid.ToString();

            AssignSaveId = false;
        }
    }

	void Start () 
    {
	}


    public void SaveObject(Stream stream, IFormatter formatter)
    {
        //Save save id
        formatter.Serialize(stream, SaveId);
    }

    public static GameObject LoadObject(Stream stream, IFormatter formatter)
    {
        //Load saveId
        string saveId = (string)formatter.Deserialize(stream);

        return LevelManager.Instance.GetGameObjectBySaveId(saveId);
       
    }

    public void SaveData(Stream stream, IFormatter formatter)
    {
        //Call OnSave on all of the Savable components
        Component[] saveableComponents = gameObject.GetComponents(typeof(Saveable));
        foreach (Component component in saveableComponents)
        {
            Saveable saveable = (Saveable)component;
            saveable.OnSave(stream, formatter);
        }
    }

    public void LoadData(Stream stream, IFormatter formatter)
    {
        //Call on load on all of its Saveable components
        Component[] saveableComponents = GetComponents(typeof(Saveable));
        foreach (Component component in saveableComponents)
        {
            Saveable saveable = (Saveable)component;
            saveable.OnLoad(stream, formatter);
        }
    }

    //This function is called when the script is loaded or a value is changed in the inspector.
    //Note that this will only called in the editor.
    void OnValidate()
    {
        CreateSaveId();
    }

    //This function is called when the user hits the Reset button in the Inspector's context menu 
    //or when adding the component the first time.  We mainly care about the case where people will
    //the component for the first time because we want a saveId to get generated automatically when
    //you add the save handler for the first time.
    //Note that this will only called in the editor.
    void Reset()
    {
        CreateSaveId();
    }

    //Creates a new save id if one isn't already created
    void CreateSaveId()
    {
        //We don't want pre-fabs to generate save id's, because if they did, then every object that instantiated
        //from the prefab would get the same id.
        if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.Model)
        {
            if (AssignSaveId)
            {
                //Using GUIDs for the save ids.  Tecnically this aren't %100 guaranteed to be unique, but
                //the probability of generating a duplicate is really, really low.
                Guid guid = Guid.NewGuid();
                SaveId = guid.ToString();

                AssignSaveId = false;
            }
        }
        else
        {
            SaveId = "";
            AssignSaveId = true;
        }
    }
}
