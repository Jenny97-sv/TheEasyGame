using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Burst.CompilerServices;
using System;
using UnityEngine.EventSystems;

public enum NetworkType
{
    eHost,
    eClient,
    eServer,
}

public enum MenuState
{
    MainMenu,
    OnlineMenu,
    CouchMenu,
    PlayerSelection,
    WaitingForPlayers,
    None
}


public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton = null;
    [SerializeField] private Button joinButton = null;

    [SerializeField] private Button onlineButton = null;
    [SerializeField] private Button couchButton = null;
    [SerializeField] private Button quitButton = null;

    [SerializeField] private Button onePlayersButton = null;
    [SerializeField] private Button twoPlayersButton = null;
    [SerializeField] private Button threePlayersButton = null;
    [SerializeField] private Button fourPlayersButton = null;

    [SerializeField] private Button backButton = null;

    [SerializeField] private TextMeshProUGUI waitingForOtherPlayersText = null;
    [SerializeField] private GameObject[] playerCountButtons = null;
    [SerializeField] private GameObject backGround = null;

    [SerializeField] private Slider allSlider = null;
    [SerializeField] private Slider musicSlider = null;
    [SerializeField] private Slider SFXSlider = null;

    [HideInInspector] public int PlayerCount = 0;

    private Stack<MenuState> menuHistory = new Stack<MenuState>();
    private MenuState currentMenu;

    private GameObject current = null;
    private bool switchedMenu = false;

    private void Awake()
    {
        for (int i = 0; i < playerCountButtons.Length; i++)
        {
            playerCountButtons[i].SetActive(false);
        }
        waitingForOtherPlayersText.gameObject.SetActive(false);

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject != null)
            {
                Hide();
                return;
            }
        }

#if !UNITY_EDITOR
        Cursor.visible = true;
        Screen.lockCursor = false;
