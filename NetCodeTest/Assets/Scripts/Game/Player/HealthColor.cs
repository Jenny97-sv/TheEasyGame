using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class HealthColor : NetworkBehaviour
{
    [Header("Health Color")]
    [SerializeField] private Image healthOverlay;
    [SerializeField] private float maxAlpha = 0.3f;
    [SerializeField] private Color healthColor = new Color(0.5f, 0.5f, 0.9f);

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float damageFlashDuration = 0.1f;
    private float damageFlashTimer = 0f;

    [SerializeField] private GameObject[] childrenToRender;

    private Renderer render;
    private NetworkVariable<Color> startColor = new NetworkVariable<Color>();
    private NetworkVariable<Color> damageFlashColorNet = new NetworkVariable<Color>();
    private Stats stats;
    private Camera playerCamera;
    private Rect cameraRect;
    private RectTransform healthOverlayRect;

    private bool shouldManageOverlay = false;
    private bool isDead = false;

    private void Start()
    {
        stats = GetComponent<Stats>();
        playerCamera = GetComponentInChildren<Camera>();
        render = GetComponent<Renderer>();
        if(IsServer && !SceneHandler.Instance.IsLocalGame)
        {
            startColor.Value = render.material.color;
            damageFlashColorNet.Value = damageFlashColor;
        }
        else if (SceneHandler.Instance.IsLocalGame) 
        {
            startColor.Value = render.material.color;
            damageFlashColorNet.Value = damageFlashColor;
        }

        if (playerCamera != null)
        {
            cameraRect = playerCamera.rect;
        }

        if (SceneHandler.Instance.IsLocalGame)
        {
            shouldManageOverlay = true;
        }
        else
        {
            shouldManageOverlay = IsOwner;
        }

        if (!shouldManageOverlay)
        {
            if (healthOverlay != null)
            {
                healthOverlay.gameObject.SetActive(false);
            }
            return;
        }

        if (healthOverlay != null)
        {
            healthOverlayRect = healthOverlay.GetComponent<RectTransform>();
            AdjustBackgroundPosition();
        }
    }

    void Update()
    {
        if (!shouldManageOverlay || healthOverlay == null) return;

        float healthPercent = stats.HP.Value / (float)stats.MaxHP.Value;
        float alpha = Mathf.Clamp01(1 - healthPercent) * maxAlpha;
        healthOverlay.color = new Color(healthColor.r, healthColor.g, healthColor.b, alpha);

        if (!isDead && stats.HP.Value <= 0)
        {
            isDead = true;
            if (SceneHandler.Instance.IsLocalGame)
            {
                SetDeadColor();
            }
            else if (IsOwner) // Only the owner should trigger this
            {
                Debug.Log("Sending serverRPC!");
                SetDeadColorServerRPC();
            }
        }

        if (stats.myTookDamage.Value)
        {
            damageFlashTimer += Time.deltaTime;
            healthOverlay.color = damageFlashColorNet.Value;
            SetFlashColor();
            if (damageFlashTimer >= damageFlashDuration)
            {
                stats.myTookDamage.Value = false;
                damageFlashTimer = 0;
                ResetFlashColor();
            }
            if(!SceneHandler.Instance.IsLocalGame)
                SetFlashColorServerRPC();
        }
    }

    private void SetFlashColor()
    {
        render.material.color = damageFlashColorNet.Value;
        foreach (var renderer in childrenToRender)
        {
            renderer.GetComponent<Renderer>().material.color = damageFlashColor;
        }
    }
    private void ResetFlashColor()
    {
        render.material.color = startColor.Value;
        foreach (var renderer in childrenToRender)
        {
            renderer.GetComponent<Renderer>().material.color = startColor.Value;
        }
    }

    [ServerRpc]
    private void SetFlashColorServerRPC()
    {
        if (stats.myTookDamage.Value)
        {
            SetFlashColor();
        }
        else
        {
            Debug.Log("EY!");
            ResetFlashColor();
        }
        SetFlashColorClientRPC();
    }

    [ClientRpc]
    private void SetFlashColorClientRPC()
    {
        if (!IsOwner)
        {
            if (!stats.myTookDamage.Value)
                SetFlashColor();
            else
            {
                Debug.Log("EY!");
                ResetFlashColor();
            }
        }
    }

    private void SetDeadColor()
    {
        render.material.color = new Color(healthColor.r, healthColor.g, healthColor.b, 1);
        foreach (var renderer in childrenToRender)
        {
            renderer.GetComponent<Renderer>().material.color = new Color(healthColor.r, healthColor.g, healthColor.b, 1);
        }
    }

    [ServerRpc]
    private void SetDeadColorServerRPC()
    {
        SetDeadColor();

        SetDeadColorClientRPC();
    }

    [ClientRpc]
    private void SetDeadColorClientRPC()
    {
        if (IsOwner) return;

        SetDeadColor();
    }

    void AdjustBackgroundPosition()
    {
        if (playerCamera == null || healthOverlayRect == null) return;

        if (!SceneHandler.Instance.IsLocalGame)
        {
            healthOverlayRect.anchorMin = new Vector2(0, 0);
            healthOverlayRect.anchorMax = new Vector2(1, 1);
            return;
        }

        int playerCount = SceneHandler.Instance.MaxPlayerCount;
        int playerIndex = stats.ID.Value;

        switch (playerCount)
        {
            case 1: // Fullscreen
                healthOverlayRect.anchorMin = new Vector2(0, 0);
                healthOverlayRect.anchorMax = new Vector2(1, 1);
                break;

            case 2: // Two players, top and bottom split
                switch (playerIndex)
                {
                    case 0:
                        healthOverlayRect.anchorMin = new Vector2(0, 0.5f);
                        healthOverlayRect.anchorMax = new Vector2(1, 1);
                        break;

                    case 1:
                        healthOverlayRect.anchorMin = new Vector2(0, 0);
                        healthOverlayRect.anchorMax = new Vector2(1, 0.5f);
                        break;
                }
                break;

            case 3: // Three players (Top-Left, Top-Right, Bottom)
                switch (playerIndex)
                {
                    case 0:
                        healthOverlayRect.anchorMin = new Vector2(0, 0.5f);
                        healthOverlayRect.anchorMax = new Vector2(0.5f, 1);
                        break;

                    case 1:
                        healthOverlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                        healthOverlayRect.anchorMax = new Vector2(1, 1);
                        break;

                    case 2:
                        healthOverlayRect.anchorMin = new Vector2(0, 0);
                        healthOverlayRect.anchorMax = new Vector2(1, 0.5f);
                        break;
                }
                break;

            case 4: // Four players (2x2 grid)
                float xMin = (playerIndex % 2 == 0) ? 0 : 0.5f;
                float xMax = xMin + 0.5f;
                float yMin = (playerIndex < 2) ? 0.5f : 0;
                float yMax = yMin + 0.5f;

                healthOverlayRect.anchorMin = new Vector2(xMin, yMin);
                healthOverlayRect.anchorMax = new Vector2(xMax, yMax);
                break;
        }

        healthOverlayRect.anchoredPosition = Vector2.zero;
        healthOverlayRect.sizeDelta = Vector2.zero;
    }

    public void OnScreenConfigChanged()
    {
        if (!shouldManageOverlay) return;

        if (playerCamera != null)
        {
            cameraRect = playerCamera.rect;
            AdjustBackgroundPosition();
        }
    }
}