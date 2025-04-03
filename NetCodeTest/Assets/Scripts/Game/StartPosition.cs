using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum ePlayer
{
    ePlayer1,
    ePlayer2,
    ePlayer3,
    ePlayer4
}

[System.Serializable]
public class BetterPosition
{
    public GameObject startPosition = null;
    public bool IsDebug = false;
    public ePlayer player = ePlayer.ePlayer1;
}

[System.Serializable]
public class WorldSpawnPositions
{
    public int worldIndex;
    public List<BetterPosition> startPositions;
}

public class StartPosition : MonoBehaviour
{
    //private float distance = 100;
    public bool IsDebug = false;
    public int CurrentWorld = 0;
    [SerializeField] private List<WorldSpawnPositions> worldSpawnPositions = new();

    public Transform BetterStartPosition(ulong clientId)
    {
        WorldSpawnPositions worldData = worldSpawnPositions.Find(w => w.worldIndex == CurrentWorld);

        if (worldData == null || worldData.startPositions.Count == 0)
        {
            Debug.LogWarning($"No spawn points found for world {CurrentWorld}, using default.");
            return CreateDefaultTransform();
        }

        foreach (var position in worldData.startPositions)
        {
            if (position.startPosition != null && (ulong)position.player == clientId)
            {
                return position.startPosition.transform;
            }
        }

        return worldData.startPositions[0].startPosition.transform;
    }

    private Transform CreateDefaultTransform()
    {
        GameObject tempObject = new GameObject("DefaultSpawn");
        tempObject.transform.rotation = Quaternion.identity;
        switch (CurrentWorld)
        {
            case 0:
                tempObject.transform.position = Vector3.zero;
                return tempObject.transform;

            case 1:
                tempObject.transform.position = new Vector3(500, 5, 500);
                return tempObject.transform;
        }

        tempObject.transform.position = Vector3.zero;
        return tempObject.transform;
    }

    // THIS IS FUCKIN UGLY!!!!!!
    //private void Update()
    //{
    //    if (NetworkManager.Singleton.IsServer)
    //    {
    //        Vector3 prevPos = Vector3.zero;

    //        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
    //        {
    //            if (client.PlayerObject != null) // Ensure the player object exists
    //            {
    //                Transform startTransform = BetterStartPosition(client.ClientId);

    //                if ((client.PlayerObject.transform.position - prevPos).magnitude > distance)
    //                {
    //                    client.PlayerObject.transform.position = startTransform.position;
    //                    client.PlayerObject.transform.rotation = startTransform.rotation;
    //                }

    //                prevPos = client.PlayerObject.transform.position; // Update prevPos for next iteration
    //            }
    //        }
    //    }

    //}

}
