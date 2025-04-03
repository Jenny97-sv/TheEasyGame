using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.Netcode.Components;
using Unity.VisualScripting;

public class SaveLoadManager : NetworkBehaviour
{
    public static SaveLoadManager Instance = null;
    private GameData gameData;
    private string path = "";
    //public List<string> playerNames = new List<string>();

    [System.Serializable]
    public struct GameData
    {
        public int bestInt;
        public List<Vector3> position;
        public List<Stats> stats;
    }


    //private InputActionAsset actionAsset = null;
    //private InputActionMap game = null;
    //private InputAction save = null;
    //private InputAction load = null;
    private void OnEnable()
    {
        //if (SceneHandler.Instance)
        //{
        //    if (SceneHandler.Instance.sceneName == SceneName.Menu)
        //        return;
        //}
        //else return;
        //actionAsset = this.GetComponent<PlayerInput>().actions;
        //game = actionAsset.FindActionMap("Game");

        //save = game.FindAction("Save");
        //save.performed += OnSave;

        //load = game.FindAction("Load");
        //load.performed += OnLoad;
    }
    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }


        path = Path.Combine(Application.persistentDataPath, "JennyTest.json");
    }

    private void OnDisable()
    {
        //load.performed -= OnLoad;
        //save.performed -= OnSave;
    }

    private void OnSave(InputAction.CallbackContext context)
    {
        //SaveGame();
    }

    private void OnLoad(InputAction.CallbackContext context)
    {
        //LoadLastSave();
    }


    // ----- public ------
    public void SaveGame()
    {
        //Debug.Log("Saving game...");

        List<Vector3> positions = new List<Vector3>();
        List<Stats> staats = new List<Stats>();
        if (SceneHandler.Instance.IsLocalGame)
        {
            foreach (GameObject player in GameManager.Instance.GetPlayers().Keys)
            {
                Stats stats = player.GetComponent<Stats>();
                if(!stats)
                {
                    Debug.Log("Stats are null");
                }
                staats.Add(stats);
                positions.Add(player.transform.position);
            }
        }
        else
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null)
                {
                    positions.Add(playerObject.gameObject.transform.position);
                    staats.Add(playerObject.GetComponent<Stats>());
                }
            }
        }



        gameData = new GameData
        {
            position = positions,
            stats = staats
        };


        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(path, json);

        Debug.Log($"Game saved to {path}");
    }

    public void LoadLastSave()
    {
        Debug.Log("Loading game...");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);

            if (SceneHandler.Instance.IsLocalGame)
            {
                var players = GameManager.Instance.GetPlayers();
                if(players != null && players.Count == SceneHandler.Instance.MaxPlayerCount)
                {
                    int i = 0;
                    foreach(GameObject player in players.Keys)
                    {
                        if (data.position != null && data.position.Count == players.Count)
                        {
                            player.transform.position = data.position[i];
                        }
                        if (data.stats[i] != null)
                        {
                            Stats playerStats = player.GetComponent<Stats>();
                            if (playerStats == null)
                            {
                                playerStats = player.AddComponent<Stats>();
                            }
                            playerStats.LoadStats(data.stats[i]);
                        }
                        else
                        {
                            Debug.Log("Stats are NULL!");
                        }
                        i++;
                    }
                }
            }
            else
            {
                var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
                if (data.position != null && data.position.Count == clientIds.Count)
                {
                    int index = 0;
                    foreach (var clientId in clientIds)
                    {
                        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                        var networkTransform = playerObject.GetComponent<NetworkTransform>();
                        var movement = playerObject.GetComponent<Movement>();
                        if (playerObject != null)
                        {
                            if (networkTransform != null)
                            {
                                networkTransform.Teleport(data.position[index], Quaternion.identity, new Vector3(1, 1, 1));
                            }
                        }
                        index++;
                    }
                }
                else
                {
                    Debug.LogError("Mismatch between saved positions and connected clients.");
                }
            }
        }
        else
        {
            Debug.LogWarning("No save file found!");
        }
    }

    private void OnApplicationQuit()
    {
        //SaveGame();
    }
}
