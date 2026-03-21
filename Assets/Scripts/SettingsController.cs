using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SettingsController : MonoBehaviour
{
    public string backSceneName = "Home";

    [Header("Calm Mode - assign the container")]
    public RectTransform calmModeContainer;

    private Image _trackImage;
    private RectTransform _knobRect;
    private TextMeshProUGUI _label;
    private TextMeshProUGUI _statusLabel;
    private bool _animating;

    private readonly Color trackOff = new Color(0.35f, 0.38f, 0.45f);
    private readonly Color trackOn = new Color(0.3f, 0.72f, 0.4f);
    private readonly Color knobColor = Color.white;

    private const float TRACK_WIDTH = 120f;
    private const float TRACK_HEIGHT = 60f;
    private const float KNOB_SIZE = 50f;
    private const float KNOB_PADDING = 5f;

    void Start()
    {
        if (calmModeContainer != null)
            BuildPillToggle();
    }

    void BuildPillToggle()
    {
        // Load RoundedRect sprite
        Sprite pill = null;
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (s.name == "RoundedRect") { pill = s; break; }
        }

        // Label: "Calm Mode"
        var labelGo = new GameObject("CalmLabel");
        labelGo.transform.SetParent(calmModeContainer, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0.55f, 1f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.text = "Calm Mode";
        _label.fontSize = 42;
        _label.fontStyle = FontStyles.Bold;
        _label.color = Color.white;
        _label.alignment = TextAlignmentOptions.MidlineRight;

        // Track (the pill background)
        var trackGo = new GameObject("Track");
        trackGo.transform.SetParent(calmModeContainer, false);
        var trackRt = trackGo.AddComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.6f, 0.5f);
        trackRt.anchorMax = new Vector2(0.6f, 0.5f);
        trackRt.pivot = new Vector2(0f, 0.5f);
        trackRt.sizeDelta = new Vector2(TRACK_WIDTH, TRACK_HEIGHT);
        trackRt.anchoredPosition = Vector2.zero;
        _trackImage = trackGo.AddComponent<Image>();
        _trackImage.color = trackOff;
        if (pill != null) { _trackImage.sprite = pill; _trackImage.type = Image.Type.Sliced; }

        // Make track clickable
        var btn = trackGo.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(ToggleCalmMode);

        // Knob (the circle that slides)
        var knobGo = new GameObject("Knob");
        knobGo.transform.SetParent(trackGo.transform, false);
        _knobRect = knobGo.AddComponent<RectTransform>();
        _knobRect.anchorMin = new Vector2(0f, 0.5f);
        _knobRect.anchorMax = new Vector2(0f, 0.5f);
        _knobRect.pivot = new Vector2(0.5f, 0.5f);
        _knobRect.sizeDelta = new Vector2(KNOB_SIZE, KNOB_SIZE);
        var knobImg = knobGo.AddComponent<Image>();
        knobImg.color = knobColor;
        knobImg.raycastTarget = false;
        if (pill != null) { knobImg.sprite = pill; knobImg.type = Image.Type.Sliced; }

        // Status label (ON/OFF) to the right of track
        var statusGo = new GameObject("Status");
        statusGo.transform.SetParent(calmModeContainer, false);
        var statusRt = statusGo.AddComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.82f, 0f);
        statusRt.anchorMax = new Vector2(1f, 1f);
        statusRt.offsetMin = Vector2.zero;
        statusRt.offsetMax = Vector2.zero;
        _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
        _statusLabel.fontSize = 32;
        _statusLabel.alignment = TextAlignmentOptions.MidlineLeft;

        UpdateVisual(false);
    }

    void ToggleCalmMode()
    {
        if (_animating) return;
        GameSettings.CalmMode = !GameSettings.CalmMode;
        UpdateVisual(true);
    }

    void UpdateVisual(bool animate)
    {
        bool on = GameSettings.CalmMode;
        float knobX = on
            ? TRACK_WIDTH - KNOB_SIZE / 2f - KNOB_PADDING
            : KNOB_SIZE / 2f + KNOB_PADDING;

        _trackImage.color = on ? trackOn : trackOff;

        if (_statusLabel != null)
        {
            _statusLabel.text = on ? "ON" : "OFF";
            _statusLabel.color = on ? trackOn : new Color(0.6f, 0.6f, 0.65f);
        }

        if (animate && _knobRect != null)
            StartCoroutine(AnimateKnob(knobX));
        else if (_knobRect != null)
            _knobRect.anchoredPosition = new Vector2(knobX, 0f);
    }

    IEnumerator AnimateKnob(float targetX)
    {
        _animating = true;
        float startX = _knobRect.anchoredPosition.x;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _knobRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), 0f);
            yield return null;
        }

        _knobRect.anchoredPosition = new Vector2(targetX, 0f);
        _animating = false;
    }

    public void Back()
    {
        SceneManager.LoadScene(backSceneName);
    }
}