using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMusic : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.SetParameter(eMusic.Music, 1);
        //AudioManager.Instance.PlayMusic(eMusic.MainGame);
        AudioManager.Instance.StopSound(eSound.Click);
        AudioManager.Instance.SetPlayerSFXVolume(1);
#if !UNITY_EDITOR
        Cursor.visible = false;
        Screen.lockCursor = true;
#endif
    }
}
