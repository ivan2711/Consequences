using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CredentialsPanel : MonoBehaviour
{
    private GameObject overlayPanel;

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        CreateCreditsButton(canvas.transform);
    }

    void CreateCreditsButton(Transform canvasTransform)
    {
        GameObject btnObj = new GameObject("CreditsButton");
        btnObj.transform.SetParent(canvasTransform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 30f);
        btnRect.sizeDelta = new Vector2(400f, 150f);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(ShowOverlay);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "Credits";
        btnText.fontSize = 48;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontStyle = FontStyles.Bold;
    }

    void ShowOverlay()
    {
        if (overlayPanel != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        overlayPanel = new GameObject("CredentialsOverlay");
        overlayPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = overlayPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = overlayPanel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.14f, 0.22f, 1f);

        // Vertical layout for content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(overlayPanel.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(60f, 180f);
        contentRect.offsetMax = new Vector2(-60f, -80f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title
        AddText(content.transform, "Consequences v1.0", 52, new Color(1f, 0.85f, 0.2f, 1f), FontStyles.Bold, 80f);

        // Credential lines
        string[] lines = new string[]
        {
            "A Financial Literacy game for SEND players",
            "Developed by Ivan Murtov at UCL Computer Science",
            "Supervised by Prof. Dean Mohamedally",
            "Developed for MotionInput Games Ltd \u00a9 2026+"
        };

        foreach (string line in lines)
        {
            AddText(content.transform, line, 44, Color.white, FontStyles.Normal, 70f);
        }

        // Close button
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(overlayPanel.transform, false);

        RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 30f);
        closeRect.sizeDelta = new Vector2(400f, 150f);

        Image closeImage = closeBtnObj.AddComponent<Image>();
        closeImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeImage;
        closeBtn.onClick.AddListener(HideOverlay);

        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);

        RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "Close";
        closeText.fontSize = 48;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.fontStyle = FontStyles.Bold;
    }

    void AddText(Transform parent, string text, float fontSize, Color color, FontStyles style, float height)
    {
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, height);

        LayoutElement le = textObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
    }

    void HideOverlay()
    {
        if (overlayPanel != null)
        {
            Destroy(overlayPanel);
            overlayPanel = null;
        }
    }
}
