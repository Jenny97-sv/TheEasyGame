using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AudioListenerManager : NetworkBehaviour
{
    private AudioListener audioListener;

    void Start()
    {
        audioListener = GetComponent<AudioListener>();
        if (SceneHandler.Instance.IsLocalGame)
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();

            foreach (AudioListener listener in listeners)
            {
                if (listener)
                    listener.enabled = false;
            }

            Transform yo = transform.Find("TempCam");
            if(yo)
                yo.GetComponent<AudioListener>().enabled = true;
        }
        else
        {
            if (!IsOwner)
            {
                if(audioListener)
                    audioListener.enabled = false;
            }

            Transform listener = transform.Find("TempCam");
            if(listener)
                listener.GetComponent<AudioListener>().enabled = false;
        }
    }
}
