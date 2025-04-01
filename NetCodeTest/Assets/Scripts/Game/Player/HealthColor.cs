using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthColor : NetworkBehaviour
{
    [SerializeField] private Image healthOverlay;
    private Stats playerStats;
    [SerializeField] private float maxAlpha = 0.3f;
    private NetworkObject netObj;

    private Camera playerCamera;
    private Rect cameraRect;
    private RectTransform healthOverlayRect;

    private bool shouldManageOverlay = false;
    private bool isDead = false;

    private void Start()
    {
        playerStats = GetComponent<Stats>();
        netObj = GetComponent<NetworkObject>();
        playerCamera = GetComponentInChildren<Camera>();

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
            shouldManageOverlay = netObj.IsOwner;
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

        float healthPercent = playerStats.HP.Value / (float)playerStats.MaxHP.Value;
        float alpha = Mathf.Clamp01(1 - healthPercent) * maxAlpha;
        healthOverlay.color = new Color(1, 0, 0, alpha);

        if (!isDead && playerStats.HP.Value <= 0)
        {
            isDead = true;
            if (SceneHandler.Instance.IsLocalGame)
            {
                SetDeadColor();
            }
            else if (netObj.IsOwner) // Only the owner should trigger this
            {
                Debug.Log("Sending serverRPC!");
                SetDeadColorServerRPC();
            }
        }
    }

    private void SetDeadColor()
    {
        GetComponent<Renderer>().material.color = new Color(0.1f, 0, 0.1f, 1);
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.material.color = new Color(0.1f, 0, 0.1f, 1);
        }
    }

    [ServerRpc]
    private void SetDeadColorServerRPC()
    {
        // Apply color change on server
        SetDeadColor();

        // Synchronize to all clients
        SetDeadColorClientRPC();

        Debug.Log("Applied colors on server!");
    }

    [ClientRpc]
    private void SetDeadColorClientRPC()
    {
        // Skip if we're the owner (already handled)
        if (netObj.IsOwner) return;

        // Apply color change on all clients
        SetDeadColor();

        Debug.Log("Applied colors on client!");
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
        int playerIndex = playerStats.ID.Value;

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