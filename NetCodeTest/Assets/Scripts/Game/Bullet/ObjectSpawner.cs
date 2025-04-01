using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab = null;

    private const int maxOverSpawns = 3;
    private const int maxForwardSpawns = 1;
    private const int maxBulletSpawns = 3;

    private int debugTest = 0;

    [SerializeField] private InputActionAsset actionAsset = null;
    private InputActionMap game = null;
    private InputAction shoot = null;

    void Start()
    {
        if (SceneHandler.Instance.sceneName.Value == SceneName.Scene1) // Shitty, but that's what I got
        {
            if (SceneHandler.Instance.IsLocalGame || !SceneHandler.Instance.IsLocalGame && IsOwner)
                PoolManager.Instance.Register(bulletPrefab, maxBulletSpawns);
        }
    }

    private void OnEnable()
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            actionAsset = GetComponent<PlayerInput>().actions;
            game = actionAsset.FindActionMap("Game");
            shoot = game.FindAction("Shoot");
            if (shoot != null)
            {
                Debug.Log("Debugtest = " + debugTest);
                debugTest++;
                shoot.started -= OnShoot;
                shoot.started += OnShoot;
            }
        }
        else
        {
            //if (IsOwner)
            //{
            InputHandler.Instance.shootAction.started -= OnShoot;
            InputHandler.Instance.shootAction.started += OnShoot;
            //}
        }
    }

    private void OnDisable()
    {
        ReturnObjectsToPool();
        //if (SceneHandler.Instance.sceneName.Value == SceneName.Scene1) // Shitty, but that's what I got
        //{
        if (SceneHandler.Instance.IsLocalGame)
        {
            if(shoot != null)
                shoot.started -= OnShoot;
        }
        else
        {
            if (IsOwner)
                InputHandler.Instance.shootAction.started -= OnShoot;
        }
        //}

    }

    private void ReturnObjectsToPool()
    {
        NetworkObject[] activeObjects = FindObjectsOfType<NetworkObject>();

        foreach (NetworkObject obj in activeObjects)
        {
            string objName = obj.gameObject.name.Replace("(Clone)", "").Trim();

            if (objName == bulletPrefab.name)
            {
                if (obj.IsSpawned)
                {
                    if (!SceneHandler.Instance.IsLocalGame && NetworkManager.Singleton.IsServer)
                    {
                        obj.Despawn(false);
                    }

                    PoolManager.Instance.ReturnNetworkObject(obj);
                    PoolManager.Instance.DestroyNetworkObject(obj);
                }
            }
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        Debug.Log("Debugtest = " + debugTest);
        debugTest++;
        if (!GetComponent<Stats>().IsWinner.Value)
            return;
        Vector3 shootPosition = new Vector3(
            transform.position.x + GetLocalCamera().transform.forward.x * 2,
            transform.position.y + GetLocalCamera().transform.forward.y * 2 + 1,
            transform.position.z + GetLocalCamera().transform.forward.z * 2);
        Vector3 shootDirection = GetLocalCamera().transform.forward;

        if (!SceneHandler.Instance.IsLocalGame)
        {
            if (!IsOwner)
                return;
            SpawnBulletServerRpc(shootPosition, shootDirection);
        }
        else
        {
            SpawnBulletLocal(shootPosition, shootDirection);
        }
    }

    private void SpawnBulletLocal(Vector3 position, Vector3 direction)
    {
        NetworkObject bullet = PoolManager.Instance.GetNetworkObject(bulletPrefab);
        if (bullet == null)
        {
            Debug.LogError("Failed to instantiate bullet!");
            return;
        }
        AudioManager.Instance.PlaySound(eSound.Shoot);

        bullet.transform.position = position;
        bullet.transform.forward = direction;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Host = gameObject.GetComponent<NetworkObject>();
            bulletScript.StartBullet();
        }
    }

    [ServerRpc]
    private void SpawnBulletServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default)
    {
        NetworkObject bullet = PoolManager.Instance.GetNetworkObject(bulletPrefab);
        if (bullet != null)
        {
            bullet.transform.position = position;
            bullet.transform.forward = direction;

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.Host = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject;

            bullet.Spawn(true);
            bulletScript.StartBullet();  // Start logic after spawning

            SpawnBulletClientRpc(bullet);
        }
    }

    [ClientRpc]
    private void SpawnBulletClientRpc(NetworkObjectReference bulletRef)
    {
        if (bulletRef.TryGet(out NetworkObject bullet))
        {
            bullet.GetComponent<Bullet>().StartBullet();
            if (IsOwner)
            {
                AudioManager.Instance.PlaySound(eSound.Shoot);
            }
        }
    }



    private Camera GetLocalCamera()
    {
        if (!SceneHandler.Instance.IsLocalGame && IsOwner || SceneHandler.Instance.IsLocalGame)
        {
            return GetComponentInChildren<Camera>();
        }
        return Camera.main;
    }
}
