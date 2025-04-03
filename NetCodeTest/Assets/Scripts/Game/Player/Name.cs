//using System;
//using TMPro;
//using Unity.Netcode;
//using UnityEngine;

//public class PlayerName : NetworkBehaviour
//{
//    [SerializeField] private SerializeText[] startName;
//    [SerializeField] private Transform cameraTransform;
//    private Stats stats;
//    private int nameIndex = -1;
//    private string randomName;

//    public override void OnNetworkSpawn()
//    {
//        stats = GetComponent<Stats>();
//        if (IsOwner)
//        {
//            nameIndex = UnityEngine.Random.Range(0, startName.Length);
//            randomName = startName[nameIndex].text;

//            string name;
//            if (SaveLoadManager.Instance.playerNames.Count <= (int)OwnerClientId)
//            {
//                name = randomName;
//            }
//            else
//            {
//                name = SaveLoadManager.Instance.playerNames[(int)OwnerClientId];
//            }
//            NameServerRPC(name);
//            //NameServerRPC(SaveLoadManager.Instance.playerNames[(int)OwnerClientId]);
//        }
//    }

//    [ServerRpc]
//    public void NameServerRPC(string name)
//    {
//        stats.myName.Value = name;
//        NameClientRPC(name, OwnerClientId);
//    }

//    [ClientRpc]
//    private void NameClientRPC(string name, ulong id)
//    {
//        if (id == OwnerClientId)
//        {
//            stats.myName.Value = name;
//            //if (PlayerPrefs.HasKey("PlayerName"))
//            //{
//            //}
//            //else
//            //{
//            //    stats.myName.Value = randomName;
//            //}

//        }
//    }
//}
