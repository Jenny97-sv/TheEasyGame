using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class PickupSpeed : Pickup
{
    [SerializeField] private float MaxSpeed = 10;

    public override void TrueStart()
    {
        FallingSpeed = 8;
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
        float speed = Random.Range(stats.Speed.Value, MaxSpeed);
        stats.PowerUP(7);
        stats.Speed.Value += speed;
        stats.MaxSpeed.Value = stats.Speed.Value * 2;

        NetworkObject net = other.GetComponent<NetworkObject>();
        if(net.IsOwner)
            AudioManager.Instance.PlaySound(eSound.PickupSpeed);

        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
        ReturnToPool(netObj);
    }
}
