using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public sealed class PickupBuff : Pickup
{
    [SerializeField] private int MaxBuff = 10;
    [SerializeField] private int MinBuff = 1;

    public override void TrueStart()
    {
        FallingSpeed = 2;
        DespawnTimer = 0;
        MaxDespawnTimer = 10;
    }

    private void Start()
    {
        TrueStart();
    }
    private void Update()
    {
        if(TimesUp())
        {
            NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
            ReturnToPool(netObj);
        }

        Vector3 position = transform.position;
        Falling(ref position);
        transform.position = position;
    }
    private void OnTriggerEnter(Collider other) => HandleCollision(other);
    private void OnTriggerStay(Collider other) => HandleCollision(other);
    private void OnCollisionEnter(Collision other) => HandleCollision(other);
    private void OnCollisionStay(Collision other) => HandleCollision(other);

    protected override void OnTriggerPlayer(GameObject other)
    {
        int buff = Random.Range(MinBuff, MaxBuff);
        other.gameObject.GetComponent<Stats>().MaxHP.Value += buff;


        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
        NetworkObject net = other.GetComponent<NetworkObject>();
        if(net.IsOwner)
            AudioManager.Instance.PlaySound(eSound.PickupBuff);
        ReturnToPool(netObj);
    }
}
