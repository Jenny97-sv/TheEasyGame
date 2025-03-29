using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;


using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using TMPro;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem;
using System.Globalization;


public class Relay : MonoBehaviour
{
    public static Relay Instance = null;
    [SerializeField] private UnityTransport m_UnityTransport;
    public TMP_Text m_JoinCode;
    [SerializeField] private TMP_Text WaitingForOtherText;
    public TMP_InputField m_InputField;
    [HideInInspector] public bool clientConnected = false;


    public void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void InitalizeRelay()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            //Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allication = await RelayService.Instance.CreateAllocationAsync(3);
            m_JoinCode.gameObject.SetActive(true);
            string joincode = await RelayService.Instance.GetJoinCodeAsync(allication.AllocationId);
            m_JoinCode.text = joincode;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allication.RelayServer.IpV4,
                (ushort)allication.RelayServer.Port,
                allication.AllocationIdBytes,
                allication.Key,
                allication.ConnectionData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void JoinInput()
    {
        JoinRelay(m_InputField.text);
    }


    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();

            clientConnected = true;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    //private void AssignControllerToLocalPlayer()
    //{
    //    if (!NetworkManager.Singleton.IsClient) return; // Only assign controller for the local player

    //    if (Gamepad.all.Count > 0 && PlayerInput.all.Count > 0)
    //    {
    //        PlayerInput player = PlayerInput.all[0]; // Get the first player
    //        InputUser.PerformPairingWithDevice(Gamepad.all[0], player.user); // Assign the first controller
    //        Debug.Log($"Assigned {Gamepad.all[0].name} to player {player.gameObject.name}");
    //    }
    //}
    public void LeaveRelay()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            //Debug.Log("Player signed out. " + AuthenticationService.Instance.PlayerId);
        }

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Host shut down the server.");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Client disconnected.");
        }

        clientConnected = false;
    }

}
