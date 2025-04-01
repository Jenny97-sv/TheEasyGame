using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

public class UIShowStats : NetworkBehaviour
{
    private TextMeshProUGUI playerHPText = null;
    private Canvas canvas = null;
    private Stats stats = null;
    private Image playerHPImage = null;
    private Image playerHPBackground = null;
    private List<(Stats, TextMeshProUGUI)> opponentStatsUI = new List<(Stats, TextMeshProUGUI)>();
    private List<(Stats, Image, Image)> opponentImageUI = new List<(Stats, Image, Image)>();
    [SerializeField] private EndScreenText[] deadTexts = null;
    [SerializeField] private EndScreenText[] lonelyTexts = null;
    private int randomDeadText = -1;
    private int randomLonelyText = -1;

    private float width = 200;
    private float height = 20;
    private float backgroundAddedSize = 5;

    void Start()
    {
        if (!SceneHandler.Instance.IsLocalGame && !IsOwner)
            return;

        stats = GetComponent<Stats>();
        if (stats == null)
        {
            Debug.LogWarning("Stats component not found.");
            return;
        }

        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("No camera found for this player!");
            return;
        }

        GameObject canvasObject = new GameObject("StatsCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = playerCamera;
        canvas.sortingOrder = 100;
        canvas.planeDistance = 0.5f;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasObject.AddComponent<GraphicRaycaster>();

        if (GameManager.Instance.GetPlayers().Count == 1)
        {
            foreach (var text in lonelyTexts)
            {
                text.textUI.transform.SetParent(canvas.transform, false);
            }
            randomLonelyText = Random.Range(0, lonelyTexts.Length);
            lonelyTexts[randomLonelyText].textUI.text = lonelyTexts[randomLonelyText].text;
            lonelyTexts[randomLonelyText].textUI.gameObject.SetActive(true);
        }
        else
        {
            foreach (var text in lonelyTexts)
            {
                text.textUI.gameObject.SetActive(false);
            }
        }

        int offsetY = -90;
        if (SceneHandler.Instance.IsLocalGame)
        {
            playerHPBackground = CreateImageElement("PlayerHPBackground", new Vector2(48, offsetY + 40), new Vector2((width / 2) + backgroundAddedSize, (height / 2) + backgroundAddedSize), Color.black, anchorRight: false);
            playerHPImage = CreateImageElement("PlayerHPBar", new Vector2(50, offsetY + 40), new Vector2((width / 2), (height / 2)), Color.green, anchorRight: false);
        }
        else
        {
            playerHPBackground = CreateImageElement("PlayerHPBackground", new Vector2(48, offsetY + 40), new Vector2(width + backgroundAddedSize, height + backgroundAddedSize), Color.black, anchorRight: false);
            playerHPImage = CreateImageElement("PlayerHPBar", new Vector2(50, offsetY + 40), new Vector2(width, height), Color.green, anchorRight: false);
        }
        playerHPText = CreateTextElement("PlayerHPText", new Vector2(50, offsetY), TextAlignmentOptions.TopLeft, anchorRight: false);

        foreach(var text in deadTexts)
        {
            text.textUI.transform.SetParent(canvas.transform, false);
        }

        randomDeadText = Random.Range(0, deadTexts.Length);
        deadTexts[randomDeadText].textUI.text = deadTexts[randomDeadText].text; 
        deadTexts[randomDeadText].textUI.gameObject.SetActive(false);

        if (SceneHandler.Instance.IsLocalGame)
            return;
        foreach (var networkObject in FindObjectsOfType<NetworkObject>())
        {
            if (networkObject != null && networkObject != this.NetworkObject)
            {
                var opponentStats = networkObject.GetComponent<Stats>();
                if (opponentStats != null)
                {
                    var opponentHPBackground = CreateImageElement($"OpponentHPBackground_{networkObject.OwnerClientId}",
                        new Vector2(-97.5f, offsetY + 40), new Vector2(width / 2 + backgroundAddedSize, height / 2 + backgroundAddedSize), Color.black, anchorRight: true);

                    var opponentHPBar = CreateImageElement($"OpponentHPBar_{networkObject.OwnerClientId}",
                        new Vector2(-100, offsetY + 40), new Vector2(width / 2, height / 2), Color.green, anchorRight: true);


                    var opponentText = CreateTextElement($"OpponentHP_{networkObject.OwnerClientId}",
                        new Vector2(-100, offsetY), TextAlignmentOptions.TopRight, anchorRight: true);


                    opponentStatsUI.Add((opponentStats, opponentText));
                    opponentImageUI.Add((opponentStats, opponentHPBar, opponentHPBackground));

                    offsetY -= 70;
                }
            }
        }
    }


