using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance = null;
    private Dictionary<GameObject, int> players = new Dictionary<GameObject, int>();

    private bool isEndingGame = false;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private GameObject pickupBuffPrefab;
    [SerializeField] private GameObject pickupDebuffPrefab;
    [SerializeField] private GameObject pickupHealthPrefab;
    [SerializeField] private GameObject pickupDamageBuffPrefab;
    [SerializeField] private GameObject pickupSpeedPrefab;

    [SerializeField] private TMP_InputField nameField;

    [HideInInspector] private List<Collider> bounds = new List<Collider>();

    // Pickup values
    private const int buffCount = 3;
    private const int debuffCount = 3;
    private const int healthCount = 3;
    private const int damageBuffCount = 3;
    private const int speedCount = 3;

    private float currentTimeTilNextPickup = 5;
    private float timerTilNextPickup = 0;
    private int maxTimeTilNextPickup = 10;
    private int minTimeTilNextPickup = 1;

    // End screen slow down
    private float slowDownFactor = 0.2f;
    [HideInInspector] public float duration = 5;
    [HideInInspector] public bool IsSlowedDown = false;

    public int CurrentWorld = 0;

    public void SetPlayerName(string playerName, int playerIndex)
    {
        SetNameServerRPC(playerName, playerIndex);
        Debug.LogError($"Player with index {playerIndex} not found!");
    }

    [ServerRpc]
    private void SetNameServerRPC(string name, int index)
    {
        foreach (var player in players) // Loop through dictionary
        {
            if (player.Value == index) // Find the matching index
            {
                player.Key.GetComponent<Stats>().myName.Value = name; // Set the name
                return;
            }
        }
    }


    public GameObject GetPlayerGameObjectByIndex(int index)
    {
        if (index < 0 || index >= players.Count)
        {
            Debug.LogError("Index out of range!");
            return null;
        }

        return players.ElementAt(index).Key;
    }

    public Dictionary<GameObject, int> GetPlayers()
    {
        if (players.Count <= 0)
            return new Dictionary<GameObject, int>();

        Dictionary<GameObject, int> validPlayers = new Dictionary<GameObject, int>();
        foreach (var p in players)
        {
            if (p.Key)
                validPlayers[p.Key] = p.Value; 
        }

        return validPlayers;
    }


    //public Dictionary<GameObject, int> GetPlayers()
    //{
    //    bool nullPlayers = false;
    //    foreach (var key in players.Keys.ToArray())
    //    {
    //        if (key == null)
    //        {
    //            nullPlayers = true;
    //            break;
    //        }
    //    }
    //    if (nullPlayers)
    //    {
    //        if (SceneHandler.Instance.IsLocalGame)
    //        {
    //            players.Clear();
    //            players = new Dictionary<GameObject, int>();
    //            for (int i = 0; i < SceneHandler.Instance.MaxPlayerCount; i++)
    //            {
    //                GameObject player = Instantiate(playerPrefab);
    //                players.Add(player, i);
    //            }
    //        }
    //        else
    //        {
    //            if (IsServer)
    //            {
    //                // Implementation for other shit....
    //                foreach (ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
    //                {

    //                }
    //            }
    //        }
    //    }
    //    return players;
    //}

    private void Awake()
    {
        if (!Instance)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame && NetworkManager.Singleton.IsServer || SceneHandler.Instance.IsLocalGame)
        {
            if (SceneHandler.Instance.sceneName.Value != SceneName.Scene1)
                return;

            if (Input.GetKeyUp(KeyCode.N)) // Only temp, remove later
            {
                foreach (var play in players)
                {
                    play.Key.GetComponent<Stats>().TakeDamage(2);
                }
            }

            UpdateGame();
            UpdatePickup();
            UpdateInput();
        }

    }

    private void UpdateGame()
    {
        if (isEndingGame) return;

        int count = 0;
        foreach (var play in players)
        {
            if (play.Key == null)
                return;

            if (play.Key.GetComponent<Stats>().HP.Value <= 0)
            {
                count++;
                play.Key.GetComponent<Stats>().IsWinner.Value = false;
            }

            if (players.Count > 1)
            {
                if (count >= players.Count - 1)
                {
                    foreach (var pla in players)
                    {
                        Stats info = pla.Key.GetComponent<Stats>();
                        if (info.IsWinner.Value)
                        {
                            info.myScore.Value++;
                            break;
                        }
                    }
                    isEndingGame = true;
                    StartCoroutine(EndScreenRoutine());
                }
            }
            else
            {
                if (count >= players.Count)
                {
                    play.Key.GetComponent<Stats>().myScore.Value++;
                    isEndingGame = true;
                    StartCoroutine(EndScreenRoutine());
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // This is the new start!
    {
        if (SceneHandler.Instance.sceneName.Value != SceneName.Scene1)
            return;
        if (PoolManager.Instance)
        {
            PoolManager.Instance.Register(pickupBuffPrefab, buffCount);
            PoolManager.Instance.Register(pickupDebuffPrefab, debuffCount);
            PoolManager.Instance.Register(pickupHealthPrefab, healthCount);
            PoolManager.Instance.Register(pickupDamageBuffPrefab, damageBuffCount);
            PoolManager.Instance.Register(pickupSpeedPrefab, speedCount);
        }


        bounds.Clear();

        GameObject[] boundObjects = GameObject.FindGameObjectsWithTag("SpawnBounds");
        if (boundObjects.Length <= 0 || !boundObjects[0])
            return;
        foreach (GameObject obj in boundObjects)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                bounds.Add(col);
            }
        }

        if (bounds.Count == 0)
        {
            Debug.LogWarning("No bounds found in the scene!");
        }
    }

    public void SetPlayers(GameObject playerObject, int index, int maxPlayers)
    {
        if (!players.ContainsKey(playerObject) && players.Count < maxPlayers)
        {
            players.Add(playerObject, index);
            if(SceneHandler.Instance.IsLocalGame)
                DontDestroyOnLoad(playerObject);
        }
        else
        {
            Debug.Log("PLayer already exists!");
        }
    }

    public void DestroyPlayers()
    {
        foreach (var player in players.Keys)
        {
            Destroy(player);
        }
        players.Clear();
    }

    public override void OnDestroy()
    {
        DestroyPlayers();
    }


    private IEnumerator EndScreenRoutine()
    {
        if (SceneHandler.Instance.sceneName.Value != SceneName.Scene1)
            yield return null;

        //AudioManager.Instance.StopMusic(eMusic.TimeBuffer);

        if (SceneHandler.Instance.IsLocalGame)
        {
            StartCoroutine(LocalSlowMotionRoutine(slowDownFactor, duration));
        }
        else
        {
            TriggerSlowMotionServerRpc(slowDownFactor, duration);
        }

        yield return new WaitForSecondsRealtime(duration);

        //AudioManager.Instance.StopMusic(eMusic.Music);
        //AudioManager.Instance.SetMusicVolume(0);
        Debug.Log("Game manager");
        ReturnPooledObjects();

        foreach (GameObject player in players.Keys)
        {
            Stats stats = player.GetComponent<Stats>();
            stats.Heal(stats.DefaultMaxHP.Value);
        }
        SaveLoadManager.Instance.SaveGame();
        isEndingGame = false;

        if (SceneHandler.Instance.IsLocalGame)
        {
            SceneHandler.Instance.SwitchSceneLocal(SceneName.Scene2, players.Count);
        }
        else
        {
            SceneHandler.Instance.SwitchScene(SceneName.Scene2, players.Count);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerSlowMotionServerRpc(float slowDownFactor, float duration)
    {
        ApplySlowMotionClientRpc(slowDownFactor, duration);
    }

    [ClientRpc]
    private void ApplySlowMotionClientRpc(float slowDownFactor, float duration)
    {
        StartCoroutine(SlowMotionRoutine(slowDownFactor, duration));
    }

    private IEnumerator SlowMotionRoutine(float slowDownFactor, float duration)
    {
        Time.timeScale = slowDownFactor;
        Time.fixedDeltaTime = Time.timeScale;

        IsSlowedDown = true;

        yield return new WaitForSecondsRealtime(duration);

        ResetTimeScaleServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetTimeScaleServerRpc()
    {
        ResetTimeScaleClientRpc();
    }

    [ClientRpc]
    private void ResetTimeScaleClientRpc()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        IsSlowedDown = false;
    }


    private IEnumerator LocalSlowMotionRoutine(float slowDownFactor, float duration)
    {
        Time.timeScale = slowDownFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        IsSlowedDown = true;

        yield return new WaitForSecondsRealtime(duration);

        ResetTimeScale();
    }

    private void ResetTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        IsSlowedDown = false;
    }


    private void UpdatePickup()
    {
        timerTilNextPickup += Time.deltaTime;

        if (timerTilNextPickup > currentTimeTilNextPickup)
        {
            timerTilNextPickup = 0;
            currentTimeTilNextPickup = Random.Range(minTimeTilNextPickup, maxTimeTilNextPickup);

            int randomPickup = Random.Range(0, 6); // Need to update this when more pickups are created, int needs to be plus 1!
            if (SceneHandler.Instance.IsLocalGame)
            {
                SpawnPickupLocal(randomPickup);
            }
            else
            {
                SpawnPickupServerRpc(randomPickup);
            }
        }
    }


    [ServerRpc]
    private void SpawnPickupServerRpc(int randomPickup)
    {
        Bounds worldBounds = bounds[CurrentWorld].bounds;
        float x = Random.Range(worldBounds.min.x, worldBounds.max.x);
        float y = Random.Range(worldBounds.min.y, worldBounds.max.y);
        float z = Random.Range(worldBounds.min.z, worldBounds.max.z);
        if (y < 0)
            y *= -1;
        Vector3 spawnPosition = new Vector3(x, y, z);

        switch (randomPickup)
        {
            case 0:
                SpawnPickup(pickupBuffPrefab, spawnPosition);
                break;

            case 1:
                SpawnPickup(pickupDebuffPrefab, spawnPosition);
                break;

            case 2:
                SpawnPickup(pickupHealthPrefab, spawnPosition);
                break;

            case 3:
                SpawnPickup(pickupDamageBuffPrefab, spawnPosition);
                break;

            case 5:
                SpawnPickup(pickupSpeedPrefab, spawnPosition);
                break;
        }
    }

    private void SpawnPickup(GameObject prefab, Vector3 position)
    {
        NetworkObject pickup = PoolManager.Instance.GetNetworkObject(prefab);
        if (pickup)
        {
            pickup.transform.position = position;
            if (!SceneHandler.Instance.IsLocalGame)
                pickup.Spawn(true);
        }
    }

    void SpawnPickupLocal(int randomPickup)
    {
        Bounds worldBounds = bounds[CurrentWorld].bounds;
        float x = Random.Range(worldBounds.min.x, worldBounds.max.x);
        float y = Random.Range(worldBounds.min.y, worldBounds.max.y);
        float z = Random.Range(worldBounds.min.z, worldBounds.max.z);
        if (y < 0)
            y *= -1;
        Vector3 spawnPosition = new Vector3(x, y, z);

        switch (randomPickup)
        {
            case 0:
                SpawnPickup(pickupBuffPrefab, spawnPosition);
                break;

            case 1:
                SpawnPickup(pickupDebuffPrefab, spawnPosition);
                break;

            case 2:
                SpawnPickup(pickupHealthPrefab, spawnPosition);
                break;

            case 3:
                SpawnPickup(pickupDamageBuffPrefab, spawnPosition);
                break;

            case 4:
                SpawnPickup(pickupSpeedPrefab, spawnPosition);
                break;
        }
    }

    void ReturnPooledObjects()
    {
        NetworkObject[] activeObjects = FindObjectsOfType<NetworkObject>();

        foreach (NetworkObject obj in activeObjects)
        {
            string objName = obj.gameObject.name.Replace("(Clone)", "").Trim();

            if (objName == pickupBuffPrefab.name ||
                objName == pickupDebuffPrefab.name ||
                objName == pickupHealthPrefab.name ||
                objName == pickupDamageBuffPrefab.name)
            {
                if (obj.IsSpawned)
                {
                    if (!SceneHandler.Instance.IsLocalGame && NetworkManager.Singleton.IsServer)
                    {
                        obj.Despawn(false);
                    }

                    PoolManager.Instance.ReturnNetworkObject(obj);
                }
            }
        }
    }

    void UpdateInput()
    {
        //if (lastControlScheme != playerInput.currentControlScheme)
        //{
        //    lastControlScheme = playerInput.currentControlScheme;
        //    Debug.Log("Switched to: " + lastControlScheme);
        //}
    }
}
