using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PickupDamageBuff : Pickup
{
    [SerializeField] private int MaxDamage = 5;

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
        if (TimesUp())
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
        Stats stats = other.GetComponent<Stats>();
        int damage = Random.Range(stats.MaxDamage.Value + 1, MaxDamage);
        stats.PowerUP(7);
        stats.MaxDamage.Value += damage; // Have to set the powerup after

        NetworkObject net = other.GetComponent<NetworkObject>();
        if (net.IsOwner)
            AudioManager.Instance.PlaySound(eSound.PickupDamageBuff);

        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
        ReturnToPool(netObj);
    }
}
