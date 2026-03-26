using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FinancialLiteracy.UI;

public class SpendingGameController : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public float price;
        public bool isNecessary;
        public Toggle toggle;
    }

    [Header("Config")]
    public float weeklyBudgetPounds = 8.50f;

    [Header("Items (base 8 — always visible)")]
    public List<ShopItem> items = new List<ShopItem>();

    [Header("Round 3 Extra Items (hidden until Round 3)")]
    public List<ShopItem> round3ExtraItems = new List<ShopItem>();

    [Header("UI Manager")]
    public SpendingGameUI uiManager;

    [Header("Feedback UI")]
    public GameObject feedbackPanel;
    public TMP_Text totalText;
    public TMP_Text feedbackText;
    public TMP_Text scorecardText;

    [Header("Round UI")]
    public TextMeshProUGUI roundText;

    [Header("New UI Components")]
    public MoneyCounter moneyCounter;
    public StarRating starRating;
    public DuckReaction duckReaction;
    public DuckReactionBackgroundChanger backgroundChanger;
    public ConsequencePanel consequencePanel;

    // Round system — scenario loaded from JSON
    private int currentRound = 0;
    private const int TotalRounds = 3;
    private SpendingScenario _currentScenario;

    // Fallback hardcoded data (used if no JSON scenarios found)
    private static readonly float[] FallbackBudgets = { 8.50f, 7.50f, 12.00f };
    private static readonly string[] FallbackNames = { "Normal Week", "Tight Week", "Payday Week" };
    private static readonly string[] FallbackDuckLines = {
        "Normal week \u2014 buy what you need!",
        "Tight week \u2014 budget is smaller!",
        "Payday! But don't splurge!"
    };

    private float[] _roundTotals = new float[3];
    private int[] _roundStars = new int[3];
    private int[] _roundEssentials = new int[3];
    private int[] _roundTreats = new int[3];
    private bool _hasLoggedThisRound = false;
    private bool _suppressToggleReaction = false;
    private bool _roundInProgress = false; // true while player is shopping, false after Check Out
    private int _roundsCompleted = 0; // only increments — used by AdvanceRound to determine next round

    // Fun features
    private int _streakCount = 0; // consecutive 3-star rounds
    private float _totalSavingsJar = 0f; // cumulative savings across rounds
    private GameObject _starContainer; // holds star icons after checkout
    private GameObject _streakDisplay; // flame icon + streak text
    private GameObject _savingsJarDisplay; // jar icon + savings text
    private GameObject _awardDisplay; // trophy + award title at end
    private Sprite _starFilledSprite;
    private Sprite _flameSprite;
    private Sprite _jarSprite;
    private Sprite _priceTagSprite;
    private Sprite _trophySprite;

    // Inactivity tracking
    private float _idleTimer = 0f;
    private float _idleCooldown = 0f;
    private const float IdleThresholdSeconds = 60f;

    // Re-engagement popup
    private GameObject _reengagePanel;
    private TextMeshProUGUI _reengageText;
    private float _reengageCooldown = 0f;
    private const float ReengageCooldownNormal = 60f;
    private const float ReengageCooldownCalm = 120f;
    private const float ReengageIdleThreshold = 20f;

    // ==================== LIFECYCLE ====================

    private void OnDestroy()
    {
        currentRound = 0;
        _roundsCompleted = 0;

        // Destroy dynamically created RoundText so it doesn't leak across scenes
        if (roundText != null && roundText.gameObject != null)
            Destroy(roundText.gameObject);
    }

    private void Start()
    {
        // Load a random scenario from JSON
        LoadRandomScenario();

        // Load fun feature sprites
        _starFilledSprite = LoadSpriteFromResources("star_filled");
        _flameSprite = LoadSpriteFromResources("streak_flame");
        _jarSprite = LoadSpriteFromResources("savings_jar");
        _priceTagSprite = LoadSpriteFromResources("price_tag");
        _trophySprite = LoadSpriteFromResources("award_trophy");

        // Auto-find feedbackPanel if not wired in Inspector
        if (feedbackPanel == null)
        {
            // Try getting it from the UI manager
            if (uiManager != null && uiManager.feedbackPanel != null)
            {
                feedbackPanel = uiManager.feedbackPanel;
                Debug.Log("[Spending] feedbackPanel found via uiManager");
            }
            else
            {
                // Search scene by name
                Transform[] allTransforms = FindObjectsOfType<Transform>(true);
                foreach (Transform tr in allTransforms)
                {
                    if (tr.gameObject.name == "FeedbackPanel")
                    {
                        feedbackPanel = tr.gameObject;
                        Debug.Log("[Spending] feedbackPanel found by name search");
                        break;
                    }
                }
            }

            if (feedbackPanel == null)
                Debug.LogError("[Spending] feedbackPanel could not be found anywhere!");
        }

        // Fix all panel UI (colors, references, layout)
        SetupAllPanels();

        // Wire toggle listeners for base items
        foreach (var item in items)
        {
            if (item.toggle != null)
                item.toggle.onValueChanged.AddListener((bool _) => OnItemToggled());
        }

        // Auto-create round 3 extra items by cloning an existing treat row
        if (round3ExtraItems.Count == 0 && items.Count > 0)
            CreateRound3Items();

        // Wire toggle listeners for round 3 extras
        foreach (var item in round3ExtraItems)
        {
            if (item.toggle != null)
                item.toggle.onValueChanged.AddListener((bool _) => OnItemToggled());
        }

        // Auto-create round text if not wired (delayed 1 frame so canvas is ready)
        if (roundText == null)
            StartCoroutine(CreateRoundTextDelayed());

        // Fit items within the panel
        FitItemsInPanel();

        // Hide round 3 items initially
        SetRound3ItemsVisible(false);

        BuildReengagementPopup();

        StartRound(1);
        Debug.Log("SpendingGameController: Started with 3-round system!");
    }

    void Update()
    {
        if (!_roundInProgress) return;

        bool hasInput = Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (hasInput)
        {
            _idleTimer = 0f;
            return;
        }

        _idleTimer += Time.deltaTime;

        if (_idleCooldown > 0f) _idleCooldown -= Time.deltaTime;
        if (_reengageCooldown > 0f) _reengageCooldown -= Time.deltaTime;

        CheckReengagementTrigger();

        if (_idleTimer >= IdleThresholdSeconds)
        {
            if (GameSettings.CalmMode && _idleCooldown > 0f) return;
            if (PlayerModelService.Instance != null)
                PlayerModelService.Instance.RecordInactivity();
            _idleTimer = 0f;
            _idleCooldown = IdleThresholdSeconds;
        }
    }

    // ==================== RE-ENGAGEMENT POPUP ====================

    void BuildReengagementPopup()
    {
        var canvasGO = new GameObject("ReengageCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();

        _reengagePanel = new GameObject("ReengagePanel");
        _reengagePanel.transform.SetParent(canvasGO.transform, false);
        var panelRect = _reengagePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.30f);
        panelRect.anchorMax = new Vector2(0.85f, 0.70f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var bg = _reengagePanel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        bg.raycastTarget = true;

        // Find RoundedRect sprite
        Sprite roundedRect = null;
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
            if (s.name == "RoundedRect") { roundedRect = s; break; }
        if (roundedRect != null) { bg.sprite = roundedRect; bg.type = Image.Type.Sliced; }

        var textGO = new GameObject("ReengageText");
        textGO.transform.SetParent(_reengagePanel.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.55f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        _reengageText = textGO.AddComponent<TextMeshProUGUI>();
        _reengageText.fontSize = 32f;
        _reengageText.color = Color.white;
        _reengageText.alignment = TextAlignmentOptions.Center;
        _reengageText.raycastTarget = false;
        _reengageText.enableWordWrapping = true;

        CreateReengageButton("Take a hint", new Color(0.3f, 0.8f, 0.4f),
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.45f), OnTakeHint, roundedRect);
        CreateReengageButton("Keep going", new Color(0.5f, 0.6f, 0.8f),
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.45f), OnKeepGoing, roundedRect);

        _reengagePanel.SetActive(false);
    }

    void CreateReengageButton(string label, Color color, Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction action, Sprite sprite)
    {
        var btnGO = new GameObject(label + "Btn");
        btnGO.transform.SetParent(_reengagePanel.transform, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = btnGO.AddComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(action);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    void CheckReengagementTrigger()
    {
        if (!GameSettings.ShowHints) return;
        if (_reengagePanel == null || _reengagePanel.activeSelf) return;
        if (_reengageCooldown > 0f) return;
        if (!_roundInProgress) return;

        bool idleTrigger = _idleTimer >= ReengageIdleThreshold;
        bool streakTrigger = PlayerModelService.Instance != null
            && PlayerModelService.Instance.failedRoundsStreak >= 2;

        if (idleTrigger || streakTrigger)
        {
            string msg = idleTrigger
                ? "Take your time.\nCheck what you really need!"
                : "Tricky budget!\nFocus on essentials first.";
            ShowReengagementPopup(msg);
        }
    }

    void ShowReengagementPopup(string message)
    {
        if (_reengagePanel == null) return;
        if (_reengageText != null) _reengageText.text = message;
        _reengagePanel.transform.SetAsLastSibling();
        _reengagePanel.SetActive(true);
    }

    void DismissReengagementPopup()
    {
        if (_reengagePanel != null) _reengagePanel.SetActive(false);
        _reengageCooldown = GameSettings.CalmMode ? ReengageCooldownCalm : ReengageCooldownNormal;
        _idleTimer = 0f;
    }

    void OnTakeHint()
    {
        DismissReengagementPopup();
        string hint = currentRound switch
        {
            1 => "Essentials like bread and milk should come first!",
            2 => "Tight budget \u2014 skip the treats if you have to.",
            3 => "Payday is tempting, but saving some is smart!",
            _ => "Think about what you truly need."
        };
        if (duckReaction != null)
            duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, hint);
    }

    void OnKeepGoing()
    {
        DismissReengagementPopup();
    }

    // Recipe card UI (found at runtime)
    private TMP_Text _recipeText;
    private GameObject _recipeIcon;
    private bool _recipeNudged = false;
    private bool _feedbackTextNudged = false;
    private bool _scorecardTextNudged = false;
    private bool _totalTextNudged = false;

    // ==================== SCENARIO LOADING ====================

    void LoadRandomScenario()
    {
        var loader = SpendingScenarioLoader.Instance;
        _currentScenario = loader != null ? loader.GetNextScenario() : null;

        if (_currentScenario != null)
            Debug.Log("[Spending] Loaded scenario: " + _currentScenario.id + " (" + _currentScenario.seasonName + ")");
        else
            Debug.LogWarning("[Spending] No scenarios loaded — using fallback data");
    }

    float GetRoundBudget(int round)
    {
        if (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            return _currentScenario.rounds[round - 1].budget;
        return FallbackBudgets[round - 1];
    }

    string GetRoundName(int round)
    {
        if (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            return _currentScenario.rounds[round - 1].roundName;
        return FallbackNames[round - 1];
    }

    string GetRoundDuckLine(int round)
    {
        if (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            return _currentScenario.rounds[round - 1].duckLine;
        return FallbackDuckLines[round - 1];
    }

    string GetRoundDuckEmotion(int round)
    {
        if (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            return _currentScenario.rounds[round - 1].duckEmotion;
        return round == 2 ? "worried" : round == 3 ? "excited" : "neutral";
    }

    DuckReaction.Emotion ParseEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion)) return DuckReaction.Emotion.Neutral;
        switch (emotion.ToLower())
        {
            case "happy": return DuckReaction.Emotion.Happy;
            case "sad": return DuckReaction.Emotion.Sad;
            case "excited": return DuckReaction.Emotion.Excited;
            case "worried": return DuckReaction.Emotion.Worried;
            case "thinking": return DuckReaction.Emotion.Thinking;
            case "celebrating": return DuckReaction.Emotion.Celebrating;
            case "shocked": return DuckReaction.Emotion.Shocked;
            default: return DuckReaction.Emotion.Neutral;
        }
    }

    string GetRecipeName(int round)
    {
        if (_currentScenario != null && round - 1 < _currentScenario.rounds.Length
            && !string.IsNullOrEmpty(_currentScenario.rounds[round - 1].recipeName))
            return _currentScenario.rounds[round - 1].recipeName;
        return null;
    }

    void UpdateRecipeCard(int round)
    {
        // Find InstructionsText in the scene if we haven't cached it
        if (_recipeText == null)
        {
            GameObject instrGO = GameObject.Find("InstructionsText");
            if (instrGO != null)
                _recipeText = instrGO.GetComponent<TMP_Text>();
        }

        if (_recipeText == null) return;

        // Set left alignment for recipe card
        _recipeText.alignment = TextAlignmentOptions.Left;
        // Nudge recipe down 25px (once)
        if (!_recipeNudged)
        {
            _recipeNudged = true;
            RectTransform recipeRT = _recipeText.GetComponent<RectTransform>();
            if (recipeRT != null)
                recipeRT.anchoredPosition = new Vector2(recipeRT.anchoredPosition.x, recipeRT.anchoredPosition.y - 25f);
        }

        // Create cookbook icon next to recipe text (once)
        if (_recipeIcon == null)
        {
            Sprite menuSprite = Resources.Load<Sprite>("menu");
            if (menuSprite == null)
            {
                // Fallback: load as Texture2D and create sprite
                Texture2D tex = Resources.Load<Texture2D>("menu");
                if (tex != null)
                    menuSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            if (menuSprite != null)
            {
                _recipeIcon = new GameObject("RecipeIcon");
                _recipeIcon.transform.SetParent(_recipeText.transform, false);
                var rt = _recipeIcon.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-12f, 0f);
                rt.sizeDelta = new Vector2(91f, 91f);
                var img = _recipeIcon.AddComponent<Image>();
                img.sprite = menuSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        string recipe = GetRecipeName(round);
        SpendingRound roundData = (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            ? _currentScenario.rounds[round - 1] : null;

        if (recipe != null && roundData != null && roundData.essentials != null && roundData.essentials.Length > 0)
        {
            string ingredients = "";
            for (int i = 0; i < roundData.essentials.Length; i++)
            {
                if (i > 0) ingredients += ", ";
                ingredients += roundData.essentials[i].name;
            }
            _recipeText.text = "Recipe: " + recipe + "\nYou need: " + ingredients;
        }
        else
        {
            _recipeText.text = "Buy the essentials your family needs!";
        }
    }

    // ==================== ROUND SYSTEM ====================

    void StartRound(int round)
    {
        currentRound = round;
        _roundInProgress = true;
        _hasLoggedThisRound = false;
        Debug.Log("[Spending] === StartRound(" + round + ") called ===");

        // Set budget for this round (from JSON scenario or fallback)
        weeklyBudgetPounds = GetRoundBudget(round);

        // Update money counter budget
        if (moneyCounter != null)
        {
            moneyCounter.totalBudget = weeklyBudgetPounds;
            moneyCounter.SetSpent(0, false);
        }

        // Uncheck all items without triggering reactions
        _suppressToggleReaction = true;
        foreach (var it in items)
        {
            if (it.toggle != null)
                it.toggle.isOn = false;
        }
        foreach (var it in round3ExtraItems)
        {
            if (it.toggle != null)
                it.toggle.isOn = false;
        }
        _suppressToggleReaction = false;

        // Update treat item names/prices for this round
        UpdateTreatsForRound(round);

        // Randomize item order so essentials aren't always first
        ShuffleItemRows();

        // Show/hide round 3 extras
        SetRound3ItemsVisible(round == 3);

        // Fit items within panel
        FitItemsInPanel();

        // Update round text and make sure it's visible
        if (roundText != null)
        {
            roundText.gameObject.SetActive(true);
            roundText.text = "Round " + round + " of " + TotalRounds + ": " + GetRoundName(round);
        }

        // Show recipe card
        UpdateRecipeCard(round);

        // Clear fun feature displays from previous round
        ClearFunDisplays();

        // Hide feedback/consequence panels
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
        if (uiManager != null)
            uiManager.HideFeedbackPanel();

        // Show tutorial before round 1
        if (round == 1)
        {
            ShoppingListTutorial tut = FindObjectOfType<ShoppingListTutorial>(true);
            if (tut != null)
                tut.ShowTutorial();
        }

        // Duck intro for this round (from JSON scenario)
        if (duckReaction != null)
        {
            DuckReaction.Emotion emo = ParseEmotion(GetRoundDuckEmotion(round));
            duckReaction.ShowReaction(emo, GetRoundDuckLine(round));
        }

        Debug.Log("[Spending] Round " + round + " started: " + GetRoundName(round) + " (Budget: \u00a3" + weeklyBudgetPounds + ")");
    }

    void SetRound3ItemsVisible(bool visible)
    {
        foreach (var item in round3ExtraItems)
        {
            if (item.toggle != null)
                item.toggle.gameObject.SetActive(visible);
        }
    }

    void UpdateTreatsForRound(int round)
    {
        SpendingRound roundData = (_currentScenario != null && round - 1 < _currentScenario.rounds.Length)
            ? _currentScenario.rounds[round - 1] : null;

        // --- Update essentials ---
        string[] fallbackEssNames = { "Bread", "Milk", "Eggs", "Vegetables" };
        float[] fallbackEssPrices = { 1.20f, 1.10f, 1.80f, 2.00f };

        int essIndex = 0;
        foreach (var item in items)
        {
            if (item.isNecessary)
            {
                if (roundData != null && roundData.essentials != null && essIndex < roundData.essentials.Length)
                {
                    item.itemName = roundData.essentials[essIndex].name;
                    item.price = roundData.essentials[essIndex].price;
                }
                else if (essIndex < fallbackEssNames.Length)
                {
                    item.itemName = fallbackEssNames[essIndex];
                    item.price = fallbackEssPrices[essIndex];
                }
                UpdateItemUI(item);
                essIndex++;
            }
        }

        // --- Update treats ---
        string[] fallbackTreatNames = round == 2
            ? new[] { "Biscuits", "Juice" }
            : new[] { "Crisps", "Chocolate" };
        float[] fallbackTreatPrices = round == 2
            ? new[] { 1.00f, 1.30f }
            : new[] { 1.20f, 1.50f };

        SpendingTreat[] treats = roundData?.treats;

        int treatIndex = 0;
        foreach (var item in items)
        {
            if (!item.isNecessary && treatIndex < (treats != null ? treats.Length : fallbackTreatNames.Length))
            {
                if (treats != null)
                {
                    item.itemName = treats[treatIndex].name;
                    item.price = treats[treatIndex].price;
                }
                else
                {
                    item.itemName = fallbackTreatNames[treatIndex];
                    item.price = fallbackTreatPrices[treatIndex];
                }
                UpdateItemUI(item);
                treatIndex++;
            }
        }
        Debug.Log("[Spending] UpdateItemsForRound(" + round + "): " + essIndex + " essentials, " + treatIndex + " treats");
    }

    void UpdateItemUI(ShopItem item)
    {
        if (item.toggle == null) return;
        Transform row = item.toggle.transform.parent != null
            ? item.toggle.transform.parent : item.toggle.transform;
        var texts = row.GetComponentsInChildren<TMP_Text>(true);
        bool nameSet = false;
        bool priceSet = false;
        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLower();
            if (n.Contains("name") || n.Contains("item"))
            {
                t.text = item.itemName;
                nameSet = true;
            }
            else if (n.Contains("price"))
            {
                t.text = "\u00a3" + item.price.ToString("F2");
                priceSet = true;
            }
        }
        if (!nameSet && texts.Length >= 1)
            texts[0].text = item.itemName;
        if (!priceSet && texts.Length >= 2)
            texts[texts.Length - 1].text = "\u00a3" + item.price.ToString("F2");
    }

    void ShuffleItemRows()
    {
        // Shuffle the sibling order of item rows in the VerticalLayoutGroup
        // so essentials aren't always at the top
        if (items.Count == 0 || items[0].toggle == null) return;

        Transform container = items[0].toggle.transform.parent;
        if (container == null) return;
        // Go up one more if the toggle's parent is a row inside the container
        if (container.parent != null && container.parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null)
            container = container.parent;

        int childCount = container.childCount;
        for (int i = childCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            container.GetChild(j).SetSiblingIndex(i);
        }
    }

    // Returns all active items (base + round 3 extras if visible)
    List<ShopItem> GetActiveItems()
    {
        List<ShopItem> active = new List<ShopItem>(items);
        if (currentRound == 3)
            active.AddRange(round3ExtraItems);
        return active;
    }

    // ==================== ITEM TOGGLED ====================

    public void OnItemToggled()
    {
        if (_suppressToggleReaction) return;

        float total = CalculateCurrentTotal();
        int treatsCount = 0;
        int essentialsCount = 0;

        foreach (var it in GetActiveItems())
        {
            if (it.toggle != null && it.toggle.isOn)
            {
                if (!it.isNecessary)
                    treatsCount++;
                else
                    essentialsCount++;
            }
        }

        // Update money counter
        if (moneyCounter != null)
            moneyCounter.SetSpent(total, true);

        // Duck reactions
        if (duckReaction != null)
        {
            float remaining = weeklyBudgetPounds - total;
            bool calm = GameSettings.CalmMode;

            if (total > weeklyBudgetPounds)
            {
                string msg = calm ? "Try removing something." : "TOO MUCH!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Shocked;
                duckReaction.ShowReaction(emo, msg);
            }
            else if (treatsCount > 1)
            {
                string msg = calm ? "Maybe fewer treats?" : "Too many treats!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Worried;
                duckReaction.ShowReaction(emo, msg);
            }
            else if (remaining <= 1.5f && total > 0)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Almost done!");
            }
            else if (total >= 5f)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Good job!");
            }
            else if (total > 0f)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Excited, "Great!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Pick items");
            }
        }
    }

    // ==================== CONTINUE (submit round) ====================

    public void OnContinue()
    {
        // Ignore if no round is active (already submitted or double-call)
        if (!_roundInProgress)
        {
            Debug.LogWarning("[Spending] OnContinue ignored: round already submitted (currentRound=" + currentRound + ")");
            return;
        }
        _roundInProgress = false;
        Debug.Log("[Spending] OnContinue: submitting round " + currentRound + " (budget=£" + weeklyBudgetPounds + ")");

        float total = 0;
        int unnecessaryCount = 0;
        int necessaryCount = 0;

        foreach (var it in GetActiveItems())
        {
            if (it.toggle != null && it.toggle.isOn)
            {
                total += it.price;
                if (!it.isNecessary)
                    unnecessaryCount++;
                else
                    necessaryCount++;
            }
        }

        // Log spend to bank
        if (!_hasLoggedThisRound && BankAccountService.Instance != null)
        {
            _hasLoggedThisRound = true;
            string category = (unnecessaryCount == 0) ? "Needs" : "Mixed";
            BankAccountService.Instance.Spend(total, "Week " + currentRound + " Shopping", category);

            var hud = FindObjectOfType<BankHud>();
            if (hud != null) hud.Refresh();

            if (PlayerModelService.Instance != null)
            {
                int totalItems = necessaryCount + unnecessaryCount;
                PlayerModelService.Instance.RecordSpendingRound(total, weeklyBudgetPounds, unnecessaryCount, totalItems);
            }
        }

        int stars = CalculateStars(total, unnecessaryCount, necessaryCount);

        // Store round results
        _roundTotals[currentRound - 1] = total;
        _roundStars[currentRound - 1] = stars;
        _roundEssentials[currentRound - 1] = necessaryCount;
        _roundTreats[currentRound - 1] = unnecessaryCount;
        _roundsCompleted++;

        // Hide old star rating component (we use our own visual stars now)
        if (starRating != null)
            starRating.gameObject.SetActive(false);
        Debug.Log("[Spending] Round " + currentRound + " completed. Total rounds completed: " + _roundsCompleted);

        // Show fun features on feedback panel
        Transform funParent = feedbackPanel != null ? feedbackPanel.transform : null;
        ShowRoundStars(stars, funParent);
        UpdateStreakDisplay(stars, funParent);
        float roundSaved = total <= weeklyBudgetPounds ? (weeklyBudgetPounds - total) : 0f;
        UpdateSavingsJar(roundSaved, funParent);

        // Duck reaction
        ShowDuckFeedback(total, unnecessaryCount, necessaryCount);

        // Scorecard at top, commentary in body
        if (scorecardText != null)
        {
            scorecardText.text = BuildScorecard(total, unnecessaryCount, necessaryCount);
            // Push scorecard down to make room for stars/jar (once)
            if (!_scorecardTextNudged)
            {
                _scorecardTextNudged = true;
                RectTransform scRT = scorecardText.GetComponent<RectTransform>();
                if (scRT != null)
                    scRT.anchoredPosition = new Vector2(scRT.anchoredPosition.x, scRT.anchoredPosition.y - 200f);
            }
        }
        if (feedbackText != null)
        {
            feedbackText.text = BuildCommentary(total, unnecessaryCount, necessaryCount);
            // Push feedback text down to make room for stars/streak/jar (once)
            if (!_feedbackTextNudged)
            {
                _feedbackTextNudged = true;
                RectTransform ftRT = feedbackText.GetComponent<RectTransform>();
                if (ftRT != null)
                    ftRT.anchoredPosition = new Vector2(ftRT.anchoredPosition.x, ftRT.anchoredPosition.y - 200f);
            }
        }

        if (totalText != null)
        {
            totalText.gameObject.SetActive(true);
            totalText.text = string.Format("Total: \u00a3{0:F2}", total);
            if (!_totalTextNudged)
            {
                _totalTextNudged = true;
                RectTransform ttRT = totalText.GetComponent<RectTransform>();
                if (ttRT != null)
                    ttRT.anchoredPosition = new Vector2(ttRT.anchoredPosition.x, ttRT.anchoredPosition.y - 30f);
            }
        }

        // Hide round counter so it doesn't overlap stars
        if (roundText != null)
            roundText.gameObject.SetActive(false);

        // Show feedback panel (NO consequence panel yet — save for final)
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
            feedbackPanel.transform.SetAsLastSibling(); // render on top

            // Ensure CanvasGroup is fully visible
            CanvasGroup cg = feedbackPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            // Wire ALL buttons in feedback panel to AdvanceRound + update label
            Button[] btns = feedbackPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in btns)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(AdvanceRound);

                TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                    btnLabel.text = currentRound < TotalRounds ? "Next Round" : "See Results";
            }
            EnlargePanelButtons(btns);
            Debug.Log("[Spending] Feedback panel shown: active=" + feedbackPanel.activeSelf
                + " pos=" + feedbackPanel.transform.position
                + " scale=" + feedbackPanel.transform.localScale
                + " buttons=" + btns.Length);
        }
        else
        {
            Debug.LogError("[Spending] feedbackPanel is NULL — cannot show feedback!");
        }

        if (uiManager != null)
        {
            string title = total <= weeklyBudgetPounds ? "Good Job!" : "Over Budget";
            uiManager.ShowFeedbackPanel(title, BuildCommentary(total, unnecessaryCount, necessaryCount), string.Format("Total: \u00a3{0:F2}", total));
        }
    }

    // Called by "Continue" button on the feedback panel to advance rounds
    public void AdvanceRound()
    {
        // Use _roundsCompleted (monotonically increasing) as source of truth
        int nextRound = _roundsCompleted + 1;
        Debug.Log("[Spending] AdvanceRound called. roundsCompleted=" + _roundsCompleted + " → next=" + nextRound);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (uiManager != null)
            uiManager.HideFeedbackPanel();

        if (nextRound <= TotalRounds)
        {
            StartRound(nextRound);
        }
        else
        {
            Debug.Log("[Spending] All rounds complete, showing final summary");
            ShowFinalSummary();
        }
    }

    // ==================== FINAL SUMMARY ====================

    void ShowFinalSummary()
    {
        // Debug: log all round data
        for (int i = 0; i < TotalRounds; i++)
            Debug.Log("[Spending] Final — Round " + (i+1) + ": spent=£" + _roundTotals[i].ToString("F2") + " budget=£" + GetRoundBudget(i+1) + " stars=" + _roundStars[i]);

        float totalSaved = 0f;
        float totalOverspent = 0f;
        float totalBudgetAll = 0f;

        for (int i = 0; i < TotalRounds; i++)
        {
            float budget = GetRoundBudget(i + 1);
            float spent = _roundTotals[i];
            totalBudgetAll += budget;

            if (spent <= budget)
                totalSaved += (budget - spent);
            else
                totalOverspent += (spent - budget);
        }

        // Overall stars = average of round stars, rounded
        int starSum = 0;
        for (int i = 0; i < TotalRounds; i++)
            starSum += _roundStars[i];
        int overallStars = Mathf.RoundToInt((float)starSum / TotalRounds);

        // Duck final reaction
        if (duckReaction != null)
        {
            if (overallStars >= 3)
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Amazing shopper!");
            else if (overallStars >= 2)
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Well done!");
            else if (overallStars >= 1)
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Room to improve!");
            else
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Try again!");
        }

        // Hide per-round text elements — ConsequencePanel handles the full breakdown
        if (totalText != null)
            totalText.gameObject.SetActive(false);
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
        if (scorecardText != null)
            scorecardText.gameObject.SetActive(false);

        // Show consequence panel with combined results
        int roundsAllEssentials = 0;
        int totalTreats = 0;
        for (int i = 0; i < TotalRounds; i++)
        {
            if (_roundEssentials[i] >= 4) roundsAllEssentials++;
            totalTreats += _roundTreats[i];
        }

        if (consequencePanel != null)
            consequencePanel.ShowFinalConsequences(totalSaved, totalOverspent, overallStars, roundsAllEssentials, totalTreats);

        // Show award + final savings jar on feedback panel
        string awardTitle = GetAwardTitle(overallStars, roundsAllEssentials, totalTreats, totalSaved);
        Transform awardParent = feedbackPanel != null ? feedbackPanel.transform : null;
        ShowAward(awardTitle, awardParent);
        // Keep savings jar visible in final view
        UpdateSavingsJar(0f, awardParent);

        if (roundText != null)
            roundText.text = "Season Complete!";

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
            feedbackPanel.transform.SetAsLastSibling();

            CanvasGroup cg = feedbackPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            // Hide old buttons
            Button[] btns = feedbackPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in btns)
                btn.gameObject.SetActive(false);

            // Add Home + Play Again buttons
            EndGameButtons.Create(feedbackPanel.transform, ResetGame, 0f);
        }

        if (uiManager != null)
            uiManager.ShowFeedbackPanel("Season Complete!", "", "");
    }

    // ==================== DUCK FEEDBACK ====================

    void ShowDuckFeedback(float total, int unnecessaryCount, int necessaryCount)
    {
        if (duckReaction == null) return;

        bool withinBudget = (total <= weeklyBudgetPounds);
        bool allEssentials = (necessaryCount == 4);
        bool oneTreat = (unnecessaryCount == 1);
        bool calm = GameSettings.CalmMode;

        if (allEssentials && oneTreat && withinBudget)
            duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "PERFECT!");
        else if (allEssentials && unnecessaryCount == 0 && withinBudget)
            duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Excellent!");
        else if (allEssentials && withinBudget)
            duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Good!");
        else if (total > weeklyBudgetPounds)
        {
            string msg = calm ? "A bit over \u2014 try removing a treat." : "Over budget!";
            DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Shocked;
            duckReaction.ShowReaction(emo, msg);
        }
        else if (unnecessaryCount > 1 && withinBudget)
        {
            string msg = calm ? "Try picking just one treat." : "Too many treats!";
            DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Worried;
            duckReaction.ShowReaction(emo, msg);
        }
        else if (!allEssentials && withinBudget)
        {
            string msg = calm ? "Don't forget the essentials!" : "Missing essentials!";
            DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Neutral : DuckReaction.Emotion.Sad;
            duckReaction.ShowReaction(emo, msg);
        }
        else
        {
            string msg = calm ? "Give it another go!" : "Try again!";
            duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, msg);
        }
    }

    // ==================== HELPERS ====================

    void EnlargePanelButtons(Button[] btns)
    {
        foreach (Button btn in btns)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 150f);
            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 150f;
            le.preferredHeight = 150f;
        }
    }

    private float CalculateCurrentTotal()
    {
        float total = 0f;
        foreach (var it in GetActiveItems())
        {
            if (it.toggle != null && it.toggle.isOn)
                total += it.price;
        }
        return total;
    }

    private int CalculateStars(float total, int unnecessaryCount, int necessaryCount)
    {
        bool allEssentials = necessaryCount >= 4;
        bool withinBudget = total <= weeklyBudgetPounds;

        if (GameSettings.CalmMode)
        {
            if (allEssentials && withinBudget) return 3;
            if (allEssentials && total <= weeklyBudgetPounds + 2f) return 2;
            if (necessaryCount >= 3 && withinBudget) return 1;
            return 0;
        }

        // Essentials first: saving money while going hungry scores poorly
        if (allEssentials && withinBudget && unnecessaryCount <= 1) return 3;
        if (allEssentials && withinBudget) return 2;
        if ((allEssentials && total <= weeklyBudgetPounds + 2f) ||
            (necessaryCount >= 3 && withinBudget)) return 1;
        return 0;
    }

    private string BuildScorecard(float total, int unnecessaryCount, int necessaryCount)
    {
        int missingEssentials = 4 - necessaryCount;
        bool allEssentials = missingEssentials <= 0;
        bool withinBudget = total <= weeklyBudgetPounds;
        float remaining = weeklyBudgetPounds - total;
        float over = total - weeklyBudgetPounds;

        string header = "Round " + currentRound + " of " + TotalRounds
                      + "  |  " + GetRoundName(currentRound)
                      + "  |  Budget: \u00a3" + weeklyBudgetPounds.ToString("F2") + "\n";

        string recipe = GetRecipeName(currentRound);
        string essLine = allEssentials
            ? "Recipe: " + (recipe ?? "Dinner") + " \u2714"
            : "Recipe: " + (recipe ?? "Dinner") + " \u2014 " + necessaryCount + " / 4 ingredients";
        string treatLine = unnecessaryCount == 0 ? "Treats: None"
            : unnecessaryCount == 1              ? "Treats: 1"
                                                 : "Treats: " + unnecessaryCount;
        string budgetLine = withinBudget
            ? string.Format("Budget: \u00a3{0:F2} spent  (saved \u00a3{1:F2})", total, remaining)
            : string.Format("Budget: \u00a3{0:F2} spent  (over by \u00a3{1:F2})", total, over);

        return header + essLine + "     " + treatLine + "     " + budgetLine;
    }

    private string BuildCommentary(float total, int unnecessaryCount, int necessaryCount)
    {
        string recipe = GetRecipeName(currentRound);
        string recipeRef = recipe != null ? recipe : "dinner";

        // Calculate recipe cost vs takeaway for real-world comparison
        float recipeCost = 0f;
        foreach (var it in items)
            if (it.isNecessary) recipeCost += it.price;
        float takeawayCost = Mathf.Round(recipeCost * 2.2f);
        float saving = takeawayCost - recipeCost;

        int missingEssentials = 4 - necessaryCount;
        bool allEssentials = missingEssentials <= 0;
        bool withinBudget = total <= weeklyBudgetPounds;
        float remaining = weeklyBudgetPounds - total;
        float over = total - weeklyBudgetPounds;

        string tip = GetMoneyTip(unnecessaryCount, remaining);

        // === EMPTY BASKET ===
        if (total == 0)
            return "Empty basket! Takeaway tonight: \u00a3" + takeawayCost.ToString("F0")
                + ".\nThat\u2019s \u00a3" + saving.ToString("F2") + " wasted vs cooking at home.";

        // === MISSING INGREDIENTS ===
        if (!allEssentials)
        {
            string miss = missingEssentials == 1
                ? "1 ingredient short for " + recipeRef + "!"
                : missingEssentials + " ingredients short for " + recipeRef + "!";
            string cost = "Takeaway costs \u00a3" + saving.ToString("F2") + " more than cooking.";
            if (!withinBudget)
                return miss + " And over budget by \u00a3" + over.ToString("F2") + ".\n" + cost;
            return miss + "\n" + cost;
        }

        // === ALL INGREDIENTS ===
        string win = recipeRef + " sorted! Saved \u00a3" + saving.ToString("F2") + " vs takeaway.";

        if (!withinBudget)
            return win + "\nBut over budget by \u00a3" + over.ToString("F2") + " \u2014 drop a treat!";

        if (unnecessaryCount > 2)
            return win + "\n" + unnecessaryCount + " treats though \u2014 one is plenty!";

        if (unnecessaryCount == 1)
            return win + " Plus a treat \u2014 nice balance!\n" + tip;

        if (unnecessaryCount == 0 && remaining > 0.5f)
            return win + " And \u00a3" + remaining.ToString("F2") + " saved!\n" + tip;

        // On round 2, swap money tip for a price compare fact
        if (currentRound == 2)
            return win + "\n" + GetPriceCompareTip();

        return win + "\n" + tip;
    }

    private static readonly string[] MoneyTips = {
        "Planning meals before shopping saves money.",
        "A shopping list helps you avoid impulse buys.",
        "Buying only what you need means more savings.",
        "Even saving \u00a31 a week adds up to \u00a352 a year!",
        "Needs first, wants second \u2014 that\u2019s smart budgeting.",
        "Comparing prices helps stretch your budget further.",
        "Leftovers make great lunches \u2014 less waste, more savings.",
        "Treats are fine in moderation \u2014 one is enough!",
        "Saving a little each week builds a safety net.",
        "Sticking to your list is a superpower.",
        "Cooking at home is cheaper than takeaway.",
        "A budget isn\u2019t a punishment \u2014 it\u2019s a plan."
    };

    string GetMoneyTip(int unnecessaryCount, float remaining)
    {
        // Pick a tip based on round + scenario for consistency
        int seed = currentRound;
        if (_currentScenario != null)
            seed += _currentScenario.id.GetHashCode();
        int idx = Mathf.Abs(seed) % MoneyTips.Length;
        return "Tip: " + MoneyTips[idx];
    }

    // ==================== LEGACY / PUBLIC ====================

    public void ShowConsequences()
    {
        float total = CalculateCurrentTotal();

        if (uiManager != null)
        {
            float moneyPercent = total / 100f;
            float budgetPercent = total / weeklyBudgetPounds;

            string title = total <= weeklyBudgetPounds ? "Financial Impact: Positive" : "Financial Impact: Warning";
            string message = total <= weeklyBudgetPounds
                ? "You stayed within budget!"
                : string.Format("You overspent by \u00a3{0:F2}.", total - weeklyBudgetPounds);

            uiManager.HideFeedbackPanel();
            uiManager.ShowConsequencePanel(title, message, moneyPercent, budgetPercent);
        }
    }

    public void CloseFeedback()
    {
        // This is called by the Inspector-wired button on the feedback panel.
        // Instead of just closing, advance to next round.
        AdvanceRound();
    }

    public void ResetGame()
    {
        // Load a new random scenario for the next game
        LoadRandomScenario();

        currentRound = 0;
        _roundInProgress = false;
        _roundsCompleted = 0;
        _roundTotals = new float[3];
        _roundStars = new int[3];
        _roundEssentials = new int[3];
        _roundTreats = new int[3];
        _hasLoggedThisRound = false;
        _streakCount = 0;
        _totalSavingsJar = 0f;
        ClearFunDisplays();

        _suppressToggleReaction = true;
        foreach (var it in items)
        {
            if (it.toggle != null)
                it.toggle.isOn = false;
        }
        foreach (var it in round3ExtraItems)
        {
            if (it.toggle != null)
                it.toggle.isOn = false;
        }
        _suppressToggleReaction = false;

        if (moneyCounter != null)
            moneyCounter.SetSpent(0, false);

        if (duckReaction != null)
            duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Ready to shop!");

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);

            // Destroy EndGameButtons container left over from final summary
            Transform endBtns = feedbackPanel.transform.Find("EndGameButtons");
            if (endBtns != null)
                Destroy(endBtns.gameObject);

            // Re-show original buttons that were hidden during final summary
            Button[] btns = feedbackPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in btns)
                btn.gameObject.SetActive(true);
        }

        // Hide consequence panel and re-enable text elements hidden during final view
        if (consequencePanel != null)
            consequencePanel.gameObject.SetActive(false);
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(true);
        if (scorecardText != null)
            scorecardText.gameObject.SetActive(true);
        if (totalText != null)
            totalText.gameObject.SetActive(true);

        StartRound(1);
        Debug.Log("[Spending] Game reset!");
    }

    // ==================== UI SETUP (runtime) ====================

    void SetupAllPanels()
    {
        Color darkText = new Color(0.15f, 0.15f, 0.2f);
        Color accentColor = new Color(0.2f, 0.4f, 0.8f);

        // ---- Fix FeedbackPanel ----
        if (feedbackPanel != null)
        {
            // Solid dark background (fully opaque)
            Image panelImg = feedbackPanel.GetComponent<Image>();
            if (panelImg != null)
                panelImg.color = new Color(0.12f, 0.14f, 0.22f, 1f);

            // Find text objects by name and wire references
            TMP_Text[] allTexts = feedbackPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in allTexts)
            {
                string n = t.gameObject.name.ToLower();
                if (n.Contains("title") && !n.Contains("star"))
                {
                    t.color = Color.white;
                    t.fontSize = 36;
                    t.fontStyle = FontStyles.Normal;
                    t.alignment = TextAlignmentOptions.TopLeft;
                    if (scorecardText == null)
                        scorecardText = t;
                }
                else if (n.Contains("message") || (n.Contains("feedback") && !n.Contains("title")))
                {
                    t.color = Color.white;
                    t.fontSize = 44;
                    if (feedbackText == null)
                        feedbackText = t;
                }
                else if (n.Contains("total"))
                {
                    t.color = new Color(1f, 0.9f, 0.3f);
                    t.fontSize = 44;
                    t.fontStyle = FontStyles.Bold;
                    if (totalText == null)
                        totalText = t;
                }
            }

            // Style buttons (no position changes — keep scene layout)
            Button[] btns = feedbackPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in btns)
            {
                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = new Color(0.2f, 0.6f, 0.3f);
                TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                {
                    btnLabel.color = Color.white;
                    btnLabel.fontSize = 44;
                    btnLabel.fontStyle = FontStyles.Bold;
                }
            }

            EnlargePanelButtons(btns);

            // Auto-wire starRating if not set in Inspector
            if (starRating == null)
            {
                Transform starPanel = feedbackPanel.transform.Find("StarRatingPanel");
                if (starPanel != null)
                    starRating = starPanel.GetComponent<StarRating>();
            }

            Debug.Log("[Spending] FeedbackPanel setup done. feedbackText=" + (feedbackText != null) + " totalText=" + (totalText != null));
        }

        // ---- Fix TutorialPanel text colors (white on white = invisible) ----
        GameObject tutPanel = null;
        ShoppingListTutorial tut = FindObjectOfType<ShoppingListTutorial>(true);
        if (tut != null)
            tutPanel = tut.tutorialPanel;
        if (tutPanel == null)
        {
            // Search by name
            Transform[] all = FindObjectsOfType<Transform>(true);
            foreach (Transform tr in all)
            {
                if (tr.gameObject.name == "TutorialPanel")
                {
                    tutPanel = tr.gameObject;
                    break;
                }
            }
        }

        if (tutPanel != null)
        {
            // Solid opaque background
            Image tutPanelImg = tutPanel.GetComponent<Image>();
            if (tutPanelImg != null)
                tutPanelImg.color = new Color(0.12f, 0.14f, 0.22f, 1f);

            Transform contentCard = tutPanel.transform.Find("ContentCard");
            if (contentCard != null)
            {
                Image cardImg = contentCard.GetComponent<Image>();
                if (cardImg != null)
                    cardImg.color = new Color(0.12f, 0.14f, 0.22f, 1f);
            }

            // Fix all text in tutorial to be readable
            TMP_Text[] tutTexts = tutPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in tutTexts)
            {
                string n = t.gameObject.name.ToLower();
                if (n.Contains("title"))
                {
                    t.color = new Color(1f, 0.85f, 0.2f);
                    t.fontSize = 44;
                    t.fontStyle = FontStyles.Bold;
                }
                else if (n.Contains("instruction"))
                {
                    t.color = Color.white;
                    t.fontSize = 44;
                    // Override text so it doesn't mention a fixed budget
                    t.text = "Pick the items your family needs for the week.\n\n"
                        + "Essentials like milk, eggs, and vegetables should come first.\n\n"
                        + "Your budget changes each round -- spend wisely!";
                }
                else
                {
                    t.color = Color.white;
                    t.fontSize = 44;
                }
            }

            // Style AND wire tutorial buttons — override any Inspector wiring
            // so they ONLY close the tutorial (not call OnContinue)
            Button[] tutBtns = tutPanel.GetComponentsInChildren<Button>(true);
            GameObject tutRef = tutPanel; // capture for closure
            foreach (Button btn in tutBtns)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (tutRef != null) tutRef.SetActive(false);
                });

                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = new Color(0.2f, 0.6f, 0.3f);
                TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                {
                    btnLabel.color = Color.white;
                    btnLabel.fontSize = 44;
                    btnLabel.fontStyle = FontStyles.Bold;
                }
            }
            EnlargePanelButtons(tutBtns);

            Debug.Log("[Spending] TutorialPanel colors fixed");
        }

        // ---- Fix ConsequencePanel ----
        if (consequencePanel != null)
        {
            Image cpImg = consequencePanel.GetComponent<Image>();
            if (cpImg != null)
                cpImg.color = new Color(0.10f, 0.12f, 0.20f, 1f);

            TMP_Text[] cpTexts = consequencePanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in cpTexts)
            {
                string n = t.gameObject.name.ToLower();
                t.fontSize = 44;
                t.color = Color.white;
                if (n.Contains("title") || n.Contains("amount") || n.Contains("saving") || n.Contains("debt"))
                    t.fontStyle = FontStyles.Bold;
            }

            // Style buttons (no position changes — keep scene layout)
            Button[] cpBtns = consequencePanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in cpBtns)
            {
                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = new Color(0.2f, 0.6f, 0.3f);
                TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                {
                    btnLabel.color = Color.white;
                    btnLabel.fontSize = 44;
                    btnLabel.fontStyle = FontStyles.Bold;
                }
            }
            EnlargePanelButtons(cpBtns);

            Debug.Log("[Spending] ConsequencePanel styled");
        }
    }

    void FitItemsInPanel()
    {
        StartCoroutine(FitItemsNextFrame());
    }

    IEnumerator FitItemsNextFrame()
    {
        // Wait one frame so Unity has calculated layout rects
        yield return null;

        if (items.Count == 0 || items[0].toggle == null) yield break;

        VerticalLayoutGroup vlg = items[0].toggle.GetComponentInParent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            Debug.LogWarning("[Spending] No VLG found");
            yield break;
        }

        RectTransform containerRect = vlg.GetComponent<RectTransform>();
        if (containerRect == null) yield break;

        // Use rect.height (actual rendered size) instead of sizeDelta
        float containerHeight = containerRect.rect.height;
        if (containerHeight <= 0) containerHeight = containerRect.sizeDelta.y;
        if (containerHeight <= 0) yield break;

        // Count visible rows (direct children of VLG that are active)
        List<RectTransform> visibleRows = new List<RectTransform>();
        for (int i = 0; i < vlg.transform.childCount; i++)
        {
            Transform child = vlg.transform.GetChild(i);
            if (child.gameObject.activeSelf)
                visibleRows.Add(child.GetComponent<RectTransform>());
        }

        int count = visibleRows.Count;
        if (count == 0) yield break;

        // Keep VLG NOT controlling height — we set it manually
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;

        // Use 85% of container so items don't reach the bottom
        float usableHeight = containerHeight * 0.85f;
        float topPad = 6f;
        float bottomPad = 6f;
        float spacing = 4f;
        float available = usableHeight - topPad - bottomPad - (count - 1) * spacing;
        float rowHeight = available / count;

        // Apply padding/spacing
        vlg.padding = new RectOffset(10, 10, (int)topPad, (int)bottomPad);
        vlg.spacing = spacing;

        // Set each row's height and scale text to fit
        float fontSize = Mathf.Clamp(rowHeight * 0.5f, 18f, 32f);
        foreach (RectTransform row in visibleRows)
        {
            row.sizeDelta = new Vector2(row.sizeDelta.x, rowHeight);

            // Scale text within each item row
            TMP_Text[] rowTexts = row.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in rowTexts)
            {
                t.fontSize = fontSize;
                t.enableAutoSizing = false;
            }
        }

        // Force rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

        Debug.Log("[Spending] FitItems: container=" + containerHeight + "px, " + count + " rows, rowH=" + rowHeight.ToString("F1") + ", fontSize=" + fontSize.ToString("F0"));
    }

    void CreateRound3Items()
    {
        // Find an existing treat row to clone (use last item as template)
        Toggle templateToggle = null;
        foreach (var item in items)
        {
            if (item.toggle != null && !item.isNecessary)
            {
                templateToggle = item.toggle;
                break;
            }
        }

        if (templateToggle == null && items.Count > 0 && items[0].toggle != null)
            templateToggle = items[0].toggle;

        if (templateToggle == null)
        {
            Debug.LogWarning("[Spending] No item row to clone for Round 3 extras.");
            return;
        }

        Transform parent = templateToggle.transform.parent;
        if (parent == null) parent = templateToggle.transform;

        // If the toggle is inside a row container, clone the row container
        // Check if the parent has a layout group (meaning it's the row)
        Transform rowTemplate = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null
            ? parent : templateToggle.transform;

        // Get extra treats from scenario (round 3) or use fallback
        SpendingTreat[] extras = null;
        if (_currentScenario != null && _currentScenario.rounds.Length >= 3)
            extras = _currentScenario.rounds[2].extraTreats;

        string[] extraNames = (extras != null && extras.Length > 0)
            ? System.Array.ConvertAll(extras, t => t.name)
            : new[] { "Toy", "Video Game", "Sweets" };
        float[] extraPrices = (extras != null && extras.Length > 0)
            ? System.Array.ConvertAll(extras, t => t.price)
            : new[] { 4.00f, 5.00f, 2.00f };

        for (int i = 0; i < extraNames.Length; i++)
        {
            GameObject clone = Instantiate(rowTemplate.gameObject, rowTemplate.parent);
            clone.name = extraNames[i] + "Row";

            // Find and update text children
            TMPro.TextMeshProUGUI[] texts = clone.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                string nameLower = txt.gameObject.name.ToLower();
                if (nameLower.Contains("name") || nameLower.Contains("item"))
                    txt.text = extraNames[i];
                else if (nameLower.Contains("price"))
                    txt.text = "\u00a3" + extraPrices[i].ToString("F2");
            }

            // If only 2 texts found, assume first=name, second=price
            if (texts.Length >= 2)
            {
                // Check if texts weren't set by name matching
                bool nameSet = false;
                foreach (var txt in texts)
                {
                    if (txt.text == extraNames[i]) { nameSet = true; break; }
                }
                if (!nameSet)
                {
                    texts[0].text = extraNames[i];
                    if (texts.Length >= 2)
                        texts[1].text = "\u00a3" + extraPrices[i].ToString("F2");
                }
            }

            // Get the toggle
            Toggle newToggle = clone.GetComponentInChildren<Toggle>(true);
            if (newToggle != null)
            {
                newToggle.isOn = false;
                round3ExtraItems.Add(new ShopItem
                {
                    itemName = extraNames[i],
                    price = extraPrices[i],
                    isNecessary = false,
                    toggle = newToggle
                });
            }

            clone.SetActive(false);
        }

        Debug.Log("[Spending] Created " + round3ExtraItems.Count + " Round 3 extra items at runtime.");
    }

    IEnumerator CreateRoundTextDelayed()
    {
        yield return null; // wait one frame for canvas to be ready
        if (roundText == null)
            CreateRoundText();
        // Update text for current round after creation
        if (roundText != null)
            roundText.text = "Round " + currentRound + " of " + TotalRounds + ": " + GetRoundName(currentRound);
    }

    void CreateRoundText()
    {
        // Create round text near the top of the scene canvas (avoid DontDestroyOnLoad canvases)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.gameObject.scene == gameObject.scene)
                {
                    canvas = c;
                    break;
                }
            }
        }
        if (canvas == null) return;

        GameObject go = new GameObject("RoundText");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.85f);
        rect.anchorMax = new Vector2(0.95f, 0.93f);
        rect.offsetMin = new Vector2(0f, 20f);
        rect.offsetMax = new Vector2(0f, 20f);

        roundText = go.AddComponent<TMPro.TextMeshProUGUI>();
        roundText.text = "Round 1 of 3";
        roundText.fontSize = 34;
        roundText.color = new Color(0.15f, 0.15f, 0.15f);
        roundText.fontStyle = TMPro.FontStyles.Bold;
        roundText.alignment = TMPro.TextAlignmentOptions.Center;
        roundText.raycastTarget = false;
    }

    // ==================== FUN FEATURES ====================

    Sprite LoadSpriteFromResources(string name)
    {
        Sprite s = Resources.Load<Sprite>(name);
        if (s != null) return s;
        Texture2D tex = Resources.Load<Texture2D>(name);
        if (tex != null)
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return null;
    }

    // --- STAR DISPLAY (after each round checkout) ---
    void ShowRoundStars(int stars, Transform parent)
    {
        // Destroy previous star display
        if (_starContainer != null) Destroy(_starContainer);

        if (_starFilledSprite == null || parent == null) return;

        _starContainer = new GameObject("StarDisplay");
        _starContainer.transform.SetParent(parent, false);
        var rt = _starContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(480f, 140f);

        var hlg = _starContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < 3; i++)
        {
            var starGO = new GameObject("Star" + (i + 1));
            starGO.transform.SetParent(_starContainer.transform, false);
            var starRT = starGO.AddComponent<RectTransform>();
            starRT.sizeDelta = new Vector2(120f, 120f);
            var img = starGO.AddComponent<Image>();
            img.sprite = _starFilledSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            // Filled = gold, empty = dark grey
            img.color = (i < stars) ? new Color(1f, 0.85f, 0f) : new Color(0.25f, 0.25f, 0.3f);
        }
    }

    // --- STREAK DISPLAY ---
    void UpdateStreakDisplay(int stars, Transform parent)
    {
        if (stars >= 3)
            _streakCount++;
        else
            _streakCount = 0;

        // Destroy previous
        if (_streakDisplay != null) Destroy(_streakDisplay);

        if (_streakCount < 2 || _flameSprite == null || parent == null) return;

        _streakDisplay = new GameObject("StreakDisplay");
        _streakDisplay.transform.SetParent(parent, false);
        var rt = _streakDisplay.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -8f);
        rt.sizeDelta = new Vector2(160f, 50f);

        var hlg = _streakDisplay.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Flame icon
        var flameGO = new GameObject("Flame");
        flameGO.transform.SetParent(_streakDisplay.transform, false);
        var flameRT = flameGO.AddComponent<RectTransform>();
        flameRT.sizeDelta = new Vector2(40f, 40f);
        var flameImg = flameGO.AddComponent<Image>();
        flameImg.sprite = _flameSprite;
        flameImg.preserveAspect = true;
        flameImg.raycastTarget = false;

        // Streak text
        var textGO = new GameObject("StreakText");
        textGO.transform.SetParent(_streakDisplay.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(110f, 40f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = _streakCount + " streak!";
        tmp.fontSize = 28f;
        tmp.color = new Color(1f, 0.6f, 0.1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    // --- SAVINGS JAR ---
    void UpdateSavingsJar(float roundSaved, Transform parent)
    {
        if (roundSaved > 0f)
            _totalSavingsJar += roundSaved;

        // Destroy previous
        if (_savingsJarDisplay != null) Destroy(_savingsJarDisplay);

        if (_totalSavingsJar <= 0f || _jarSprite == null || parent == null) return;

        _savingsJarDisplay = new GameObject("SavingsJar");
        _savingsJarDisplay.transform.SetParent(parent, false);
        var rt = _savingsJarDisplay.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(12f, -8f);
        rt.sizeDelta = new Vector2(420f, 190f);

        var hlg = _savingsJarDisplay.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Jar icon (4.5x original)
        var jarGO = new GameObject("Jar");
        jarGO.transform.SetParent(_savingsJarDisplay.transform, false);
        var jarRT = jarGO.AddComponent<RectTransform>();
        jarRT.sizeDelta = new Vector2(180f, 180f);
        var jarImg = jarGO.AddComponent<Image>();
        jarImg.sprite = _jarSprite;
        jarImg.preserveAspect = true;
        jarImg.raycastTarget = false;

        // Savings text
        var textGO = new GameObject("JarText");
        textGO.transform.SetParent(_savingsJarDisplay.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(220f, 180f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "\u00a3" + _totalSavingsJar.ToString("F2") + " saved";
        tmp.fontSize = 48f;
        tmp.color = new Color(0.4f, 0.9f, 0.5f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    // --- PRICE COMPARE TIPS ---
    static readonly string[] PriceCompareFacts = {
        "Own-brand beans cost 50p less than branded ones!",
        "Frozen veg is just as nutritious and half the price.",
        "Buying loose fruit is often cheaper per item.",
        "Store-brand cereal can save you \u00a31+ per box.",
        "Checking unit prices helps you spot the best deal.",
        "Bulk rice & pasta are cheaper per portion.",
        "Seasonal fruit & veg costs less and tastes better.",
        "Supermarket meal deals aren't always the best value."
    };

    string GetPriceCompareTip()
    {
        int idx = (currentRound + (_currentScenario != null ? _currentScenario.id.GetHashCode() : 0));
        idx = Mathf.Abs(idx) % PriceCompareFacts.Length;
        return PriceCompareFacts[idx];
    }

    // --- END-GAME AWARDS ---
    string GetAwardTitle(int overallStars, int roundsAllEssentials, int totalTreats, float totalSaved)
    {
        float avgTreats = totalTreats / 3f;

        // Priority order: best to worst
        if (overallStars >= 3 && roundsAllEssentials == 3 && avgTreats <= 1f)
            return "Budget Boss";
        if (overallStars >= 3 && roundsAllEssentials == 3)
            return "Smart Shopper";
        if (roundsAllEssentials == 3 && totalSaved > 3f)
            return "Super Saver";
        if (roundsAllEssentials == 3)
            return "Recipe Master";
        if (totalSaved > 2f)
            return "Careful Spender";
        if (avgTreats > 2f)
            return "Treat Lover";
        if (overallStars >= 2)
            return "Good Effort";
        return "Keep Trying";
    }

    Color GetAwardColor(string award)
    {
        switch (award)
        {
            case "Budget Boss": return new Color(1f, 0.85f, 0f); // gold
            case "Smart Shopper": return new Color(0.75f, 0.75f, 0.8f); // silver
            case "Super Saver": return new Color(0.4f, 0.9f, 0.5f); // green
            case "Recipe Master": return new Color(0.5f, 0.8f, 1f); // blue
            case "Careful Spender": return new Color(0.7f, 0.85f, 0.6f); // light green
            case "Treat Lover": return new Color(1f, 0.5f, 0.7f); // pink
            case "Good Effort": return new Color(0.8f, 0.65f, 0.4f); // bronze
            default: return new Color(0.6f, 0.6f, 0.65f); // grey
        }
    }

    void ShowAward(string awardTitle, Transform parent)
    {
        if (_awardDisplay != null) Destroy(_awardDisplay);
        if (_trophySprite == null || parent == null) return;

        Color awardColor = GetAwardColor(awardTitle);

        _awardDisplay = new GameObject("AwardDisplay");
        _awardDisplay.transform.SetParent(parent, false);
        var rt = _awardDisplay.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 12f);
        rt.sizeDelta = new Vector2(320f, 80f);

        var hlg = _awardDisplay.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Trophy icon
        var trophyGO = new GameObject("Trophy");
        trophyGO.transform.SetParent(_awardDisplay.transform, false);
        var trophyRT = trophyGO.AddComponent<RectTransform>();
        trophyRT.sizeDelta = new Vector2(60f, 60f);
        var trophyImg = trophyGO.AddComponent<Image>();
        trophyImg.sprite = _trophySprite;
        trophyImg.preserveAspect = true;
        trophyImg.raycastTarget = false;
        trophyImg.color = awardColor;

        // Award text
        var textGO = new GameObject("AwardText");
        textGO.transform.SetParent(_awardDisplay.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(240f, 70f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = awardTitle;
        tmp.fontSize = 36f;
        tmp.color = awardColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    void ClearFunDisplays()
    {
        if (_starContainer != null) Destroy(_starContainer);
        if (_streakDisplay != null) Destroy(_streakDisplay);
        if (_savingsJarDisplay != null) Destroy(_savingsJarDisplay);
        if (_awardDisplay != null) Destroy(_awardDisplay);
    }

}
