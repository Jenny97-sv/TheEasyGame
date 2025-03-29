using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class PickupHealth : Pickup
{
    [SerializeField] private int MinHP = 1;
    [SerializeField] private int MaxHP = 5;
    public override void TrueStart()
    {
        FallingSpeed = 1;
        DespawnTimer = 0;
        MaxDespawnTimer = 6;
    }
    void Start()
    {
        TrueStart();
    }

    void Update()
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
        int buff = Random.Range(MinHP, MaxHP);
        other.gameObject.GetComponent<Stats>().Heal(buff);

        NetworkObject net = other.GetComponent<NetworkObject>();
        if (net.IsOwner)
            AudioManager.Instance.PlaySound(eSound.PickupHealth);

        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();  
        ReturnToPool(netObj);
    }
}
