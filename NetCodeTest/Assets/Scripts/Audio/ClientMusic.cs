using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class ClientMusic : NetworkBehaviour
{
    void Start()
    {
        if (SceneHandler.Instance.IsLocalGame)
            return;

        PlayMusicForScene(SceneHandler.Instance.sceneName.Value.ToString());
    }

    private void PlayMusicForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Menu":
                AudioManager.Instance.SetParameter(eMusic.Music, 0);
                SceneHandler.Instance.sceneName.Value = SceneName.Menu;
                break;

            case "Scene1":
                AudioManager.Instance.SetParameter(eMusic.Music, 1);
                SceneHandler.Instance.sceneName.Value = SceneName.Scene1;
                break;

            case "Scene2":
                //AudioManager.Instance.PlayMusic(eMusic.Menu);
                SceneHandler.Instance.sceneName.Value = SceneName.Scene2;
                break;
        }
    }
}
