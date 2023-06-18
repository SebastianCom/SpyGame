

using UnityEngine;
using System;
using System.Runtime.Serialization;
using System.IO;

public class SaveUtils
{
    public static void SerializeVector3(Stream stream, IFormatter formatter, Vector3 value)
    {
        formatter.Serialize(stream, value.x);
        formatter.Serialize(stream, value.y);
        formatter.Serialize(stream, value.z);
    }

    public static Vector3 DeserializeVector3(Stream stream, IFormatter formatter)
    {
        Vector3 value = new Vector3();

        value.x = (float)formatter.Deserialize(stream);
        value.y = (float)formatter.Deserialize(stream);
        value.z = (float)formatter.Deserialize(stream);

        return value;
    }

    public static void SerializeQuaternion(Stream stream, IFormatter formatter, Quaternion value)
    {
        formatter.Serialize(stream, value.w);
        formatter.Serialize(stream, value.x);
        formatter.Serialize(stream, value.y);
        formatter.Serialize(stream, value.z);
    }

    public static Quaternion DeserializeQuaternion(Stream stream, IFormatter formatter)
    {
        Quaternion value = new Quaternion();

        value.w = (float)formatter.Deserialize(stream);
        value.x = (float)formatter.Deserialize(stream);
        value.y = (float)formatter.Deserialize(stream);
        value.z = (float)formatter.Deserialize(stream);

        return value;
    }

    //Returns true if this was successful.  An emptry string will be serialized if the save object
    //is null or if a save handler doesn't exist on an object.
    public static bool SerializeObjectRef(Stream stream, IFormatter formatter, GameObject saveObj)
    {
        string saveId = "";
        bool success = false;

        if (saveObj != null)
        {
            SaveHandler saveHandler = saveObj.GetComponent<SaveHandler>();
            if (saveHandler != null)
            {
                success = true;

                saveId = saveHandler.SaveId;
            }
        }

        formatter.Serialize(stream, saveId);

        return success;
    }

    //Returns the object that the serialized save id refers to.  Could return null if the save id 
    //was an empty string or if the object it refers to doesn't exist.
    public static GameObject DeserializeObjectRef(Stream stream, IFormatter formatter)
    {
        GameObject loadObj = null;
        
        string saveId = (string)formatter.Deserialize(stream);

        if (saveId != "")
        {
            loadObj = LevelManager.Instance.GetGameObjectBySaveId(saveId);
        }

        return loadObj;
    }
}