    private TextMeshProUGUI CreateTextElement(string name, Vector2 anchoredPosition, TextAlignmentOptions alignment, bool anchorRight)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(canvas.transform, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 10;
        text.alignment = alignment;
        text.enableWordWrapping = false;

        RectTransform rectTransform = text.rectTransform;

        if (anchorRight)
        {
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 0.5f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 0.5f);
        }

        rectTransform.anchoredPosition = anchoredPosition;

        return text;
    }

    private Image CreateImageElement(string name, Vector2 anchoredPosition, Vector2 size, Color color, bool anchorRight)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(canvas.transform, false);

        var image = imageObject.AddComponent<Image>();
        image.color = color;

        RectTransform rectTransform = image.rectTransform;

        if (anchorRight)
        {
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 0.5f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 0.5f);
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(size.x, size.y);
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        width = size.x;
        height = size.y;

        return image;
    }


    void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame)
        {
            if (!IsOwner || playerHPText == null || stats == null)
            {
                return;
            }
        }
        else if (playerHPText == null || stats == null || SceneHandler.Instance.sceneName.Value != SceneName.Scene1)
            return;

        if (stats.HP.Value <= 0)
        {
            deadTexts[randomDeadText].textUI.gameObject.SetActive(true);
        }

        playerHPText.text = $"{stats.HP.Value} / {stats.MaxHP.Value}";

        float newWidth = (width / 2) * ((float)stats.HP.Value / stats.MaxHP.Value);
        float newMaxWidth = (width / 2) * ((float)stats.MaxHP.Value / stats.MaxHP.Value); 

        if (SceneHandler.Instance.IsLocalGame)
        {
            // Expand to the right (Player)
            playerHPImage.rectTransform.sizeDelta = new Vector2(newWidth, height / 2);
            playerHPBackground.rectTransform.sizeDelta = new Vector2(newMaxWidth + backgroundAddedSize, (height / 2) + backgroundAddedSize);
        }
        else
        {
            playerHPImage.rectTransform.sizeDelta = new Vector2(newWidth, height);
            playerHPBackground.rectTransform.sizeDelta = new Vector2(newMaxWidth + backgroundAddedSize, height + backgroundAddedSize);
        }

        for (int i = 0; i < opponentStatsUI.Count; i++)
        {
            var (opponentStats, opponentText) = opponentStatsUI[i];
            var opponentHPBar = opponentImageUI[i].Item2;
            var opponentHPBackground = opponentImageUI[i].Item3;

            if(opponentStats)
                opponentText.text = $"Player {opponentStats.gameObject.GetComponent<NetworkObject>().OwnerClientId + 1} : {opponentStats.HP.Value} / {opponentStats.MaxHP.Value}";

            float opponentNewWidth = (width / 2) * ((float)opponentStats.HP.Value / opponentStats.MaxHP.Value);
            float opponentNewMaxWidth = (width / 2) * ((float)opponentStats.MaxHP.Value / opponentStats.MaxHP.Value);

            opponentHPBar.rectTransform.sizeDelta = new Vector2(opponentNewWidth, height / 2);
            opponentHPBackground.rectTransform.sizeDelta = new Vector2(opponentNewMaxWidth + backgroundAddedSize, (height / 2) + backgroundAddedSize);
        }
    }

}
