using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
public class UINetworkTickRate : NetworkBehaviour
{
    TextMeshProUGUI text;
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            text.text = "Tick Rate: " + NetworkManager.Singleton.NetworkConfig.TickRate.ToString();
        }
        else
        {
            text.text = "NetworkManager not found!";
        }

    }
}
