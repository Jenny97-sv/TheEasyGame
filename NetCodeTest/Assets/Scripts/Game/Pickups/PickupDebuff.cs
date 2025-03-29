using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class PickupDebuff : Pickup
{
    [SerializeField] private int MaxDebuff = 5;
    [SerializeField] private int MinDebuff = 1;

    public override void TrueStart()
    {
        FallingSpeed = 3;
        DespawnTimer = 0;
        MaxDespawnTimer = 15;
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
        int debuff = Random.Range(MinDebuff, MaxDebuff);
        Stats stats = other.GetComponent<Stats>();
        stats.MaxHP.Value -= debuff;
        if (stats.MaxHP.Value < 0)
        {
            stats.MaxHP.Value = 0;
        }
        else if (stats.HP.Value > stats.MaxHP.Value)
        {
            stats.HP.Value = stats.MaxHP.Value;
        }
        NetworkObject net = other.GetComponent<NetworkObject>();
        if(net.IsOwner)
            AudioManager.Instance.PlaySound(eSound.PickupDebuff);

        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
        ReturnToPool(netObj);
    }
}
