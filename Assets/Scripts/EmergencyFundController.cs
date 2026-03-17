using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmergencyFundController : MonoBehaviour
{
    [Header("UI Flow")]
    public EmergencyFundUIFlow uiFlow;

    [Header("Duck & Visuals")]
    public DuckReaction duckReaction;
    public DuckReactionBackgroundChanger backgroundChanger;
    public StarRating starRating;

    [Header("Game Constants")]
    public int totalWeeks = 6;
    public int weeklyIncome = 100;
    public int weeklyEssentials = 40;
    public int weeklyAvailable = 60;
    public int emergencyFundGoal = 160;
    public int tierSmall = 20;
    public int tierBalanced = 30;
    public int tierStrong = 40;

    // Week type progression: educational pacing from easy → hard
    private static readonly string[] WeekEventTypes = {
        "normal",      // Week 1: gentle start
        "choice",      // Week 2: social decision
        "emergency",   // Week 3: surprise cost
        "bonus",       // Week 4: positive reward
        "emergency",   // Week 5: bigger challenge
        "crisis"       // Week 6: hardest
    };

    // Game state
    private int currentWeek = 0;
    private int emergencyFundBalance = 0;  // session savings (starts at 0 each game)
    private int _persistentFundBalance = 0; // total fund across all games
    private bool _wentIntoDebt = false;
    private int _totalSaved = 0;
    private int _weeksPrepared = 0;
    private EmergencyFundEvent _currentEvent = null;
    private string _lastEventId = null;

    // Calm mode timing
    private float TransitionDelay => GameSettings.CalmMode ? 1.5f : 0f;

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

    void Start()
    {
        Debug.Log("[EFController] Start. uiFlow=" + (uiFlow != null ? "OK" : "NULL"));

        // Ensure EventLoader exists
        var _ = EventLoader.Instance;

        ResetState();
        BuildReengagementPopup();

        uiFlow.OnTutorialDone = () => Invoke("StartNewWeek", 0.3f);
        uiFlow.ShowTutorial();
    }

    void OnDestroy()
    {
        CancelInvoke();
    }

    void Update()
    {
        bool hasInput = Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (hasInput)
        {
            _idleTimer = 0f;
            return;
        }

        _idleTimer += Time.deltaTime;

        if (_idleCooldown > 0f)
            _idleCooldown -= Time.deltaTime;

        if (_reengageCooldown > 0f)
            _reengageCooldown -= Time.deltaTime;

        CheckReengagementTrigger();

        if (_idleTimer >= IdleThresholdSeconds)
        {
            if (GameSettings.CalmMode && _idleCooldown > 0f)
                return;

            if (PlayerModelService.Instance != null)
                PlayerModelService.Instance.RecordInactivity();

            _idleTimer = 0f;
            _idleCooldown = IdleThresholdSeconds;
        }
    }

    // ==================== WEEK LOOP ====================

    void StartNewWeek()
    {
        currentWeek++;

        if (currentWeek > totalWeeks)
        {
            ShowFinalResults();
            return;
        }

        // Emergency bank accounting (silent — uses separate emergency balance)
        if (BankAccountService.Instance != null)
        {
            BankAccountService.Instance.EarnEmergency(weeklyIncome, "Week " + currentWeek + " income");
            BankAccountService.Instance.SpendEmergency(weeklyEssentials, "Week " + currentWeek + " essentials", "Needs");
        }

        RefreshHUD();

        // Show saving tier prompt
        uiFlow.OnTierChosen = ProcessTier;
        uiFlow.ShowSavingTier(currentWeek, weeklyAvailable, emergencyFundBalance, emergencyFundGoal);

        SetBackground("decide");
        DuckSay(DuckReaction.Emotion.Thinking, "Choose wisely!");
    }

    void ProcessTier(int tier)
    {
        _idleTimer = 0f;

        // Move savings from bank to fund
        emergencyFundBalance += tier;
        _totalSaved += tier;

        if (BankAccountService.Instance != null)
            BankAccountService.Instance.SpendEmergency(tier, "Emergency fund deposit", "Emergency");

        // Leftover stays in emergency bank
        int leftover = weeklyAvailable - tier;
        if (leftover > 0 && BankAccountService.Instance != null)
            BankAccountService.Instance.EarnEmergency(leftover, "Week " + currentWeek + " leftover");

        SaveFund();

        // Log to player model
        if (PlayerModelService.Instance != null)
        {
            Debug.Log("[EF] Week " + currentWeek + " tier: \u00a3" + tier);
            PlayerModelService.Instance.RecordEmergencyFundRound(tier, tierStrong);
        }

        // Duck feedback on tier
        if (tier >= tierStrong)
            DuckSay(DuckReaction.Emotion.Celebrating, "Strong saver!");
        else if (tier >= tierBalanced)
            DuckSay(DuckReaction.Emotion.Happy, "Balanced choice!");
        else
            DuckSay(DuckReaction.Emotion.Neutral, "Every bit counts.");

        RefreshHUD();

        // Load random event from JSON for this week
        Invoke("ShowWeekEvent", TransitionDelay);
    }

    // ==================== JSON-DRIVEN EVENTS ====================

    void ShowWeekEvent()
    {
        string eventType = WeekEventTypes[currentWeek - 1];
        _currentEvent = EventLoader.Instance.GetEvent(eventType, _lastEventId);

        if (_currentEvent == null)
        {
            // Fallback: no events of this type, skip to feedback
            Debug.LogWarning("[EF] No event found for type: " + eventType);
            if (emergencyFundBalance > 0) _weeksPrepared++;
            ShowWeekFeedback("Quiet week. Fund: \u00a3" + emergencyFundBalance);
            return;
        }

        _lastEventId = _currentEvent.id;
        Debug.Log("[EF] Week " + currentWeek + " event: " + _currentEvent.id + " (" + _currentEvent.type + ")");

        // Show duck emotion from JSON
        DuckSay(ParseEmotion(_currentEvent.duckEmotion), _currentEvent.duckLine);

        // Set background based on event category
        switch (_currentEvent.type)
        {
            case "emergency":
                SetBackground("emergency");
                ShowEmergencyEvent();
                break;
            case "bonus":
            case "lucky":
                SetBackground("bonus");
                ShowBonusEvent();
                break;
            case "crisis":
                SetBackground("emergency");
                ShowCrisisEvent();
                break;
            case "choice":
                SetBackground("choice");
                ShowChoiceEvent();
                break;
            default: // "normal"
                SetBackground("payday");
                ShowNormalEvent();
                break;
        }
    }

    // --- EMERGENCY: mandatory cost, fund-first ---
    void ShowEmergencyEvent()
    {
        int cost = _currentEvent.costPounds;
        string body = _currentEvent.description + "\n\nFund: \u00a3" + emergencyFundBalance;

        string choiceA = _currentEvent.choices.Length > 0 ? _currentEvent.choices[0].label : "Pay \u00a3" + cost;
        string choiceB = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].label : null;

        uiFlow.OnChoiceA = () => HandleEmergency(cost, _currentEvent.title);
        if (choiceB != null)
        {
            // Second choice = skip the cost (e.g. "Use it cracked")
            uiFlow.OnChoiceB = () =>
            {
                _idleTimer = 0f;
                string flavour = _currentEvent.choices[1].flavourText;
                DuckSay(DuckReaction.Emotion.Neutral, flavour != null ? flavour : "Skipped.");
                if (emergencyFundBalance > 0) _weeksPrepared++;
                RefreshHUD();
                ShowWeekFeedback(flavour != null ? flavour : "You decided to skip.");
            };
        }

        uiFlow.ShowEvent(currentWeek, _currentEvent.title, body, choiceA, choiceB);
    }

    // --- BONUS / LUCKY: extra money, choose to save or spend ---
    void ShowBonusEvent()
    {
        int bonus = _currentEvent.bonusPounds;

        // Credit bonus to emergency bank
        if (BankAccountService.Instance != null)
            BankAccountService.Instance.EarnEmergency(bonus, _currentEvent.title);

        string body = _currentEvent.description + "\n\nFund: \u00a3" + emergencyFundBalance;

        string choiceA = _currentEvent.choices.Length > 0 ? _currentEvent.choices[0].label : "Add to fund";
        string choiceB = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].label : "Keep in bank";

        uiFlow.OnChoiceA = () =>
        {
            _idleTimer = 0f;
            if (BankAccountService.Instance != null)
                BankAccountService.Instance.SpendEmergency(bonus, "Bonus to fund", "Emergency");
            emergencyFundBalance += bonus;
            _totalSaved += bonus;
            SaveFund();
            DuckSay(DuckReaction.Emotion.Celebrating, "Fund boosted!");
            RefreshHUD();
            if (emergencyFundBalance > 0) _weeksPrepared++;
            string flavour = _currentEvent.choices.Length > 0 ? _currentEvent.choices[0].flavourText : "Added to fund!";
            ShowWeekFeedback(flavour);
        };
        uiFlow.OnChoiceB = () =>
        {
            _idleTimer = 0f;
            DuckSay(DuckReaction.Emotion.Neutral, "Kept the bonus.");
            RefreshHUD();
            if (emergencyFundBalance > 0) _weeksPrepared++;
            string flavour = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].flavourText : "Kept in bank.";
            ShowWeekFeedback(flavour);
        };

        uiFlow.ShowEvent(currentWeek, _currentEvent.title, body, choiceA, choiceB);
    }

    // --- CRISIS: mandatory cost from fund/bank ---
    void ShowCrisisEvent()
    {
        int cost = _currentEvent.costPounds;
        string body = _currentEvent.description + "\n\nFund: \u00a3" + emergencyFundBalance;

        string choiceA = _currentEvent.choices.Length > 0 ? _currentEvent.choices[0].label : "Pay \u00a3" + cost;
        string choiceB = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].label : null;

        uiFlow.OnChoiceA = () => HandleEmergency(cost, _currentEvent.title);
        if (choiceB != null)
        {
            uiFlow.OnChoiceB = () =>
            {
                _idleTimer = 0f;
                // savePounds on choice B = the partial cost for this option
                int partialCost = _currentEvent.choices.Length > 1
                    ? _currentEvent.choices[1].savePounds : 0;
                if (partialCost > 0)
                    HandleEmergency(partialCost, _currentEvent.title + " (partial)");
                else
                {
                    string flavour = _currentEvent.choices[1].flavourText;
                    DuckSay(DuckReaction.Emotion.Neutral, flavour != null ? flavour : "Managed it.");
                    if (emergencyFundBalance > 0) _weeksPrepared++;
                    RefreshHUD();
                    ShowWeekFeedback(flavour != null ? flavour : "You got through it.");
                }
            };
        }

        uiFlow.ShowEvent(currentWeek, _currentEvent.title, body, choiceA, choiceB);
    }

    // --- CHOICE: optional spend (social/lifestyle) ---
    void ShowChoiceEvent()
    {
        int cost = _currentEvent.costPounds;
        string body = _currentEvent.description + "\n\nFund: \u00a3" + emergencyFundBalance;

        string choiceA = _currentEvent.choices.Length > 0 ? _currentEvent.choices[0].label : "Spend \u00a3" + cost;
        string choiceB = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].label : "Skip";

        uiFlow.OnChoiceA = () => HandleEmergency(cost, _currentEvent.title);
        uiFlow.OnChoiceB = () =>
        {
            _idleTimer = 0f;
            string flavour = _currentEvent.choices.Length > 1 ? _currentEvent.choices[1].flavourText : "Saved the money.";
            DuckSay(DuckReaction.Emotion.Neutral, flavour);
            RefreshHUD();
            if (emergencyFundBalance > 0) _weeksPrepared++;
            ShowWeekFeedback(flavour);
        };

        uiFlow.ShowEvent(currentWeek, _currentEvent.title, body, choiceA, choiceB);
    }

    // --- NORMAL: no cost, quiet week ---
    void ShowNormalEvent()
    {
        string body = _currentEvent.description + "\n\nFund: \u00a3" + emergencyFundBalance;

        string flavour = (_currentEvent.choices != null && _currentEvent.choices.Length > 0)
            ? _currentEvent.choices[0].flavourText : null;

        uiFlow.OnChoiceA = () =>
        {
            _idleTimer = 0f;
            DuckSay(DuckReaction.Emotion.Happy, flavour != null ? flavour : "Steady week!");
            if (emergencyFundBalance > 0) _weeksPrepared++;
            RefreshHUD();
            ShowWeekFeedback(flavour != null ? flavour : "Quiet week. Fund: \u00a3" + emergencyFundBalance);
        };
        uiFlow.ShowEvent(currentWeek, _currentEvent.title, body, "Continue", null);
    }

    // ==================== EMERGENCY HANDLER ====================

    void HandleEmergency(int cost, string description)
    {
        _idleTimer = 0f;

        int coveredByFund = Mathf.Min(emergencyFundBalance, cost);
        emergencyFundBalance -= coveredByFund;
        int remaining = cost - coveredByFund;

        string result = "";
        if (coveredByFund > 0)
            result = "Fund covered \u00a3" + coveredByFund + ".";

        if (remaining > 0)
        {
            float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetEmergencyBalance() : 0f;
            if (bank >= remaining)
            {
                BankAccountService.Instance.SpendEmergency(remaining, description, "Emergency");
                result += " Bank paid \u00a3" + remaining + ".";
            }
            else
            {
                int fromBank = (int)bank;
                if (fromBank > 0 && BankAccountService.Instance != null)
                    BankAccountService.Instance.SpendEmergency(fromBank, description, "Emergency");
                int deficit = remaining - fromBank;
                _wentIntoDebt = true;
                result += " Short \u00a3" + deficit + " \u2014 debt!";
            }
        }

        SaveFund();

        // Duck reaction based on outcome
        if (coveredByFund >= cost)
            DuckSay(DuckReaction.Emotion.Celebrating, "Fund saved you!");
        else if (!_wentIntoDebt)
            DuckSay(DuckReaction.Emotion.Worried, "That was close...");
        else
            DuckSay(DuckReaction.Emotion.Sad, "You're in debt...");

        // Use flavourText from JSON if available
        if (_currentEvent != null && _currentEvent.choices.Length > 0
            && !string.IsNullOrEmpty(_currentEvent.choices[0].flavourText))
        {
            result += "\n" + _currentEvent.choices[0].flavourText;
        }

        result += "\nFund: \u00a3" + emergencyFundBalance;

        if (emergencyFundBalance > 0) _weeksPrepared++;
        RefreshHUD();
        ShowWeekFeedback(result);
    }

    // ==================== FEEDBACK ====================

    void ShowWeekFeedback(string message)
    {
        string title = currentWeek >= totalWeeks ? "Week " + currentWeek + " Done!" : "Week " + currentWeek + " Complete";

        if (currentWeek >= totalWeeks)
            uiFlow.OnContinue = ShowFinalResults;
        else
            uiFlow.OnContinue = () => Invoke("StartNewWeek", TransitionDelay);

        uiFlow.ShowFeedback(title, message);
    }

    // ==================== FINAL RESULTS ====================

    void ShowFinalResults()
    {
        Debug.Log("[EF] Season ended! Fund: \u00a3" + emergencyFundBalance + " Debt: " + _wentIntoDebt);

        if (PlayerModelService.Instance != null)
            PlayerModelService.Instance.RecordEmergencyFundRound(emergencyFundBalance, emergencyFundGoal);

        int stars = CalculateStars();

        if (starRating != null)
            starRating.SetRating(stars);

        if (stars >= 3)
        {
            SetBackground("perfect");
            DuckSay(DuckReaction.Emotion.Celebrating, "Amazing season!");
        }
        else if (stars >= 2)
        {
            SetBackground("perfect");
            DuckSay(DuckReaction.Emotion.Happy, "Well done!");
        }
        else if (stars >= 1)
        {
            SetBackground("gameover");
            DuckSay(DuckReaction.Emotion.Neutral, "Room to improve!");
        }
        else
        {
            SetBackground("gameover");
            DuckSay(DuckReaction.Emotion.Worried, "Try again!");
        }

        // Commit session savings to persistent fund
        _persistentFundBalance += emergencyFundBalance;
        PlayerPrefs.SetInt("EmergencyFundBalance", _persistentFundBalance);
        PlayerPrefs.Save();

        string line1 = "This session: \u00a3" + emergencyFundBalance + " saved (goal: \u00a3" + emergencyFundGoal + ")";
        string line2 = "Total emergency fund: \u00a3" + _persistentFundBalance;
        string line3 = "Weeks you stayed prepared: " + _weeksPrepared + " / " + totalWeeks;

        uiFlow.OnFinish = RestartGame;
        uiFlow.ShowFinal(line1, line2, line3);
    }

    int CalculateStars()
    {
        if (!_wentIntoDebt && emergencyFundBalance > 0) return 3;
        if (!_wentIntoDebt) return 2;
        if (_totalSaved > 0) return 1;
        return 0;
    }

    // ==================== HELPERS ====================

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

    void RefreshHUD()
    {
        float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetEmergencyBalance() : 0f;
        uiFlow.UpdateHUD(bank, emergencyFundBalance, emergencyFundGoal);

        // Refresh both balance HUDs
        BankHud bankHud = FindObjectOfType<BankHud>();
        if (bankHud != null)
            bankHud.Refresh();

        EmergencyBankHud emergHud = FindObjectOfType<EmergencyBankHud>();
        if (emergHud != null)
            emergHud.Refresh();
    }

    void SaveFund()
    {
        PlayerPrefs.SetInt("EmergencyFundBalance", _persistentFundBalance + emergencyFundBalance);
        PlayerPrefs.Save();
    }

    void DuckSay(DuckReaction.Emotion emotion, string message)
    {
        if (duckReaction != null)
            duckReaction.ShowReaction(emotion, message);
    }

    void SetBackground(string category)
    {
        if (backgroundChanger == null) return;
        switch (category)
        {
            case "payday":    backgroundChanger.SetPayDay(); break;
            case "decide":    backgroundChanger.SetDecideWisely(); break;
            case "bonus":     backgroundChanger.SetBonusWeek(); break;
            case "emergency": backgroundChanger.SetUseYourFund(); break;
            case "choice":    backgroundChanger.SetTwoThings(); break;
            case "gameover":  backgroundChanger.SetGameOver(); break;
            case "perfect":   backgroundChanger.SetPerfect(); break;
        }
    }

    void ResetState()
    {
        currentWeek = 0;
        _persistentFundBalance = PlayerPrefs.GetInt("EmergencyFundBalance", 0);
        emergencyFundBalance = 0;
        _wentIntoDebt = false;
        _totalSaved = 0;
        _weeksPrepared = 0;
        _currentEvent = null;
        _lastEventId = null;
    }

    // ==================== RESTART ====================

    public void RestartGame()
    {
        ResetState();
        RefreshHUD();
        DuckSay(DuckReaction.Emotion.Neutral, "Let's try again!");
        Invoke("StartNewWeek", TransitionDelay);
    }

    [ContextMenu("Reset Everything (Tutorial + Game)")]
    public void ResetEverything()
    {
        PlayerPrefs.DeleteKey("EmergencyTutorialSeen");
        PlayerPrefs.DeleteKey("EmergencyFundTutorialShown");
        PlayerPrefs.DeleteKey("EmergencyFundBalance");
        PlayerPrefs.Save();
        Debug.Log("[EF] Reset complete — tutorial will show on next play.");
    }

    // ==================== RE-ENGAGEMENT POPUP ====================

    void BuildReengagementPopup()
    {
        GameObject canvasGO = new GameObject("ReengageCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();

        _reengagePanel = new GameObject("ReengagePanel");
        _reengagePanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = _reengagePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.30f);
        panelRect.anchorMax = new Vector2(0.85f, 0.70f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = _reengagePanel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        bg.raycastTarget = true;

        GameObject textGO = new GameObject("ReengageText");
        textGO.transform.SetParent(_reengagePanel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.55f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _reengageText = textGO.AddComponent<TextMeshProUGUI>();
        _reengageText.text = "Take your time with this decision.";
        _reengageText.fontSize = 26f;
        _reengageText.color = Color.white;
        _reengageText.alignment = TextAlignmentOptions.Center;
        _reengageText.raycastTarget = false;
        _reengageText.enableWordWrapping = true;

        CreateReengageButton("Take a hint", new Color(0.3f, 0.8f, 0.4f),
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.45f), OnTakeHint);
        CreateReengageButton("Keep going", new Color(0.5f, 0.6f, 0.8f),
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.45f), OnKeepGoing);

        _reengagePanel.SetActive(false);
    }

    void CreateReengageButton(string label, Color color, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGO = new GameObject(label + "Btn");
        btnGO.transform.SetParent(_reengagePanel.transform, false);
        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = btnGO.AddComponent<Image>();
        img.color = color;

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(action);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    void CheckReengagementTrigger()
    {
        if (_reengagePanel == null || _reengagePanel.activeSelf) return;
        if (_reengageCooldown > 0f) return;
        if (currentWeek <= 0 || currentWeek > totalWeeks) return;

        bool idleTrigger = _idleTimer >= ReengageIdleThreshold;
        bool streakTrigger = PlayerModelService.Instance != null
            && PlayerModelService.Instance.failedRoundsStreak >= 2;

        if (idleTrigger || streakTrigger)
        {
            ShowReengagementPopup(idleTrigger
                ? "Take your time.\nThere's no wrong answer!"
                : "Tough stretch!\nSaving is hard \u2014 keep going!");
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
        DuckSay(DuckReaction.Emotion.Thinking,
            _currentEvent != null ? _currentEvent.duckLine : "Think about what matters most.");
    }

    void OnKeepGoing()
    {
        DismissReengagementPopup();
    }
}
