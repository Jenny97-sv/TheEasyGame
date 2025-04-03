using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Security.Cryptography.X509Certificates;
public class NameInput : NetworkBehaviour
{
    private TMP_InputField input;

    private void Start()
    {
        input = GetComponent<TMP_InputField>();
        input.onEndEdit.AddListener(SaveName);
    }

    private void OnDisable()
    {
        input.onEndEdit.RemoveListener(SaveName);
    }

    private void SaveName(string name)
    {
        PlayerPrefs.SetString("PlayerName", name); // Save the name locally
        PlayerPrefs.Save();
        //AddNameServerRPC(name);
    }

    //[ServerRpc]
    //private void AddNameServerRPC(string name)
    //{
    //    SaveLoadManager.Instance.playerNames.Add(name);
    //}
}
