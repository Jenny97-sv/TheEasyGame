using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    protected float FallingSpeed = 3;
    protected float DespawnTimer = 0;
    protected float MaxDespawnTimer = 10;
    //static private LayerMask groundLayer; // Later make array and make several layers work!
    static private LayerMask[] groundLayers = new LayerMask[2];


    private void Awake()
    {
        //groundLayer = LayerMask.GetMask("Ground");
        groundLayers[0] = LayerMask.GetMask("Ground");
        groundLayers[1] = LayerMask.GetMask("Obstacle");
    }

    public abstract void TrueStart();
    protected void HandleCollision(Collider other)
    {
        switch (other.gameObject.tag)
        {
            case "Player":
                OnTriggerPlayer(other.gameObject);
                break;
        }
    }
    protected void HandleCollision(Collision other)
    {
        switch (other.gameObject.tag)
        {
            case "Player":
                OnTriggerPlayer(other.gameObject);
                break;
        }
    }

    protected abstract void OnTriggerPlayer(GameObject other);
    //protected abstract void OnExitPlayer(GameObject other);

    protected void Falling(ref Vector3 position)
    {
        if (!IsGrounded(position))
        {
            position += Vector3.down * FallingSpeed * Time.deltaTime;
        }
    }
    private bool IsGrounded(Vector3 position)
    {
        float groundCheckRadius = 0.3f;
        float groundCheckDistance = 0.2f;
        Vector3 sphereOrigin = position + Vector3.up * 0.1f;

        bool grounded = false;
        foreach (var layer in groundLayers)
        {
            grounded = Physics.SphereCast(sphereOrigin, groundCheckRadius, Vector3.down,
                                       out RaycastHit hit, groundCheckDistance, layer); 
            if(grounded)
                return true;
        }
        return grounded;
    }

    protected bool TimesUp()
    {
        DespawnTimer += Time.deltaTime;
        if (DespawnTimer >= MaxDespawnTimer)
        {
            DespawnTimer = 0;
            return true;
        }
        return false;
    }

    protected void ReturnToPool(NetworkObject netObject)
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            if(netObject)
            {
                //netObject.Despawn(false);
                PoolManager.Instance.ReturnNetworkObject(netObject);
            }
        }
        else
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (netObject)
                {
                    netObject.Despawn(false);
                    PoolManager.Instance.ReturnNetworkObject(netObject);
                }
            }
        }
    }
}
