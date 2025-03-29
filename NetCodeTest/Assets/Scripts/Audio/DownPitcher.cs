using UnityEngine;

public class DownPitcher : MonoBehaviour
{
    private float originalPitch = 1.0f;
    private float timer = 0;
    private bool isPitchControlledByThis = false;

    private void Start()
    {
        timer = GameManager.Instance.duration;
        AudioManager.Instance.SetPitch(eMusic.Music, originalPitch);
        AudioManager.Instance.SetPitch(eSound.WalkSpeed, originalPitch);
    }

    private void Update()
    {
        if (GameManager.Instance.IsSlowedDown)
        {
            isPitchControlledByThis = true;
            PitchDown();
        }
        else if (isPitchControlledByThis)
        {
            AudioManager.Instance.SetPitch(eMusic.Music, originalPitch);
            AudioManager.Instance.SetPitch(eSound.WalkSpeed, originalPitch);
            timer = GameManager.Instance.duration;
            isPitchControlledByThis = false;
        }
    }

    private void PitchDown()
    {
        timer = Mathf.Max(timer - Time.deltaTime, 0);
        float t = (timer / GameManager.Instance.duration);
        AudioManager.Instance.SetPitch(eMusic.Music, t);
        AudioManager.Instance.SetPitch(eSound.WalkSpeed, t);
    }
}
