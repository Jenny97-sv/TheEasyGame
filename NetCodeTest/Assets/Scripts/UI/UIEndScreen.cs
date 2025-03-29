using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Globalization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


[System.Serializable]
public struct EndScreenText
{
    public string text;
    public TextMeshProUGUI textUI;
}


public class UIEndScreen : MonoBehaviour
{
    [SerializeField] private Button quitButton = null;
    [SerializeField] private Button continueButton = null;
    [SerializeField] private TextMeshProUGUI waitingForOthersText = null;
    [SerializeField] private EndScreenText[] loseTexts = null;
    [SerializeField] private EndScreenText[] winTexts = null;
    private PlayerInput playerInput = null;
    private GameObject current = null;
    private bool switchedMenu = false;

    private int currentWinText = -1;
    private int currentLoseText = -1;

    private Stats playerStats = null;

    void Start()
    {
        AudioManager.Instance.SetPitch(eMusic.Music, 1);
        AudioManager.Instance.PlayMusic(eMusic.Music);

        AudioManager.Instance.SetPlayerSFXVolume(0);

        if (SceneHandler.Instance.IsLocalGame)
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.SwitchCurrentActionMap("UI");


            int winnerID = -1;

            foreach (GameObject player in GameManager.Instance.GetPlayers().Keys)
            {
                if (player.GetComponent<Stats>().IsWinner.Value)
                {
                    winnerID = player.GetComponent<Stats>().ID.Value;
                    break;
                }
            }

            waitingForOthersText.text = "Player " + (winnerID + 1) + " won!";

            quitButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            current = EventSystem.current.currentSelectedGameObject;
            winTexts = null;
            loseTexts = null;

            return;
        }

        if (NetworkManager.Singleton.LocalClient != null)
        {
            playerStats = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Stats>();
        }

        if (!playerStats)
        {
            Debug.LogWarning("Stats component not found for local player.");
            return;
        }

        if (playerStats.IsWinner.Value)
        {
            AudioManager.Instance.SetParameter(eMusic.Music, 2);
            currentWinText = Random.Range(0, winTexts.Length);

            winTexts[currentWinText].textUI.gameObject.SetActive(true);
            winTexts[currentWinText].textUI.color = Color.green;
            winTexts[currentWinText].textUI.text = winTexts[currentWinText].text; // ✅ Set text

            foreach (var lose in loseTexts)
            {
                lose.textUI.gameObject.SetActive(false);
            }
        }
        else
        {
            AudioManager.Instance.SetParameter(eMusic.Music, 3);
            AudioManager.Instance.PlayMusic(eMusic.Music);

            foreach (var win in winTexts)
            {
                win.textUI.gameObject.SetActive(false);
            }

            currentLoseText = Random.Range(0, loseTexts.Length);

            loseTexts[currentLoseText].textUI.gameObject.SetActive(true);
            loseTexts[currentLoseText].textUI.color = Color.red;
            loseTexts[currentLoseText].textUI.text = loseTexts[currentLoseText].text; // ✅ Set text
        }


        quitButton.gameObject.SetActive(true);
        if (quitButton)
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        if (NetworkManager.Singleton.IsServer)
        {
            continueButton.gameObject.SetActive(true);
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinue);
            }
            waitingForOthersText.text = "";
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        else
        {
            waitingForOthersText.text = "Waiting for host to dominate your life!";
            waitingForOthersText.color = Color.red;
            continueButton.gameObject.SetActive(false);
            quitButton.transform.position = new Vector2(Screen.width / 2, quitButton.transform.position.y);
            EventSystem.current.SetSelectedGameObject(quitButton.gameObject);
        }
    }

    private void OnEnable()
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            if (quitButton) quitButton.onClick.AddListener(OnQuit);
            if (continueButton) continueButton.onClick.AddListener(OnContinue);
        }

    }
    private void OnDisable()
    {
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
    }

    private void Update()
    {
        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != null && selected != current) // Only play if selection changes
            {
                if (!switchedMenu)
                    AudioManager.Instance.PlaySound(eSound.Hover);
                current = selected;
            }
        }

        if (switchedMenu)
        {
            switchedMenu = false;
        }
    }


    private void OnQuit()
    {
        switchedMenu = true;
        AudioManager.Instance.SetParameter(eSound.Click, 0);
        AudioManager.Instance.PlaySound(eSound.Click);
        if (SceneHandler.Instance.IsLocalGame)
        {
            GameManager.Instance.DestroyPlayers();
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); // Ensure players have the "Player" tag
            foreach (GameObject player in players)
            {
                Destroy(player);
            }
            PoolManager.Instance.DestroyAllObjects();
            SceneHandler.Instance.SwitchSceneLocal(SceneName.Menu, 0); // Delete players?
        }
        else
        {
            Relay.Instance.LeaveRelay();
            QuitServerRpc();
            NetworkManager.Singleton.DisconnectClient(this.gameObject.GetComponent<NetworkObject>().OwnerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void QuitServerRpc()
    {
        NetworkManager.Singleton.Shutdown(); // Scenehandler handles disconnection
        GameManager.Instance.DestroyPlayers();
    }

    [ClientRpc]
    public void SwitchToMenuClientRpc()
    {
        NetworkManager.Singleton.Shutdown();
        SwitchToMenuServerRpc();
    }

    [ServerRpc]
    private void SwitchToMenuServerRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(SceneName.Menu.ToString());
    }


    private void OnContinue()
    {
        switchedMenu = true;

        AudioManager.Instance.SetParameter(eSound.Click, 0);
        AudioManager.Instance.PlaySound(eSound.Click);
        AudioManager.Instance.SetPlayerSFXVolume(1);

        if (SceneHandler.Instance.IsLocalGame)
        {
            StartNextRoundLocal();
        }
        else
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartNextRound();
            }
            else
            {
                SendContinuePressedServerRpc();
            }
        }
    }
    private void StartNextRoundLocal()
    {
        foreach (var player in GameManager.Instance.GetPlayers().Keys)
        {
            Stats stats = player.GetComponent<Stats>();
            stats.Heal(stats.MaxHP.Value);
            stats.IsReady.Value = false;
            stats.IsWinner.Value = true;
        }
        waitingForOthersText.text = "";

        SceneHandler.Instance.SwitchSceneLocal(SceneName.Scene1, SceneHandler.Instance.MaxPlayerCount);

    }


    [ServerRpc(RequireOwnership = false)]
    private void SendContinuePressedServerRpc(ServerRpcParams rpcParams = default)
    {
        StartNextRound();
    }

    private void StartNextRound()
    {
        if (!NetworkManager.Singleton.IsServer) return; // Ensure only the server runs this
        waitingForOthersText.text = "";
        AudioManager.Instance.SetPlayerSFXVolume(1);
        SceneHandler.Instance.SwitchScene(SceneName.Scene1, NetworkManager.Singleton.ConnectedClients.Count);
    }


    [ClientRpc]
    private void ReturnAllToMenuClientRpc()
    {
        StartCoroutine(DisconnectAndReturnToMenu());
    }

    private IEnumerator DisconnectAndReturnToMenu()
    {
        NetworkManager.Singleton.Shutdown();

        yield return new WaitForEndOfFrame();

        SceneManager.LoadScene(SceneName.Menu.ToString());
    }
}

