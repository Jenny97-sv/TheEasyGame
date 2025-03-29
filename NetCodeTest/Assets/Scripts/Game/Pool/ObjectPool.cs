using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectPool
{
    private Queue<NetworkObject> objectPool = new Queue<NetworkObject>();
    private GameObject prefab = null;
    private Transform poolContainer = null;

    public ObjectPool(GameObject prefab, int initialSize)
    {
        this.prefab = prefab;
        poolContainer = new GameObject($"Pool_{prefab.name}").transform;
        Object.DontDestroyOnLoad(poolContainer.gameObject);

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private void CreateNewObject()
    {
        NetworkObject newObj = Object.Instantiate(prefab).GetComponent<NetworkObject>();
        newObj.gameObject.SetActive(false);

        objectPool.Enqueue(newObj);
    }

    public NetworkObject GetObject()
    {
        if (objectPool.Count > 0)
        {
            NetworkObject obj = objectPool.Dequeue();
            if (obj != null && obj.gameObject != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }
        return null;
    }

    public void ReturnObject(NetworkObject obj)
    {
        if (obj != null && obj.gameObject != null)
        {
            obj.gameObject.SetActive(false);

            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            objectPool.Enqueue(obj);
        }
    }

    public void ClearPool()
    {
        while (objectPool.Count > 0)
        {
            NetworkObject obj = objectPool.Dequeue();
            if (obj != null && obj.gameObject != null)
            {
                Debug.Log("Destroying " + obj.name);
                Object.Destroy(obj.gameObject);
            }
        }

        if (poolContainer != null)
        {
            Object.Destroy(poolContainer.gameObject);
        }
    }
}