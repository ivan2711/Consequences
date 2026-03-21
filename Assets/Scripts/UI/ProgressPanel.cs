using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ProgressPanel : MonoBehaviour
{
    private TextMeshProUGUI bankBalanceValue;
    private TextMeshProUGUI emergencyFundValue;
    private TextMeshProUGUI successStreakValue;
    private TextMeshProUGUI overspendValue;
    private TextMeshProUGUI treatRatioValue;
    private TextMeshProUGUI engagementValue;

    void Start()
    {
        BuildUI();
        Refresh();
    }

    void BuildUI()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Background
        var bg = CreatePanel(transform, "Background", Vector2.zero, Vector2.one, new Color(0.1f, 0.12f, 0.16f, 1f));

        // Title
        CreateText(transform, "TitleText", "Your Progress",
            new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.96f),
            60, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // Find RoundedRect sprite
        Sprite roundedRect = null;
        var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in allSprites)
        {
            if (s.name == "RoundedRect")
            {
                roundedRect = s;
                break;
            }
        }

        // Stat cards — 3x2 grid
        float gapX = 0.02f;
        float gapY = 0.03f;
        float cardW = (0.90f - 2 * gapX) / 3f;
        float cardH = (0.65f - gapY) / 2f;
        float startX = 0.05f;
        float startY = 0.18f;

        var cardColor = new Color(0.18f, 0.22f, 0.28f, 1f);

        // Row 1 (top): Bank Balance, Emergency Fund, Success Streak
        bankBalanceValue = CreateCard(startX, startY + cardH + gapY, cardW, cardH,
            "Bank Balance", "\u00a3500.00", Color.white, cardColor, roundedRect);

        emergencyFundValue = CreateCard(startX + cardW + gapX, startY + cardH + gapY, cardW, cardH,
            "Emergency Fund", "\u00a30.00", new Color(0.4f, 0.7f, 1f), cardColor, roundedRect);

        successStreakValue = CreateCard(startX + 2 * (cardW + gapX), startY + cardH + gapY, cardW, cardH,
            "Success Streak", "0", new Color(0.4f, 0.9f, 0.5f), cardColor, roundedRect);

        // Row 2 (bottom): Times Overspent, Treat Ratio, Engagement
        overspendValue = CreateCard(startX, startY, cardW, cardH,
            "Times Overspent", "0", new Color(1f, 0.45f, 0.4f), cardColor, roundedRect);

        treatRatioValue = CreateCard(startX + cardW + gapX, startY, cardW, cardH,
            "Treat Ratio", "0%", new Color(1f, 0.8f, 0.3f), cardColor, roundedRect);

        engagementValue = CreateCard(startX + 2 * (cardW + gapX), startY, cardW, cardH,
            "Engagement", "OK", new Color(0.4f, 0.9f, 0.5f), cardColor, roundedRect);

        // Home button
        var homeBtn = CreatePanel(transform, "HomeButton",
            new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.14f),
            new Color(0.3f, 0.5f, 0.8f, 1f), roundedRect);
        var btn = homeBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => SceneManager.LoadScene("Home"));
        CreateText(homeBtn.transform, "Text", "Home",
            Vector2.zero, Vector2.one,
            40, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    TextMeshProUGUI CreateCard(float x, float y, float w, float h,
        string label, string defaultValue, Color valueColor, Color bgColor, Sprite sprite)
    {
        var card = CreatePanel(transform, "Card_" + label.Replace(" ", ""),
            new Vector2(x, y), new Vector2(x + w, y + h), bgColor, sprite);

        CreateText(card.transform, "Label", label,
            new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.92f),
            28, FontStyles.Normal, new Color(0.6f, 0.65f, 0.75f), TextAlignmentOptions.Center);

        var valueText = CreateText(card.transform, "Value", defaultValue,
            new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.58f),
            44, FontStyles.Bold, valueColor, TextAlignmentOptions.Center);

        return valueText;
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Color color, Sprite sprite = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        return go;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        int fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        return tmp;
    }

    void Refresh()
    {
        var bank = BankAccountService.Instance;
        var model = PlayerModelService.Instance;

        if (bank != null)
        {
            SetValue(bankBalanceValue, string.Format("\u00a3{0:0.00}", bank.GetBalance()));
            int fundBalance = PlayerPrefs.GetInt("EmergencyFundBalance", 0);
            SetValue(emergencyFundValue, "\u00a3" + fundBalance);
        }

        if (model != null)
        {
            SetValue(successStreakValue, model.successStreak.ToString());
            SetValue(overspendValue, model.overspendCount.ToString());
            SetValue(treatRatioValue, string.Format("{0:0}%", model.treatRatioAvg * 100f));

            var state = model.GetEngagementState();
            SetValue(engagementValue, state.ToString());
            if (engagementValue != null)
            {
                engagementValue.color = state switch
                {
                    PlayerModelService.EngagementState.OK => new Color(0.4f, 0.9f, 0.5f),
                    PlayerModelService.EngagementState.Frustrated => new Color(1f, 0.45f, 0.4f),
                    PlayerModelService.EngagementState.Bored => new Color(1f, 0.8f, 0.3f),
                    _ => Color.white
                };
            }
        }
    }

    void SetValue(TextMeshProUGUI text, string value)
    {
        if (text != null) text.text = value;
    }
}
