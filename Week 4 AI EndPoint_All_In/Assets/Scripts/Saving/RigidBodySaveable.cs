using UnityEngine;
using System.Runtime.Serialization;
using System.IO;

//Put this component on objects that are raw physics objects that just need their rigid bodies serialized
public class RigidBodySaveable : MonoBehaviour, Saveable
{
    public void OnSave(Stream stream, IFormatter formatter)
    {
        SaveUtils.SerializeVector3(stream, formatter, transform.position);
        SaveUtils.SerializeQuaternion(stream, formatter, transform.rotation);

        SaveUtils.SerializeVector3(stream, formatter, GetComponent<Rigidbody>().velocity);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        transform.position = SaveUtils.DeserializeVector3(stream, formatter);
        transform.rotation = SaveUtils.DeserializeQuaternion(stream, formatter);

        GetComponent<Rigidbody>().velocity = SaveUtils.DeserializeVector3(stream, formatter);
    }
}
