using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Unity.VisualScripting;
using FMODUnity;

public enum SceneName
{
    Menu,
    Scene1,
    Scene2
};

public class SceneHandler : NetworkBehaviour
{
    public static SceneHandler Instance = null;

    public NetworkVariable<SceneName> sceneName = new NetworkVariable<SceneName>(SceneName.Menu);
    [SerializeField] private GameObject playerPrefab = null;
    public bool IsLocalGame = true;
    public int MaxPlayerCount = -1;

    private Dictionary<ulong, bool> clientsLoaded = new Dictionary<ulong, bool>();

    private void Awake()
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
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }
        if (PlayerInputManager.instance)
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
    }
    void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log($"Player {playerInput.playerIndex} joined!");
    }

    private IEnumerator WaitForNetwork()
    {
        Debug.Log("Waiting for network scene to load");
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName.Value.ToString());

        if (sceneName.Value == SceneName.Scene1)
        {
            if (FindObjectOfType<StartPosition>() == null)
            {
                Debug.LogError("StartPosition not found before waiting!");
                yield return new WaitUntil(() => FindObjectOfType<StartPosition>() != null);
            }
            GameManager.Instance.DestroyPlayers();
        }

        yield return new WaitForEndOfFrame();

        StartPosition startPosition = FindObjectOfType<StartPosition>();
        startPosition.CurrentWorld = UnityEngine.Random.Range(0, 2);
        Debug.Log("World = " + startPosition.CurrentWorld);

        if (sceneName.Value == SceneName.Scene1)
        {
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                Debug.Log("Spawn player " + id);
                SpawnPlayer(startPosition, id);
            }
        }
    }

    private IEnumerator WaitForScene()
    {
        GameManager.Instance.DestroyPlayers();

        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => FindObjectOfType<StartPosition>() != null);
        yield return new WaitForEndOfFrame();

        AudioManager.Instance.SetParameter(eMusic.Music, 1);
        StartPosition startPosition = FindObjectOfType<StartPosition>();
        startPosition.CurrentWorld = UnityEngine.Random.Range(0, 2);
        Debug.Log("World = " + startPosition.CurrentWorld);

        for (int i = 0; i < MaxPlayerCount; i++)
        {
            Transform startPos = StartPosition(startPosition, (ulong)i);
            GameObject playerObject = null;

            var playersDict = GameManager.Instance.GetPlayers();
            var playerKeys = new List<GameObject>(playersDict.Keys);

            if (playerKeys.Count > i) 
            {
                playerObject = playerKeys[i];
            }
            else
            {
                playerObject = Instantiate(playerPrefab, startPos.position, startPos.rotation);
                GameManager.Instance.SetPlayers(playerObject, i, MaxPlayerCount);
                PlayerInputManager.instance.DisableJoining();
            }

            if (playerObject)
            {
                playerObject.transform.position = startPos.position;
                playerObject.transform.rotation = startPos.rotation;
                playerObject.GetComponent<Stats>().IsWinner.Value = true;
                playerObject.GetComponent<Stats>().ID.Value = i;
                playerObject.GetComponent<Movement>().ReStart();
            }
            else
            {
                Debug.LogError("Something went wrong, player doesn't exist.");
            }
        }

    }

    void LoadSceneAndSpawnPlayers(SceneName targetScene)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (targetScene == SceneName.Scene1 || targetScene == SceneName.Menu)
        {
            Debug.Log("Cleaning up!");
            CleanupExistingPlayers();
        }

        sceneName.Value = targetScene;

        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);

        StartCoroutine(WaitForNetwork());
    }

    void CleanupExistingPlayers()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject != null)
            {
                try
                {
                    if (playerObject.IsSpawned)
                    {
                        playerObject.Despawn(false);
                    }
                    else
                    {
                        NetworkObject.Destroy(playerObject.gameObject);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to despawn player object for client {clientId}: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"No player object found for client {clientId}");
            }
        }
    }

    void SpawnPlayer(StartPosition startPos, ulong clientId)
    {
        Transform startPoss = StartPosition(startPos, clientId);
        var playerObject = Instantiate(playerPrefab, startPoss.position, startPoss.rotation);

        if (playerObject)
        {
            playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            playerObject.transform.position = startPoss.position;
            playerObject.GetComponent<Stats>().IsWinner.Value = true;
            GameManager.Instance.SetPlayers(playerObject, (int)clientId, MaxPlayerCount);
        }
        else
        {
            Debug.Log("Something went wrong, player don't exist");
        }
    }

    private Transform StartPosition(StartPosition startPos, ulong clientId)
    {
        if (startPos != null)
        {
            GameManager.Instance.CurrentWorld = startPos.CurrentWorld;

            return startPos.BetterStartPosition(clientId);
        }
        else
        {
            Debug.Log("NO START POSITION FOUND!");
            GameObject tempObject = new GameObject("DefaultTransform");
            tempObject.transform.position = Vector3.zero;
            tempObject.transform.rotation = Quaternion.identity;
            return tempObject.transform;
        }
    }

    [ClientRpc]
    void NotifyClientOfSpawnClientRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            AudioManager.Instance.SetParameter(eMusic.Music, 1);
            Debug.Log($"Client {clientId} received spawn confirmation at {position}");
        }
    }


    public void SwitchScene(SceneName scene, int PlayerCount)
    {
        if (scene == SceneName.Menu)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                UIEndScreen uIEndScreen = FindObjectOfType<UIEndScreen>();
                if (uIEndScreen)
                {
                    uIEndScreen.SwitchToMenuClientRpc();
                }

                AudioManager.Instance.SetParameter(eMusic.Music, 0);
                //AudioManager.Instance.PlayMusic(eMusic.Menu);
                SceneManager.LoadScene(scene.ToString());
            }
            MaxPlayerCount = 0;
        }
        else
        {
            MaxPlayerCount = PlayerCount;
            LoadSceneAndSpawnPlayers(scene);
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ulong clientID = client.ClientId;
                Transform transform = client.PlayerObject.gameObject.transform;
                NotifyClientOfSpawnClientRpc(clientID, transform.position, transform.rotation);
            }
        }
    }

    public void SwitchSceneLocal(SceneName scene, int PlayerCount)
    {
        switch (scene)
        {
            case SceneName.Menu:
                AudioManager.Instance.SetParameter(eMusic.Music, 0);
                //AudioManager.Instance.PlayMusic(eMusic.Menu);
                sceneName.Value = SceneName.Menu;
                SceneManager.LoadScene(scene.ToString());
                MaxPlayerCount = 0;
                break;

            case SceneName.Scene1:
                SceneManager.LoadScene(scene.ToString());
                MaxPlayerCount = PlayerCount;
                sceneName.Value = SceneName.Scene1;
                StartCoroutine(WaitForScene());
                break;

            case SceneName.Scene2:
                AudioManager.Instance.SetParameter(eMusic.Music, 2);
                //AudioManager.Instance.PlayMusic(eMusic.Win);
                sceneName.Value = SceneName.Scene2;
                SceneManager.LoadScene(scene.ToString());
                break;
        }
    }

    NetworkObject FindNetworkObjectByName(string objectName)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (obj.gameObject.name == objectName)
            {
                return obj;
            }
        }
        return null;
    }

    private void OnClientStopped(bool shit)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            ReturnToMenuClientRpc();
        }
    }
    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
        Relay.Instance.LeaveRelay();

        if (IsServer)
        {
            NetworkObject netObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            netObj.RemoveOwnership();
            NetworkManager.Singleton.DisconnectClient(clientId);
            ReturnToMenuClientRpc();
            StartCoroutine(DelayedShutdown());
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(SceneName.Menu.ToString());
            AudioManager.Instance.SetParameter(eMusic.Music, 0);
            //AudioManager.Instance.PlayMusic(eMusic.Menu);
        }
    }

    private IEnumerator DelayedShutdown()
    {
        yield return new WaitForSeconds(0.1f);

        if (IsServer)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(SceneName.Menu.ToString());
        }
    }

    [ClientRpc]
    private void ReturnToMenuClientRpc()
    {
        Debug.Log("Received return to menu command!");

        if (!IsServer)
        {
            StartCoroutine(ReturnToMenuSequence());
        }
    }

    private IEnumerator ReturnToMenuSequence()
    {
        if (IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(SceneName.Menu.ToString());
    }
}
