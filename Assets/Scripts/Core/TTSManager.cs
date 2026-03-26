using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages TTS button and current speakable content.
/// Persists across scenes. Game scripts call SetContent() to update what gets read aloud.
/// </summary>
public class TTSManager : MonoBehaviour
{
    private static TTSManager _instance;
    public static TTSManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TTSManager>();
                if (_instance == null)
                {
                    var go = new GameObject("TTSManager");
                    _instance = go.AddComponent<TTSManager>();
                }
            }
            return _instance;
        }
    }

    private string _currentContent = "";
    private GameObject _buttonObj;
    private Image _buttonImage;
    private bool _speaking;

    private readonly Color idleColor = new Color(0.25f, 0.55f, 0.85f);
    private readonly Color speakingColor = new Color(0.3f, 0.75f, 0.4f);

    // TTS disabled for NAS build — re-enable when backend is ready
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // static void AutoCreate()
    // {
    //     var _ = Instance;
    // }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        CreateButton();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Set default content based on scene
        string sceneName = scene.name;
        switch (sceneName)
        {
            case "Home":
                SetContent("Home screen. Choose Play, Progress, Settings, or Bank.");
                break;
            case "GameChoice":
                SetContent("Game choice. Pick Spending Game or Emergency Fund.");
                break;
            case "Settings":
                SetContent("Settings. Toggle Calm Mode on or off.");
                break;
            case "Progress":
                SetContent("Your progress. View your stats and balances.");
                break;
            case "BankScene":
                SetContent("Bank account. View your balance and recent transactions.");
                break;
            default:
                SetContent(sceneName);
                break;
        }

        // Re-parent button canvas to ensure it renders on top
        EnsureButtonVisible();
    }

    /// <summary>
    /// Set what gets read aloud when the TTS button is pressed.
    /// Call this whenever the screen content changes meaningfully.
    /// </summary>
    public static void SetContent(string text)
    {
        if (Instance != null)
            Instance._currentContent = text ?? "";
    }

    /// <summary>
    /// Append additional context to current content.
    /// </summary>
    public static void AppendContent(string text)
    {
        if (Instance != null && !string.IsNullOrEmpty(text))
            Instance._currentContent += " " + text;
    }

    void OnButtonPressed()
    {
        if (TTSService.Instance == null) return;

        if (_speaking && TTSService.Instance.IsSpeaking)
        {
            TTSService.Instance.Stop();
            _speaking = false;
            UpdateButtonVisual();
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentContent)) return;

        _speaking = true;
        UpdateButtonVisual();
        TTSService.Instance.Speak(_currentContent, () =>
        {
            _speaking = false;
            UpdateButtonVisual();
        });
    }

    void UpdateButtonVisual()
    {
        if (_buttonImage != null)
            _buttonImage.color = _speaking ? speakingColor : idleColor;
    }

    void CreateButton()
    {
        // Create a separate overlay canvas for the button
        var canvasGO = new GameObject("TTSButtonCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Button
        _buttonObj = new GameObject("TTSButton");
        _buttonObj.transform.SetParent(canvasGO.transform, false);
        var rt = _buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(30f, 30f);
        rt.sizeDelta = new Vector2(80f, 80f);

        // Find RoundedRect sprite
        Sprite roundedRect = null;
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
            if (s.name == "RoundedRect") { roundedRect = s; break; }

        _buttonImage = _buttonObj.AddComponent<Image>();
        _buttonImage.color = idleColor;
        if (roundedRect != null) { _buttonImage.sprite = roundedRect; _buttonImage.type = Image.Type.Sliced; }

        var btn = _buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(OnButtonPressed);

        // Speaker icon (text-based: 🔊 emoji alternative)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(_buttonObj.transform, false);
        var iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var iconText = iconGO.AddComponent<TextMeshProUGUI>();
        iconText.text = "TTS";
        iconText.fontSize = 28;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = Color.white;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.raycastTarget = false;
    }

    void EnsureButtonVisible()
    {
        // Button canvas persists via DontDestroyOnLoad, always on top
        if (_buttonObj != null)
            _buttonObj.SetActive(true);
    }
}
