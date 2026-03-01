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
    public int emergencyFundGoal = 400;
    public int tierSmall = 20;
    public int tierBalanced = 30;
    public int tierStrong = 40;

    // Game state
    private int currentWeek = 0;
    private int emergencyFundBalance = 0;
    private bool _wentIntoDebt = false;
    private int _totalSaved = 0;
    private int _weeksPrepared = 0;
    private int _crisisPhase = 0;
    private string _pendingFeedback = "";

    // Calm mode timing
    private float TransitionDelay => GameSettings.CalmMode ? 2.5f : 1.5f;

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
        ResetState();
        BuildReengagementPopup();

        uiFlow.OnTutorialDone = () => Invoke("StartNewWeek", 0.3f);
        uiFlow.ShowTutorial();
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

        // Bank accounting (silent)
        if (BankAccountService.Instance != null)
        {
            BankAccountService.Instance.Earn(weeklyIncome, "Week " + currentWeek + " income");
            BankAccountService.Instance.Spend(weeklyEssentials, "Week " + currentWeek + " essentials", "Needs");
        }

        RefreshHUD();

        // Show saving tier prompt
        uiFlow.OnTierChosen = ProcessTier;
        uiFlow.ShowSavingTier(currentWeek, weeklyAvailable, emergencyFundBalance, emergencyFundGoal);

        DuckSay(DuckReaction.Emotion.Thinking, "Choose wisely!");
    }

    void ProcessTier(int tier)
    {
        _idleTimer = 0f;

        // Move savings from bank to fund
        emergencyFundBalance += tier;
        _totalSaved += tier;

        if (BankAccountService.Instance != null)
            BankAccountService.Instance.Spend(tier, "Emergency fund deposit", "Emergency");

        // Leftover stays in bank
        int leftover = weeklyAvailable - tier;
        if (leftover > 0 && BankAccountService.Instance != null)
            BankAccountService.Instance.Earn(leftover, "Week " + currentWeek + " leftover");

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

        // Route to week event or straight to feedback
        switch (currentWeek)
        {
            case 3: Invoke("ShowWeek3Event", TransitionDelay); break;
            case 4: Invoke("ShowWeek4Event", TransitionDelay); break;
            case 5: Invoke("ShowWeek5Event", TransitionDelay); break;
            case 6: _crisisPhase = 0; Invoke("ShowWeek6Event", TransitionDelay); break;
            default:
                // Weeks 1 & 2: no event, straight to feedback
                if (emergencyFundBalance > 0) _weeksPrepared++;
                ShowWeekFeedback("Saved \u00a3" + tier + ". Fund: \u00a3" + emergencyFundBalance);
                break;
        }
    }

    // ==================== WEEK EVENTS ====================

    void ShowWeek3Event()
    {
        uiFlow.OnChoiceA = () => HandleEmergency(30, "Phone repair");
        uiFlow.ShowEvent(currentWeek, "Emergency!",
            "Phone screen cracked.\nRepair: \u00a330\n\nFund: \u00a3" + emergencyFundBalance,
            "Pay \u00a330", null);

        if (emergencyFundBalance >= 30)
            DuckSay(DuckReaction.Emotion.Thinking, "Use your fund!");
        else
            DuckSay(DuckReaction.Emotion.Shocked, "Unexpected cost!");
    }

    void ShowWeek4Event()
    {
        if (BankAccountService.Instance != null)
            BankAccountService.Instance.Earn(40, "Week 4 bonus");

        uiFlow.OnChoiceA = () =>
        {
            _idleTimer = 0f;
            if (BankAccountService.Instance != null)
                BankAccountService.Instance.Spend(40, "Bonus to fund", "Emergency");
            emergencyFundBalance += 40;
            _totalSaved += 40;
            SaveFund();
            DuckSay(DuckReaction.Emotion.Celebrating, "Fund boosted!");
            RefreshHUD();
            if (emergencyFundBalance > 0) _weeksPrepared++;
            ShowWeekFeedback("Added \u00a340 bonus to fund!");
        };
        uiFlow.OnChoiceB = () =>
        {
            _idleTimer = 0f;
            DuckSay(DuckReaction.Emotion.Neutral, "Kept the bonus.");
            RefreshHUD();
            if (emergencyFundBalance > 0) _weeksPrepared++;
            ShowWeekFeedback("Kept \u00a340 bonus in bank.");
        };

        uiFlow.ShowEvent(currentWeek, "Bonus!",
            "Extra \u00a340 this week!\n\nFund: \u00a3" + emergencyFundBalance,
            "Add to fund", "Keep in bank");

        DuckSay(DuckReaction.Emotion.Excited, "Bonus week!");
    }

    void ShowWeek5Event()
    {
        uiFlow.OnChoiceA = () => HandleEmergency(60, "Laptop repair");
        uiFlow.ShowEvent(currentWeek, "Emergency!",
            "Laptop broke.\nRepair: \u00a360\n\nFund: \u00a3" + emergencyFundBalance,
            "Pay \u00a360", null);

        if (emergencyFundBalance >= 60)
            DuckSay(DuckReaction.Emotion.Thinking, "Your fund can cover this!");
        else
            DuckSay(DuckReaction.Emotion.Worried, "This is a big one...");
    }

    void ShowWeek6Event()
    {
        if (_crisisPhase == 0)
        {
            uiFlow.OnChoiceA = () => HandleCrisisTravel(true);
            uiFlow.OnChoiceB = () => HandleCrisisTravel(false);
            uiFlow.ShowEvent(currentWeek, "Crisis!",
                "Family event coming up.\nTravel: \u00a320\n\nFund: \u00a3" + emergencyFundBalance,
                "Pay \u00a320", "Skip trip");

            DuckSay(DuckReaction.Emotion.Worried, "Tough week ahead...");
        }
        else
        {
            uiFlow.OnChoiceA = () => HandleCrisisSocial(true);
            uiFlow.OnChoiceB = () => HandleCrisisSocial(false);
            uiFlow.ShowEvent(currentWeek, "Crisis!",
                "Friends invite you out.\nCost: \u00a320\n\nFund: \u00a3" + emergencyFundBalance,
                "Go (\u00a320)", "Stay home");

            DuckSay(DuckReaction.Emotion.Thinking, "One more decision...");
        }
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
            float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetBalance() : 0f;
            if (bank >= remaining)
            {
                BankAccountService.Instance.Spend(remaining, description, "Emergency");
                result += " Bank paid \u00a3" + remaining + ".";
            }
            else
            {
                int fromBank = (int)bank;
                if (fromBank > 0 && BankAccountService.Instance != null)
                    BankAccountService.Instance.Spend(fromBank, description, "Emergency");
                int deficit = remaining - fromBank;
                _wentIntoDebt = true;
                result += " Short \u00a3" + deficit + " \u2014 debt!";
            }
        }

        SaveFund();

        // Duck reaction
        if (coveredByFund >= cost)
            DuckSay(DuckReaction.Emotion.Celebrating, "Fund saved you!");
        else if (!_wentIntoDebt)
            DuckSay(DuckReaction.Emotion.Worried, "That was close...");
        else
            DuckSay(DuckReaction.Emotion.Sad, "You're in debt...");

        if (emergencyFundBalance > 0) _weeksPrepared++;
        RefreshHUD();
        ShowWeekFeedback(result);
    }

    // ==================== CRISIS HANDLERS ====================

    void HandleCrisisTravel(bool pay)
    {
        _idleTimer = 0f;

        if (pay)
        {
            float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetBalance() : 0f;
            if (bank >= 20)
            {
                BankAccountService.Instance.Spend(20, "Family event travel", "Needs");
                DuckSay(DuckReaction.Emotion.Neutral, "Family comes first.");
            }
            else
            {
                int canPay = (int)bank;
                if (canPay > 0 && BankAccountService.Instance != null)
                    BankAccountService.Instance.Spend(canPay, "Family event travel", "Needs");
                _wentIntoDebt = true;
                DuckSay(DuckReaction.Emotion.Sad, "Tight budget...");
            }
        }
        else
        {
            DuckSay(DuckReaction.Emotion.Sad, "Missed the family event...");
        }

        _crisisPhase = 1;
        RefreshHUD();
        Invoke("ShowWeek6Event", TransitionDelay);
    }

    void HandleCrisisSocial(bool go)
    {
        _idleTimer = 0f;

        if (go)
        {
            float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetBalance() : 0f;
            if (bank >= 20)
            {
                BankAccountService.Instance.Spend(20, "Going out with friends", "Social");
                DuckSay(DuckReaction.Emotion.Happy, "Great time out!");
            }
            else
            {
                int canPay = (int)bank;
                if (canPay > 0 && BankAccountService.Instance != null)
                    BankAccountService.Instance.Spend(canPay, "Going out with friends", "Social");
                _wentIntoDebt = true;
                DuckSay(DuckReaction.Emotion.Sad, "Couldn't quite afford it...");
            }
        }
        else
        {
            DuckSay(DuckReaction.Emotion.Neutral, "Saved \u00a320.");
        }

        if (emergencyFundBalance > 0) _weeksPrepared++;
        RefreshHUD();
        ShowWeekFeedback("End of the season!");
    }

    // ==================== FEEDBACK ====================

    void ShowWeekFeedback(string message)
    {
        string title = currentWeek >= totalWeeks ? "Week " + currentWeek + " Done!" : "Week " + currentWeek + " Complete";

        if (currentWeek >= totalWeeks)
        {
            uiFlow.OnContinue = ShowFinalResults;
        }
        else
        {
            uiFlow.OnContinue = () => Invoke("StartNewWeek", TransitionDelay);
        }

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

        // Duck celebration
        if (stars >= 3)
            DuckSay(DuckReaction.Emotion.Celebrating, "Amazing season!");
        else if (stars >= 2)
            DuckSay(DuckReaction.Emotion.Happy, "Well done!");
        else if (stars >= 1)
            DuckSay(DuckReaction.Emotion.Neutral, "Room to improve!");
        else
            DuckSay(DuckReaction.Emotion.Worried, "Try again!");

        string line1 = "Emergency fund saved: \u00a3" + emergencyFundBalance + " / \u00a3" + emergencyFundGoal;
        string line2 = "Weeks you stayed prepared: " + _weeksPrepared + " / " + totalWeeks;
        string line3 = "Tip: Saving early makes surprises manageable.";

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

    void RefreshHUD()
    {
        float bank = BankAccountService.Instance != null ? BankAccountService.Instance.GetBalance() : 0f;
        uiFlow.UpdateHUD(bank, emergencyFundBalance, emergencyFundGoal);
    }

    void SaveFund()
    {
        PlayerPrefs.SetInt("EmergencyFundBalance", emergencyFundBalance);
        PlayerPrefs.Save();
    }

    void DuckSay(DuckReaction.Emotion emotion, string message)
    {
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(emotion, message);
            if (backgroundChanger != null)
                backgroundChanger.CheckAndChangeBackground(message);
        }
    }

    void ResetState()
    {
        currentWeek = 0;
        emergencyFundBalance = 0;
        _wentIntoDebt = false;
        _totalSaved = 0;
        _weeksPrepared = 0;
        _crisisPhase = 0;

        PlayerPrefs.SetInt("EmergencyFundBalance", 0);
        PlayerPrefs.Save();
    }

    // ==================== RESTART ====================

    public void RestartGame()
    {
        ResetState();
        RefreshHUD();
        DuckSay(DuckReaction.Emotion.Neutral, "Let's try again!");
        Invoke("StartNewWeek", TransitionDelay);
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

        string hint;
        switch (currentWeek)
        {
            case 1: hint = "Saving more now means safety later!"; break;
            case 2: hint = "Saving more now means safety later!"; break;
            case 3: hint = "This is why we save!"; break;
            case 4: hint = "A bonus is a chance to build your fund."; break;
            case 5: hint = "Big costs show why funds matter."; break;
            case 6: hint = "Tough week \u2014 choose carefully!"; break;
            default: hint = "Think about what matters most."; break;
        }
        DuckSay(DuckReaction.Emotion.Thinking, hint);
    }

    void OnKeepGoing()
    {
        DismissReengagementPopup();
    }
}