#endif

        if (AudioManager.Instance.GetPitch(eMusic.Music) != 1)
            AudioManager.Instance.SetPitch(eMusic.Music, 1);
        AudioManager.Instance.PlayMusic(eMusic.Music);
        AudioManager.Instance.SetParameter(eMusic.Music, 0);
        AudioManager.Instance.SetPlayerSFXVolume(0);
        PlayerInputManager.instance.DisableJoining();

        hostButton.onClick.AddListener(OnHostButtonPressed);
        joinButton.onClick.AddListener(OnJoinButtonPressed);
        backButton.onClick.AddListener(OnBackButtonPressed);
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);

        onlineButton.onClick.AddListener(OnOnlineButtonPressed);
        couchButton.onClick.AddListener(OnCouchButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);

        onePlayersButton.onClick.AddListener(() => SelectPlayerCount(1));
        twoPlayersButton.onClick.AddListener(() => SelectPlayerCount(2));
        threePlayersButton.onClick.AddListener(() => SelectPlayerCount(3));
        fourPlayersButton.onClick.AddListener(() => SelectPlayerCount(4));

        allSlider.onValueChanged.AddListener(SetAllVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        SFXSlider.onValueChanged.AddListener(SetSFXVolume);

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        EventSystem.current.SetSelectedGameObject(onlineButton.gameObject);
        current = EventSystem.current.currentSelectedGameObject;
    }

    public void Start()
    {
        for (int i = 0; i < playerCountButtons.Length; i++)
        {
            playerCountButtons[i].SetActive(false);
        }
        waitingForOtherPlayersText.gameObject.SetActive(false);

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject)
            {
                Hide();
                return;
            }
        }

        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void Update()
    {
        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != null && selected != current) // Only play if selection changes
            {
                if(!switchedMenu)
                    AudioManager.Instance.PlaySound(eSound.Hover);
                current = selected;
            }
        }

        if(switchedMenu)
        {
            switchedMenu = false;
        }
    }

    private void SetAllVolume(float value)
    {
        AudioManager.Instance.SetVolume(value);
    }

    private void SetMusicVolume(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }
    private void SetSFXVolume(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void SetMenuState(MenuState newState)
    {
        if (newState == currentMenu)
            return;

        if (menuHistory.Count == 0 || menuHistory.Peek() != currentMenu)
        {
            menuHistory.Push(currentMenu);
        }
        switchedMenu = true;
        currentMenu = newState;

        HideAllUI();

        switch (newState)
        {
            case MenuState.MainMenu:
                onlineButton.gameObject.SetActive(true);
                couchButton.gameObject.SetActive(true);
                quitButton.gameObject.SetActive(true);
                break;

            case MenuState.OnlineMenu:
                hostButton.gameObject.SetActive(true);
                joinButton.gameObject.SetActive(true);
                backButton.gameObject.SetActive(true);
                break;

            case MenuState.CouchMenu:
                ShowPlayerSelection();
                break;

            case MenuState.PlayerSelection:
                ShowPlayerSelection();
                backButton.gameObject.SetActive(true);
                break;

            case MenuState.WaitingForPlayers:
                waitingForOtherPlayersText.gameObject.SetActive(true);
                waitingForOtherPlayersText.text = "Waiting for other players!";
                break;
        }
    }

    private void HideAllUI()
    {
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        onlineButton.gameObject.SetActive(false);
        couchButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        waitingForOtherPlayersText.gameObject.SetActive(false);

        foreach (var btn in playerCountButtons)
        {
            btn.SetActive(false);
        }
    }



    private void ShowPlayerSelection()
    {
        foreach (var btn in playerCountButtons)
        {
            btn.SetActive(true);
        }
        backButton.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(playerCountButtons[0].gameObject);
    }

    private void OnHostButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);

        SetMenuState(MenuState.PlayerSelection);
    }
    private void OnJoinButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);
    }


    private void OnBackButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);
        if (menuHistory.Count > 0)
        {
            if (currentMenu == MenuState.OnlineMenu) // Hardcoded, but only thing I found that worked....
            {
                menuHistory.Pop();
                SetMenuState(MenuState.MainMenu);
                EventSystem.current.SetSelectedGameObject(onlineButton.gameObject);
            }
            else
            {
                SetMenuState(menuHistory.Pop());
                if (SceneHandler.Instance.IsLocalGame)
                    EventSystem.current.SetSelectedGameObject(couchButton.gameObject);
                else
                {
                    EventSystem.current.SetSelectedGameObject(onlineButton.gameObject);
                    Relay.Instance.LeaveRelay(); // Maybe check if it's loged in?
                }
            }
        }
        else
        {
            Debug.Log("Back button shouldn't exist...");
        }
    }

    private void OnOnlineButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);

        SceneHandler.Instance.IsLocalGame = false;
        SetMenuState(MenuState.OnlineMenu);
        EventSystem.current.SetSelectedGameObject(hostButton.gameObject);
    }

    private void OnCouchButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);

        SceneHandler.Instance.IsLocalGame = true;
        SetMenuState(MenuState.CouchMenu);
        EventSystem.current.SetSelectedGameObject(playerCountButtons[0].gameObject);
    }

    private void HideFirstButtons()
    {
        onlineButton.gameObject.SetActive(false);
        couchButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);

        backButton.gameObject.SetActive(true);
    }


    private void OnQuitButtonPressed()
    {
        AudioManager.Instance.PlaySound(eSound.Click);

        Application.Quit();
    }

    private void SelectPlayerCount(int count)
    {
        AudioManager.Instance.PlaySound(eSound.Click);

        PlayerCount = count;

        if (!SceneHandler.Instance.IsLocalGame)
        {
            //NetworkManager.Singleton.StartHost();
            SetMenuState(MenuState.WaitingForPlayers);
        }
        else
        {
            menuHistory.Clear();
            AudioManager.Instance.SetPlayerSFXVolume(1);
            SceneHandler.Instance.SwitchSceneLocal(SceneName.Scene1, PlayerCount);
        }
    }


    private void Hide()
    {
        HideFirstButtons();

        for (int i = 0; i < playerCountButtons.Length; i++)
        {
            playerCountButtons[i].SetActive(false);
        }
        waitingForOtherPlayersText.gameObject.SetActive(true);
        waitingForOtherPlayersText.text = "Waiting for other players!";

        backGround.GetComponent<UIBackgoundSize>().Hide();
    }

    private void OnDisable()
    {
        hostButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();

        onlineButton.onClick.RemoveAllListeners();
        couchButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        onePlayersButton.onClick.RemoveAllListeners();
        twoPlayersButton.onClick.RemoveAllListeners();
        threePlayersButton.onClick.RemoveAllListeners();
        fourPlayersButton.onClick.RemoveAllListeners();

        backButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {

        if (NetworkManager.Singleton.ConnectedClients.Count == PlayerCount && NetworkManager.Singleton.IsServer)
        {
            AudioManager.Instance.SetPlayerSFXVolume(1);
            menuHistory.Clear();
            waitingForOtherPlayersText.text = "";
            waitingForOtherPlayersText = null;
            SceneHandler.Instance.SwitchScene(SceneName.Scene1, PlayerCount);
        }
    }



    // Input thingies
    private Selectable FindFirstSelectable()
    {
        return GameObject.FindObjectOfType<Selectable>();
    }

    private void MoveSelection(Vector2 direction)
    {
        current = EventSystem.current.currentSelectedGameObject;
        Selectable currentSelectable = current?.GetComponent<Selectable>();

        if (currentSelectable != null)
        {
            Selectable nextSelectable = currentSelectable.FindSelectable(direction);
            if (nextSelectable != null)
            {
                nextSelectable.Select();
            }
        }
    }


    private void OnNavigation(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            Selectable firstSelectable = FindFirstSelectable();
            if (firstSelectable != null)
            {
                firstSelectable.Select();
            }
            return;
        }

        if (inputVector.y > 0) // Up
        {
            MoveSelection(Vector2.up);
        }
        else if (inputVector.y < 0) // Down
        {
            MoveSelection(Vector2.down);
        }
        else if (inputVector.x > 0) // Right
        {
            MoveSelection(Vector2.right);
        }
        else if (inputVector.x < 0) // Left
        {
            MoveSelection(Vector2.left);
        }
    }

    private void OnPress(InputAction.CallbackContext context)
    {

    }

    private void OnBack(InputAction.CallbackContext context)
    {

    }
}