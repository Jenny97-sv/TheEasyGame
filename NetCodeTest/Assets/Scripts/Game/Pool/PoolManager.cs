using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance = null;

    // Use prefab names as keys instead of instance IDs
    private Dictionary<string, ObjectPool> networkObjectPools = new Dictionary<string, ObjectPool>();
    private Dictionary<string, GameObject> prefabReferences = new Dictionary<string, GameObject>();

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

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }


    private void OnSceneUnloaded(Scene arg0)
    {
        DestroyAllObjects();
    }

    public void Register(GameObject prefab, int initialPoolSize)
    {
        string prefabKey = prefab.name;

        networkObjectPools[prefabKey] = new ObjectPool(prefab, initialPoolSize);
        prefabReferences[prefabKey] = prefab;
    }

    public bool IsRegistered(GameObject prefab)
    {
        return networkObjectPools.ContainsKey(prefab.name);
    }

    public NetworkObject GetNetworkObject(GameObject prefab)
    {
        string prefabKey = prefab.name;
        if (networkObjectPools.ContainsKey(prefabKey))
        {
            return networkObjectPools[prefabKey].GetObject();
        }
        else
        {
            //Debug.LogError($"Prefab {prefab.name} not registered with PoolManager!");
            return null;
        }
    }

    public void ReturnNetworkObject(NetworkObject obj)
    {
        GameObject originalPrefab = FindOriginalPrefab(obj.gameObject);
        if (originalPrefab != null)
        {
            string prefabKey = originalPrefab.name;
            if (networkObjectPools.ContainsKey(prefabKey))
            {
                networkObjectPools[prefabKey].ReturnObject(obj);
            }
            else
            {
                Debug.LogError($"No pool found for NetworkObject {obj.name}!");
            }
        }
        else
        {
            Debug.LogError($"Could not find original prefab for NetworkObject {obj.name}!");
        }
    }

    public void DestroyNetworkObject(NetworkObject obj)
    {
        GameObject originalPrefab = FindOriginalPrefab(obj.gameObject);
        if (originalPrefab)
        {
            string prefabKey = originalPrefab.name;
            if (networkObjectPools.ContainsKey(prefabKey))
            {
                Destroy(obj.gameObject);
            }
            else
            {
                Debug.LogError($"No pool found for NetworkObject {obj.name}!");
            }
        }
        else
        {
            Debug.LogError($"Could not find original prefab for NetworkObject {obj.name}!");
        }
    }

    public void DestroyAllObjects()
    {
        List<string> keys = new List<string>(networkObjectPools.Keys);

        foreach (string key in keys)
        {
            networkObjectPools[key].ClearPool(); 
        }

        networkObjectPools.Clear(); 
    }

    private GameObject FindOriginalPrefab(GameObject clone)
    {
        string originalName = clone.name.Replace("(Clone)", "").Trim();
        foreach (var prefab in prefabReferences.Values)
        {
            if (prefab.name == originalName)
            {
                return prefab;
            }
        }
        return null;
    }

    // Optional: Add a method to see what's in your pools for debugging
    public void PrintRegisteredPools()
    {
        Debug.Log($"Registered pools: {networkObjectPools.Count}");
        foreach (var key in networkObjectPools.Keys)
        {
            Debug.Log($"- {key}");
        }
    }
}