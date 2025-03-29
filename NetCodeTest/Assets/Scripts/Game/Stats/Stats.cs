using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class Stats : NetworkBehaviour
{
    public Stats LoadStats(Stats stats)
    {
        if (stats == null)
        {
            Debug.LogError("LoadStats() received a null Stats object!");
            return null; // Prevents a crash
        }
        return new Stats
        {
            ID = stats.ID,
            HP = stats.HP,
            MaxHP = stats.MaxHP,
            Damage = stats.Damage,
            MaxDamage = stats.MaxDamage,

            DefaultHP = stats.DefaultHP,
            DefaultMaxHP = stats.DefaultMaxHP,
            DefaultDamage = stats.DefaultDamage,
            DefaultMaxDamage = stats.DefaultMaxDamage,

            IsWinner = stats.IsWinner,
            IsReady = stats.IsReady,
            myScore = stats.myScore,
            myIsBadASS = stats.myIsBadASS,

            myPowerUpTimer = stats.myPowerUpTimer,
            myMaxPowerUpTimer = stats.myMaxPowerUpTimer,
            myIsPoweredUp = stats.myIsPoweredUp
        };
    }
    [HideInInspector] public NetworkVariable<int> ID = new NetworkVariable<int>(0);

    [HideInInspector] public NetworkVariable<int> HP = new NetworkVariable<int>(10);
    [HideInInspector] public NetworkVariable<int> MaxHP = new NetworkVariable<int>(10);
    [HideInInspector] public NetworkVariable<int> Damage = new NetworkVariable<int>(2);
    [HideInInspector] public NetworkVariable<int> MaxDamage = new NetworkVariable<int>(4);
    [HideInInspector] public NetworkVariable<float> Speed = new NetworkVariable<float>(5);
    [HideInInspector] public NetworkVariable<float> MaxSpeed = new NetworkVariable<float>(10);

    public NetworkVariable<int> DefaultHP = new NetworkVariable<int>(10);
    public NetworkVariable<int> DefaultMaxHP = new NetworkVariable<int>(10);
    public NetworkVariable<int> DefaultDamage = new NetworkVariable<int>(2);
    public NetworkVariable<int> DefaultMaxDamage = new NetworkVariable<int>(4);
    public NetworkVariable<float> DefaultSpeed = new NetworkVariable<float>(5);
    public NetworkVariable<float> DefaultMaxSpeed = new NetworkVariable<float>(10);

    [HideInInspector] public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(true);
    [HideInInspector] public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);
    [HideInInspector] public NetworkVariable<int> myScore = new NetworkVariable<int>(0);
    [HideInInspector] public NetworkVariable<bool> myIsBadASS = new NetworkVariable<bool>(false);

    [HideInInspector] private NetworkVariable<float> myPowerUpTimer = new NetworkVariable<float>(0);
    [HideInInspector] private NetworkVariable<float> myMaxPowerUpTimer = new NetworkVariable<float>(6);
    [HideInInspector] public NetworkVariable<bool> myIsPoweredUp = new NetworkVariable<bool>(false);

    public void TakeDamage(int damage)
    {
        if (!SceneHandler.Instance.IsLocalGame && IsServer || SceneHandler.Instance.IsLocalGame)
        {
            HP.Value -= damage;
            if (HP.Value < 0)
            {
                HP.Value = 0;
            }
            else
            {
                if (SceneHandler.Instance.IsLocalGame)
                    AudioManager.Instance.PlaySound(eSound.TakingDamage);
                else
                    PlayHurtSoundClientRPC();
            }
        }
    }

    [ClientRpc]
    private void PlayHurtSoundClientRPC()
    {
        if (IsOwner)
            AudioManager.Instance.PlaySound(eSound.TakingDamage);
    }

    public void Heal(int heal)
    {
        if (!SceneHandler.Instance.IsLocalGame && IsServer || SceneHandler.Instance.IsLocalGame)
        {
            HP.Value += heal;
            if (HP.Value > MaxHP.Value)
            {
                HP.Value = MaxHP.Value;
            }
        }
    }

    public void PowerUP(int maxTimer)
    {
        Debug.Log($"[PowerUP] LocalClientId: {NetworkManager.LocalClientId}");

        //if (!SceneHandler.Instance.IsLocalGame)
        //    SetToDefaultServerRPC();
        //else
        SetToDefault();

        //if (!SceneHandler.Instance.IsLocalGame && IsServer)
        //{
        //    PlayTimerServerRPC(maxTimer);
        //}
        //else if (SceneHandler.Instance.IsLocalGame)
        //{
        Debug.Log("Powering up!");
        myPowerUpTimer.Value = 0;
        myMaxPowerUpTimer.Value = maxTimer;
        myIsPoweredUp.Value = true;
        //Debug.Log("Playing buff timer");
        //if (IsOwner)
        //    AudioManager.Instance.PlayMusic(eMusic.TimeBuffer);
        //}
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayTimerServerRPC(int maxTimer)
    {
        myPowerUpTimer.Value = 0;
        myMaxPowerUpTimer.Value = maxTimer;
        myIsPoweredUp.Value = true;
        AudioManager.Instance.PlayMusic(eMusic.TimeBuffer);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetToDefaultServerRPC()
    {
        HP.Value = DefaultHP.Value;
        MaxHP.Value = DefaultMaxHP.Value;
        Damage.Value = DefaultDamage.Value;
        MaxDamage.Value = DefaultMaxDamage.Value;
        Speed.Value = DefaultSpeed.Value;
        MaxSpeed.Value = DefaultMaxSpeed.Value;
    }

    private void SetToDefault()
    {
        HP.Value = DefaultHP.Value;
        MaxHP.Value = DefaultMaxHP.Value;
        Damage.Value = DefaultDamage.Value;
        MaxDamage.Value = DefaultMaxDamage.Value;
        Speed.Value = DefaultSpeed.Value;
        MaxSpeed.Value = DefaultMaxSpeed.Value;
    }

    private void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame && IsServer || SceneHandler.Instance.IsLocalGame)
        {
            if (myIsPoweredUp.Value)
            {
                //Debug.Log("Is powered up! Timer = " + myPowerUpTimer.Value + ", maxTimer = " + myMaxPowerUpTimer.Value);
                myPowerUpTimer.Value += Time.deltaTime;

                if (myPowerUpTimer.Value >= myMaxPowerUpTimer.Value)
                {
                    myPowerUpTimer.Value = 0;
                    myIsPoweredUp.Value = false;
                    SetToDefault();
                }
            }
        }
    }
}
