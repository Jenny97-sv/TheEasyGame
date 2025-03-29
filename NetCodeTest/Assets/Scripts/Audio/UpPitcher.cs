using Unity.Netcode;
using UnityEngine;

public class UpPitcher : MonoBehaviour
{
    private float originalPitch = 1.0f;
    private float targetPitch = 1.1f;
    private float timer = 0;
    private float duration = 0.2f;
    private Stats stats;
    private bool isPitchingUp = false;
    private bool isPitchingDown = false;

    private void Start()
    {
        stats = GetComponent<Stats>();
    }

    private void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame && GetComponent<NetworkObject>().IsOwner || SceneHandler.Instance.IsLocalGame)
        {
            //Debug.Log("Client " + GetComponent<NetworkObject>().OwnerClientId);
            if (stats.myIsPoweredUp.Value && !GameManager.Instance.IsSlowedDown)
            {
                if (!isPitchingUp) // Start pitch up only if it's not already doing so
                {
                    //Debug.Log("Is pitching up!");
                    isPitchingUp = true;
                    isPitchingDown = false;
                    StartPitchUp();
                }
            }
            else
            {
                if (!isPitchingDown) // Start pitch down only if it's not already doing so
                {
                    isPitchingDown = true;
                    isPitchingUp = false;
                    StartPitchDown();
                }
            }
        }
    }

    private void StartPitchUp()
    {
        timer = 0;
        InvokeRepeating(nameof(PitchUp), 0f, Time.deltaTime); // Run every frame until done
    }

    private void PitchUp()
    {
        if (timer >= duration)
        {
            AudioManager.Instance.SetPitch(eMusic.Music, targetPitch);
            CancelInvoke(nameof(PitchUp)); // Stop updating
            return;
        }

        timer = Mathf.Min(timer + Time.deltaTime, duration);
        float t = timer / duration;
        float newPitch = Mathf.Lerp(originalPitch, targetPitch, t);
        //Debug.Log("Pitch = " + newPitch);

        AudioManager.Instance.SetPitch(eMusic.Music, newPitch);
    }

    private void StartPitchDown()
    {
        timer = duration;
        InvokeRepeating(nameof(PitchDown), 0f, Time.deltaTime); // Run every frame until done
    }

    private void PitchDown()
    {
        if (timer <= 0)
        {
            AudioManager.Instance.SetPitch(eMusic.Music, originalPitch);
            CancelInvoke(nameof(PitchDown)); // Stop updating
            return;
        }

        timer = Mathf.Max(timer - Time.deltaTime, 0);
        float t = timer / duration;
        float newPitch = Mathf.Lerp(targetPitch, originalPitch, 1 - t);
        //Debug.Log("Pitch = " + newPitch);

        AudioManager.Instance.SetPitch(eMusic.Music, newPitch);
    }
}
