using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassToShader : MonoBehaviour
{

    List<AIPlayerController> AiControllers = new List<AIPlayerController>();
    Vector4[] AiPositions = new Vector4[4];
    Vector4[] AiDirections = new Vector4[4];
    Renderer Renderer;
    // Start is called before the first frame update
    void Start()
    {
        Renderer = GameObject.Find("TestSurface").GetComponent<Renderer>();
        Renderer.material.shader = Shader.Find("FieldOfView");

        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject searchObj in gameObjects)
        {
            AIPlayerController Ai = searchObj.GetComponent<AIPlayerController>();
            if (Ai == null)
            {
                continue;
            }
            else
            {
                AiControllers.Add(Ai);

            }

        }
    }

    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i < AiControllers.Count; i++)
        {
            AiPositions[i] = AiControllers[i].transform.position;
            AiDirections[i] = AiControllers[i].transform.forward;
        }

        Renderer.material.SetVector("_Position1", AiPositions[0]);
        Renderer.material.SetVector("_Direction1", AiDirections[0]);

        Renderer.material.SetVector("_Position2", AiPositions[1]);
        Renderer.material.SetVector("_Direction2", AiDirections[1]);

        Renderer.material.SetVector("_Position3", AiPositions[2]);
        Renderer.material.SetVector("_Direction3", AiDirections[2]);

        Renderer.material.SetVector("_Position4", AiPositions[3]);
        Renderer.material.SetVector("_Direction4", AiDirections[3]);
    }
}
