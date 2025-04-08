using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using static UnityEngine.Rendering.DebugUI;
using Unity.VisualScripting;

public enum eMusic
{
    Music,
    Menu,
    MainGame,
    Win,
    Lose,
    TimeBuffer
}

public enum eSound
{
    Jump,
    Shoot,
    WalkSpeed,
    TakingDamage,
    Death,

    PickupHealth,
    PickupDamageBuff,
    PickupBuff,
    PickupDebuff,
    PickupSpeed,

    UI,
    Click,
    Hover
}

[System.Serializable]
public class Music
{
    public eMusic music;
    public EventReference reference;
}

[System.Serializable]
public class Sound
{
    public eSound sound;
    public EventReference reference;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance = null;

    private VCA musicVCA;
    private VCA sfxVCA;
    private VCA playerSFXVCA;
    private VCA allVCA;

    [SerializeField, TextArea]
    private string Description = "Make sure to ALWAYS name the enum EXACTLY like the FMOD event!";

    [Header("Music Settings")]
    [SerializeField] private Music[] musics;
    private Dictionary<eMusic, EventInstance> musicLookup = new Dictionary<eMusic, EventInstance>();
    private EventInstance currentMusicInstance;

    [Header("Sound Effects")]
    [SerializeField] private Sound[] sounds;
    private Dictionary<eSound, EventInstance> soundLookup = new Dictionary<eSound, EventInstance>();

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log(Description);
            Description = null;

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeMusic();
            InitializeSounds();

            musicVCA = RuntimeManager.GetVCA("vca:/Music");
            sfxVCA = RuntimeManager.GetVCA("vca:/SFX");
            allVCA = RuntimeManager.GetVCA("vca:/All");
            playerSFXVCA = RuntimeManager.GetVCA("vca:/PlayerSFX");

            SetVolume(0.2f);
        }
        else
        {
            Debug.Log("Destroying instance");
            Destroy(gameObject);
        }
    }

    private void InitializeMusic()
    {
        foreach (Music music in musics)
        {
            if (!musicLookup.ContainsKey(music.music))
            {
                EventInstance instance = RuntimeManager.CreateInstance(music.reference);
                musicLookup.Add(music.music, instance);
            }
        }
    }

    private void InitializeSounds()
    {
        foreach (Sound sound in sounds)
        {
            if (!soundLookup.ContainsKey(sound.sound))
            {
                EventInstance instance = RuntimeManager.CreateInstance(sound.reference);
                soundLookup.Add(sound.sound, instance);
            }
        }
    }

    //private void Update()
    //{
    //    //float vol;
    //    //playerSFXVCA.getVolume(out vol);
    //    //Debug.Log("PlayerSFX volume = " + vol);
    //    //Debug.Log("Jump playbackstate = " + GetPlayBackState(eSound.Jump));

    //    //Debug.Log("Current music = " + AudioManager.Instance.GetCurrentMusic().ToString());

    //}

    public void PlayMusic(eMusic music)
    {
        Debug.Log("Playing music " + music.ToString());
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            currentMusicInstance = instance;
            instance.start();
        }
    }

    public void SetParameter(eMusic music, float value)
    {
        Debug.Log("Setting " + music.ToString() + " to parameter " + value);
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            instance.setParameterByName(music.ToString(), value);
        }
    }

    public void SetParameter(eSound sound, float value)
    {
        Debug.Log("Setting " + sound.ToString() + " to parameter " + value);

        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.setParameterByName(sound.ToString(), value);
        }
    }

    public float GetParameterValue(eMusic music)
    {
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            float value;
            instance.getParameterByName(music.ToString(), out value);
            return value;
        }
        else { return 0f; }
    }

    public float GetParameterValue(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            float value;
            instance.getParameterByName(sound.ToString(), out value);
            return value;
        }
        else { return 0f; }
    }

    public void StopMusic(eMusic music)
    {
        Debug.Log("Stopping music : " + music.ToString());

        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
        }
        else
        {
            Debug.Log("Couldn't stop music : " + music.ToString());
        }
    }

    public eMusic GetCurrentMusic()
    {
        if (currentMusicInstance.isValid())
        {
            float value;
            currentMusicInstance.getParameterByName("Music", out value);
            return (eMusic)value;
        }
        return eMusic.Menu;
    }

    public bool GetIsPlaying(eMusic music)
    {
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            instance.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING;
        }

        Debug.LogWarning($"Music {music} not found in AudioManager!");
        return false;
    }

    public bool GetIsPlaying(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING;
        }

        Debug.LogWarning($"Music {sound} not found in AudioManager!");
        return false;
    }

    public PLAYBACK_STATE GetPlayBackState(eMusic music)
    {
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            PLAYBACK_STATE playbackState;
            instance.getPlaybackState(out playbackState);
            return playbackState;
        }

        Debug.Log("No playbackstate");
        return PLAYBACK_STATE.STOPPED;
    }

    public PLAYBACK_STATE GetPlayBackState(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            PLAYBACK_STATE playbackState;
            instance.getPlaybackState(out playbackState);
            return playbackState;
        }

        Debug.Log("No playbackstate");
        return PLAYBACK_STATE.STOPPED;
    }

    public float GetPitch(eMusic music)
    {
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            float pitch;
            instance.getPitch(out pitch);
            return pitch;
        }

        return 1f;
    }

    public float GetPitch(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            float pitch;
            instance.getPitch(out pitch);
            return pitch;
        }

        return 1f;
    }


    public void SetPitch(eMusic music, float t)
    {
        if (musicLookup.TryGetValue(music, out EventInstance instance))
        {
            instance.setPitch(t);
        }
    }
    public void SetPitch(eSound sound, float t)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.setPitch(t);
        }
    }

    public void PlaySound(eSound sound)
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            if (soundLookup.TryGetValue(sound, out EventInstance instance))
            {
                instance.start();
            }
        }
        else
        {
            PlaySoundClientRpc(sound);
        }
    }
    public void PlayFirstSound(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.start();
        }
    }

    [ClientRpc]
    private void PlaySoundClientRpc(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.start();
        }
        else
        {
            Debug.Log("Didn't find the event reference");
        }
    }

    public void StopSound(eSound sound)
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            if (soundLookup.TryGetValue(sound, out EventInstance instance))
            {
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        else
        {
            StopSoundClientRpc(sound);
        }
    }

    [ClientRpc]
    private void StopSoundClientRpc(eSound sound)
    {
        if (soundLookup.TryGetValue(sound, out EventInstance instance))
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
        else
        {
            Debug.Log("Didn't find the event reference");
        }
    }

    public void SetVolume(float volume)
    {
        allVCA.setVolume(volume);
    }
    public void SetMusicVolume(float volume)
    {
        musicVCA.setVolume(volume);
    }
    public void SetSFXVolume(float volume)
    {
        sfxVCA.setVolume(volume);
    }

    public void SetPlayerSFXVolume(float volume)
    {
        playerSFXVCA.setVolume(volume);
    }

    private void OnDestroy()
    {
        //StopMusic(musicLookup[currentMusicInstance]);
    }
}
