using UnityEngine.Rendering.PostProcessing;
using UnityEngine;

public class BlurEffect : MonoBehaviour
{
    private PostProcessVolume postProcessingVolume;
    private DepthOfField depthOfField;
    private ColorGrading colorGrading;
    private Stats stats;
    private PostProcessLayer postProcessLayer;
    private new Camera camera;

    private float fadeTimer = 0f;
    private float fadeGreyTimer = 0f;
    public const float maxFadeTimer = 0.2f;
    public const float maxGreyFadeTimer = 0.8f;

    private const float defaultSaturation = 0;
    private Color defaultColorFilter = Color.white;

    private const float targetSaturation = -100;
    private Color targetColorFilter = Color.grey;

    private const float defaultFocusDistance = 10f;
    private const float defaultAperture = 1.6f;
    private const float defaultFocalLength = 50f;

    private const float targetFocusDistance = 0.1f;
    private const float targetAperture = 32f;
    private const float targetFocalLength = 300f;
    void Start()
    {
        stats = GetComponentInParent<Stats>();
        camera = GetComponent<Camera>();
        postProcessingVolume = camera.GetComponent<PostProcessVolume>();
        postProcessLayer = camera.GetComponent<PostProcessLayer>();

        if (postProcessingVolume)
        {
            postProcessingVolume.profile.TryGetSettings(out depthOfField);
            postProcessingVolume.profile.TryGetSettings(out colorGrading);
        }
        int playerLayer = LayerMask.NameToLayer($"PostPro{stats.ID.Value}");
        if (playerLayer == -1)
        {
            Debug.Log($"Layer 'PostPro{stats.ID.Value}' does not exist! Add it in Unity -> Project Settings -> Tags & Layers.");
            playerLayer = LayerMask.NameToLayer("Default"); // Fallback to Default layer
        }

        camera.gameObject.layer = playerLayer;

        if (postProcessLayer)
        {
            postProcessLayer.volumeLayer = (1 << playerLayer);
            //Debug.Log($"PostProcessLayer set to layer {playerLayer} (Bitmask: {postProcessLayer.volumeLayer})");
        }

        if (postProcessingVolume)
        {
            postProcessingVolume.gameObject.layer = playerLayer;
        }

        ResetToDefault();
    }

    void Update()
    {
        if (!stats.IsWinner.Value)
        {
            UpdateGraying();
        }
        if (GameManager.Instance && depthOfField != null)
        {
            if (GameManager.Instance.IsSlowedDown)
            {
                UpdateFading();
                depthOfField.active = true;
            }
            else
            {
                ResetToDefault();
                depthOfField.active = false;
            }
        }
        else if (depthOfField != null)
        {
            depthOfField.active = false;
        }
    }


    private void UpdateFading()
    {
        fadeTimer = Mathf.Min(fadeTimer + Time.deltaTime, maxFadeTimer);
        float t = fadeTimer / maxFadeTimer;

        depthOfField.focusDistance.value = Mathf.Lerp(defaultFocusDistance, targetFocusDistance, t);
        depthOfField.aperture.value = Mathf.Lerp(defaultAperture, targetAperture, t);
        depthOfField.focalLength.value = Mathf.Lerp(defaultFocalLength, targetFocalLength, t);
    }

    private void ResetToDefault()
    {
        if (depthOfField)
        {
            depthOfField.active = true;
            depthOfField.focusDistance.value = defaultFocusDistance;
            depthOfField.aperture.value = defaultAperture;
            depthOfField.focalLength.value = defaultFocalLength;
        }

        if (colorGrading)
        {
            colorGrading.saturation.value = defaultSaturation;
            colorGrading.colorFilter.value = defaultColorFilter;
        }
    }

    private void UpdateGraying()
    {
        camera.backgroundColor = Color.grey;
        fadeGreyTimer = Mathf.Min(fadeGreyTimer + Time.deltaTime, maxGreyFadeTimer);
        float t = (fadeGreyTimer / maxFadeTimer);

        colorGrading.saturation.value = Mathf.Lerp(defaultSaturation, targetSaturation, t);
        colorGrading.colorFilter.value.r = Mathf.Lerp(defaultColorFilter.r, targetColorFilter.r, t);
        colorGrading.colorFilter.value.g = Mathf.Lerp(defaultColorFilter.g, targetColorFilter.g, t);
        colorGrading.colorFilter.value.b = Mathf.Lerp(defaultColorFilter.b, targetColorFilter.b, t);
    }
}