using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    private float initialSpeed = 50f;
    private Vector3 Velocity = Vector3.zero;
    [HideInInspector] public NetworkObject Host = null;

    private void Start()
    {
        StartBullet();
    }
    public void StartBullet()
    {
        Velocity = transform.forward * initialSpeed;
    }

    private void Update()
    {
        transform.position += Velocity.normalized * initialSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {

        switch (collision.gameObject.tag)
        {
            case "Player":
                if (SceneHandler.Instance.IsLocalGame)
                {
                    int id = collision.gameObject.GetComponent<Stats>().ID.Value;
                    Stats stats = Host.GetComponent<Stats>();
                    int damage = Random.Range(stats.Damage.Value, stats.MaxDamage.Value);
                    ApplyDamageLocal(id, damage);
                }
                else
                {
                    ulong targetClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
                    int damage = Random.Range(Host.GetComponent<Stats>().Damage.Value, Host.GetComponent<Stats>().MaxDamage.Value);
                    ApplyDamageServerRpc(targetClientId, damage);
                }
                break;
        }

        HandleCollision();
    }
    private void OnCollisionStay(Collision collision) => HandleCollision();
    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Player":
                if (SceneHandler.Instance.IsLocalGame)
                {
                    int id = other.GetComponent<Stats>().ID.Value;
                    Stats stats = Host.GetComponent<Stats>();
                    int damage = Random.Range(stats.Damage.Value, stats.MaxDamage.Value);
                    ApplyDamageLocal(id, damage);
                }
                else
                {
                    ulong targetClientId = other.GetComponent<NetworkObject>().OwnerClientId;
                    int damage = Random.Range(Host.GetComponent<Stats>().Damage.Value, Host.GetComponent<Stats>().MaxDamage.Value);
                    ApplyDamageServerRpc(targetClientId, damage);
                }
                break;
        }

        HandleCollision();
    }
    private void OnTriggerStay(Collider other) => HandleCollision();


    private void ApplyDamageLocal(int Id, int damage)
    {
        if (GameManager.Instance)
        {
            GameObject player = GameManager.Instance.GetPlayerGameObjectByIndex(Id);
            player.GetComponent<Stats>().TakeDamage(damage);
        }
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(ulong targetClientId, int damage)
    {
        var targetPlayer = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);
        if (targetPlayer != null)
        {
            targetPlayer.GetComponent<Stats>().TakeDamage(damage);
        }
    }

    private void HandleCollision()
    {
        var networkObject = GetComponent<NetworkObject>();
        if (SceneHandler.Instance.IsLocalGame)
        {
            ReturnToPool(networkObject);
        }
        else if (networkObject != null && networkObject.IsOwner && networkObject.IsSpawned)
        {
            RequestDespawnServerRpc();
        }
    }

    private void ReturnToPool(NetworkObject obj)
    {
        PoolManager.Instance.ReturnNetworkObject(obj);
    }


    [ServerRpc(RequireOwnership = true)]
    private void RequestDespawnServerRpc()
    {
        ReturnToPoolServerRpc();
    }

    [ServerRpc]
    private void ReturnToPoolServerRpc()
    {
        ReturnToPoolClientRpc();

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(false);
            PoolManager.Instance.ReturnNetworkObject(networkObject);
        }
    }

    [ClientRpc]
    private void ReturnToPoolClientRpc()
    {
        // Clients can prepare the object for pooling if needed
        // For example, reset any client-side effects or states
    }
}
