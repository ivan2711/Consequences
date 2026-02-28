using UnityEngine;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    private static DebugOverlay _instance;

    private GameObject _panel;
    private TextMeshProUGUI _text;
    private bool _visible = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        _panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            _visible = !_visible;
            if (_panel != null) _panel.SetActive(_visible);
        }

        if (_visible && _text != null)
            RefreshText();
    }

    void BuildUI()
    {
        // Canvas at scene root — avoid inheriting parent transform
        GameObject canvasGO = new GameObject("DebugCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // Panel — anchor to center of screen
        _panel = new GameObject("DebugPanel");
        _panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.35f);
        panelRect.anchorMax = new Vector2(0.75f, 0.65f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image bg = _panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = false;

        // Text — fill panel with padding
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(_panel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        _text = textGO.AddComponent<TextMeshProUGUI>();
        _text.fontSize = 22f;
        _text.color = new Color(0f, 1f, 0.4f);
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.raycastTarget = false;
        _text.enableWordWrapping = true;
    }

    void RefreshText()
    {
        var pm = PlayerModelService.Instance;
        if (pm == null)
        {
            _text.text = "[PlayerModel] not found";
            return;
        }

        _text.text =
            $"<b>--- Debug Overlay ---</b>\n" +
            $"State: <b>{pm.GetEngagementState()}</b>\n" +
            $"Overspends: {pm.overspendCount}\n" +
            $"Treat Avg:  {pm.treatRatioAvg:F2}\n" +
            $"Streak:     {pm.failedRoundsStreak}F / {pm.successStreak}S\n" +
            $"Idle Count: {pm.inactivityCount}\n" +
            $"Bank: {(BankAccountService.Instance != null ? "£" + BankAccountService.Instance.GetBalance().ToString("F2") : "n/a")}";
    }
}
