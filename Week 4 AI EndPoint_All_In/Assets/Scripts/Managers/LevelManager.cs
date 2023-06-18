using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelManager : MonoBehaviour 
{
    public static LevelManager Instance;
    
    //This isn't really needed for anything other than confirming that the game manager persisted between scenes
    public string OwningLevelName;

    public string SaveFileName = "TestSaveGame.sav";

    //The name of the empty game object used to position the player when he/she transitions between levels.
    const string TransitionPtName = "LevelTransitionPt";

    //Tracks the version of your save games.  You should increment this number whenever you make changes that will
    //effect what gets saved.  This will allow you to detect incompatible save game versions early, instead of getting
    //weird hard to track down bugs.
    const int SaveGameVersionNum = 1;


    void Awake()
    {
        //This is similar to a singleton in that it only allows one instance to exist and there is instant global 
        //access to the LevelManager using the static Instance member.
        //
        //This will set the instance to this object if it is the first time it is created.  Otherwise it will delete 
        //itself.
        if (Instance == null)
        {
            //This tells unity not to delete the object when you load another scene
            DontDestroyOnLoad(gameObject);
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        m_LoadSceneState = LoadSceneState.NotLoadingScene;

        m_PersistentData = new GamePersistentData();
        m_AIPlayerContainer = new Dictionary<string, Player>();
    }

	void Start () 
    {
        m_PersistentData.levelName = SceneManager.GetActiveScene().name;
	}
	
	void Update () 
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save(SaveFileName);
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            Load(SaveFileName);
        }
    }

    public Player GetPlayer()
    {
        return m_CurrentPlayer;
    }

    public void RegisterPlayer(Player player)
    {
        m_CurrentPlayer = player;
    }
    public void RegisterAI(string saveId, Player player)
    {
        m_AIPlayerContainer.Add(saveId, player);
    }

    public Player GetAI(string saveId)
    {
        
        if(m_AIPlayerContainer.ContainsKey(saveId))
        {
            return m_AIPlayerContainer[saveId];
        }
        else
        {
            return null;
        }
    }


    //This will search for a game object based on a save id.  This might end up being slow if 
    //there are a lot of objects in a scene.
    public GameObject GetGameObjectBySaveId(string saveId)
    {
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject searchObj in gameObjects)
        {
            SaveHandler saveHandler = searchObj.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            if (saveHandler.SaveId == saveId)
            {
                return searchObj;
            }
        }

        return null;
    }

    //This callback gets called when a scene is done loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (m_LoadSceneState)
        {
            case LoadSceneState.NotLoadingScene:

                break;

            case LoadSceneState.LevelTransition:

                //Move the player to the proper transition position
                if (m_CurrentPlayer != null)
                {
                    Vector3 loadPos = m_CurrentPlayer.transform.position;
                    Vector3 loadDir = m_CurrentPlayer.transform.forward;

                    GameObject transitionPt = GameObject.Find(TransitionPtName);
                    if (transitionPt != null)
                    {
                        loadPos = transitionPt.transform.position;
                        loadDir = transitionPt.transform.forward;
                    }

                    m_CurrentPlayer.OnLevelTransition(loadPos, loadDir);
                }

                break;

            case LoadSceneState.LoadingSaveGame:
                //This section will finish loading the save game.  We need to load the objects here since

                //Get number of objects to load  //continue loading from stream of open file
                int numObjectsToLoad = (int)m_LoadGameFormatter.Deserialize(m_LoadGameStream);

                //Load objects Stage 1.  The objects are loaded in two stages so that
                //by the time the second stage is running all of the objects will exist which
                //will make reconstructing relationships between everything easier
                List<GameObject> gameObjectsLoaded = new List<GameObject>();
                for (int i = 0; i < numObjectsToLoad; ++i)
                {
                    GameObject loadedObject = SaveHandler.LoadObject(m_LoadGameStream, m_LoadGameFormatter);

                    if (loadedObject != null)
                    {
                        gameObjectsLoaded.Add(loadedObject);
                    }
                }

                //Load objects Stage 2
                for (int i = 0; i < numObjectsToLoad; ++i)
                {
                    GameObject loadedObject = gameObjectsLoaded[i];

                    SaveHandler saveHandler = loadedObject.GetComponent<SaveHandler>();

                    saveHandler.LoadData(m_LoadGameStream, m_LoadGameFormatter);
                }

                //Clean up
                m_LoadGameStream.Close();
                m_LoadGameStream = null;
                m_LoadGameFormatter = null;
                break;

            default:
                DebugUtils.LogError("Unsupported load scene state after load: {0}", m_LoadSceneState);
                break;
        }

        if (m_PersistentData != null)
        {
            m_PersistentData.levelName = SceneManager.GetActiveScene().name;
        }

        m_LoadSceneState = LoadSceneState.NotLoadingScene;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(5, 5, 3000, 40), "Save Game: F5, Load Game: F9");
    }

    //Call this function to transition the player to a new scene.
    public void TransitionToLevel(string levelName)
    {
        m_LoadSceneState = LoadSceneState.LevelTransition;

        //Move to the next scene
        DebugUtils.Log("Transitioning to level: {0}", levelName);

        SceneManager.LoadScene(levelName);
    }

    //Use this to save your game.  Make sure that the order that you serialize things is the same as the order that
    //you deserialize things
    public void Save(string fileName)
    {
        string savePath = GetSaveFilePath(fileName);

        DebugUtils.Log("Saving Game to file: '{0}'", savePath);

        //Serializes and deserializes an object, or an entire graph of connected objects, in binary format
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        //Creates or overwrites a file in the specified path
        FileStream file = File.Create(savePath);

        //Save Version number
        binaryFormatter.Serialize(file, SaveGameVersionNum);

        //Save persistent data
        binaryFormatter.Serialize(file, m_PersistentData);

        //Save the objects
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();

        //Need to save out how many objects are saved so we know how how many to loop over
        //when we load
        List<SaveHandler> objectsToSave = new List<SaveHandler>();
        foreach (GameObject gameObjectToSave in gameObjects)
        {
            //Get the save handler on the object if there is one
            SaveHandler saveHandler = gameObjectToSave.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            //Checking if this object can and should be saved
            if (!saveHandler.AllowSave)
            {
                continue;
            }

            objectsToSave.Add(saveHandler);
        }

        binaryFormatter.Serialize(file, objectsToSave.Count);

        //Save the game objects stage 1.  Saving out all of the data needed to recreate
        //the objects
        foreach (SaveHandler saveHandler in objectsToSave)
        {
            saveHandler.SaveObject(file, binaryFormatter);
        }

        //Save the game objects stage 2.  Saving the rest of the data for the objects Scripts
        foreach (SaveHandler saveHandler in objectsToSave)
        {
            saveHandler.SaveData(file, binaryFormatter);
        }

        //Clean up
        file.Close();
    }

    //Use this to load your game.  Make sure that the order that you deserialize things is the same as the order that
    //you serialize things
    public void Load(string fileName)
    {
        //Set the proper loading state
        m_LoadSceneState = LoadSceneState.LoadingSaveGame;

        //Get and verify the save path
        string savePath = GetSaveFilePath(fileName);

        DebugUtils.Log("Loading Game from file: {0}...", savePath);

        if (!File.Exists(savePath))
        {
            DebugUtils.Log("LoadFile doesn't exist: {0}", savePath);
            return;
        }

        m_LoadGameFormatter = new BinaryFormatter();
        m_LoadGameStream = File.Open(savePath, FileMode.Open);

        //Version number
        int versionNumber = (int)m_LoadGameFormatter.Deserialize(m_LoadGameStream);

        DebugUtils.Log("Load file version number: {0}", versionNumber);

        //Handle version numbers appropriately
        DebugUtils.Assert(versionNumber == SaveGameVersionNum, "Loading an incompatible version number (File version: {0}, Expected version: {1}).  Your game may become unstable.", versionNumber, SaveGameVersionNum);

        //Load GameSaveData
        m_PersistentData = (GamePersistentData)m_LoadGameFormatter.Deserialize(m_LoadGameStream);

        //Load the level
        SceneManager.LoadScene(m_PersistentData.levelName);

        PoolManager.DestroyAllPools();

        //The rest will be loaded once the level is done loading.  (otherwise the loaded objects will be
        //deleted during the level transition)
    }

    //A helper function to create the save path.  This uses the persistentDataPath, which will be a safe place
    //to store data on a user's machine without errors.
    string GetSaveFilePath(string fileName)
    {
        return Application.persistentDataPath + "/" + fileName;
    }

    //This is used to keep track of what kind of loading we need to do
    enum LoadSceneState
    {
        NotLoadingScene,
        LevelTransition,
        LoadingSaveGame
    }

    Player m_CurrentPlayer;
    Dictionary<string, Player> m_AIPlayerContainer;

    LoadSceneState m_LoadSceneState;

    BinaryFormatter m_LoadGameFormatter;
    FileStream m_LoadGameStream;

    GamePersistentData m_PersistentData;
}

[Serializable]
class GamePersistentData
{
    public string levelName;
}
