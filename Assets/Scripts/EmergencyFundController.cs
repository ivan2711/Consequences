using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FinancialLiteracy.UI;

public class EmergencyFundController : MonoBehaviour
{
    [Header("UI References")]
    public Image progressBarFill;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI eventText;
    public TextMeshProUGUI roundCounterText;
    public Transform choicesPanel;
    public EmergencyFundConsequencePanel consequencePanel;
    public StarRating starRating;
    public DuckReaction duckReaction;
    public DuckReactionBackgroundChanger backgroundChanger;

    [Header("Season Settings")]
    public int totalWeeks = 6;
    public int weeklyAvailable = 60;  // £100 income - £40 essentials
    public int goalAmount = 120;
    public int progressBarMax = 120;

    [Header("Saving Tiers")]
    public int tierSmall = 20;
    public int tierBalanced = 30;
    public int tierStrong = 40;

    [Header("Game State")]
    public int currentWeek = 0;
    public int emergencyFundBalance = 0;

    // --- Season tracking ---
    private bool _wentIntoDebt = false;
    private int _totalOptionalSpend = 0;
    private int _totalSavedToFund = 0;
    private int _weeklyLeftover = 0; // Available remaining after saving tier

    // Week 6 crisis sub-phase (0 = travel, 1 = social)
    private int _crisisPhase = 0;

    // --- Inactivity tracking ---
    private float _idleTimer = 0f;
    private float _idleCooldown = 0f;
    private const float IdleThresholdSeconds = 60f;

    // --- Re-engagement popup ---
    private GameObject _reengagePanel;
    private TextMeshProUGUI _reengageText;
    private float _reengageCooldown = 0f;

    private const float ReengageCooldownNormal = 60f;
    private const float ReengageCooldownCalm = 120f;
    private const float ReengageIdleThreshold = 20f;

    // ==================== LIFECYCLE ====================

    void Start()
    {
        currentWeek = 0;
        emergencyFundBalance = 0;
        _wentIntoDebt = false;
        _totalOptionalSpend = 0;
        _totalSavedToFund = 0;
        _weeklyLeftover = 0;
        _crisisPhase = 0;

        PlayerPrefs.SetInt("EmergencyFundBalance", 0);
        PlayerPrefs.Save();

        BuildReengagementPopup();
        UpdateUI();
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

    public void StartGameAfterTutorial()
    {
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Let's build an emergency fund!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Let's build an emergency fund!");
        }

        Invoke("StartNewWeek", 1.5f);
    }

    // ==================== WEEKLY FLOW ====================

    void StartNewWeek()
    {
        currentWeek++;

        if (currentWeek > totalWeeks)
        {
            ShowReflection();
            return;
        }

        if (roundCounterText != null)
            roundCounterText.text = "Week " + currentWeek + " of " + totalWeeks;

        // Silent bank accounting (no visible animation)
        BankAccountService.Instance.Earn(100, "Week " + currentWeek + " income");
        BankAccountService.Instance.Spend(40, "Week " + currentWeek + " essentials", "Needs");

        UpdateUI();
        ShowSavingTierPrompt();
    }

    // ==================== SAVING TIER (shown every week) ====================

    void ShowSavingTierPrompt()
    {
        eventText.text = "WEEK " + currentWeek + "\n\n"
            + "Available this week: \u00a3" + weeklyAvailable + "\n"
            + "Emergency fund: \u00a3" + emergencyFundBalance + "\n\n"
            + "How much will you save?";

        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Choose wisely!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Choose wisely!");
        }

        ClearChoices();
        CreateActionButton("Strong \u00a3" + tierStrong, new Color(0.2f, 0.9f, 0.3f), () => ProcessTier(tierStrong));
        CreateActionButton("Balanced \u00a3" + tierBalanced, new Color(0.5f, 0.7f, 0.5f), () => ProcessTier(tierBalanced));
        CreateActionButton("Small \u00a3" + tierSmall, new Color(0.6f, 0.7f, 0.9f), () => ProcessTier(tierSmall));
    }

    void ProcessTier(int tier)
    {
        _idleTimer = 0f;

        // Deduct tier from available, add to fund
        emergencyFundBalance += tier;
        _totalSavedToFund += tier;
        _weeklyLeftover = weeklyAvailable - tier;

        // Move leftover to bank as disposable income
        if (_weeklyLeftover > 0)
            BankAccountService.Instance.Earn(_weeklyLeftover, "Week " + currentWeek + " leftover");

        // Transfer tier conceptually (already in fund, deduct from bank's net)
        BankAccountService.Instance.Spend(tier, "Week " + currentWeek + " emergency fund saving", "Emergency");

        SaveEmergencyFund();

        // Log tier choice to PlayerModelService
        if (PlayerModelService.Instance != null)
        {
            Debug.Log("[EF] Week " + currentWeek + " tier: \u00a3" + tier);
            PlayerModelService.Instance.RecordEmergencyFundRound(tier, tierStrong);
        }

        // Duck feedback on tier
        if (duckReaction != null)
        {
            if (tier >= tierStrong)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Strong saver!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Strong saver!");
            }
            else if (tier >= tierBalanced)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Balanced choice!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Balanced choice!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Every bit counts.");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Every bit counts.");
            }
        }

        UpdateUI();

        // Check if this week has a follow-up event
        switch (currentWeek)
        {
            case 3: Invoke("ShowWeek3Event", 1.5f); break;
            case 4: Invoke("ShowWeek4Event", 1.5f); break;
            case 5: Invoke("ShowWeek5Event", 1.5f); break;
            case 6: _crisisPhase = 0; Invoke("ShowWeek6Event", 1.5f); break;
            default: ShowWeekFeedback("You saved \u00a3" + tier + " this week."); break;
        }
    }

    // ==================== WEEK 3: Minor emergency (£30) ====================

    void ShowWeek3Event()
    {
        eventText.text = "EMERGENCY!\n\n"
            + "Your phone screen cracked.\nRepair costs \u00a330.\n\n"
            + "Emergency fund: \u00a3" + emergencyFundBalance;

        if (duckReaction != null)
        {
            if (emergencyFundBalance >= 30)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Use your fund!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Use your fund!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Shocked, "Unexpected cost!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Unexpected cost!");
            }
        }

        ClearChoices();
        CreateActionButton("Pay \u00a330 repair", new Color(0.8f, 0.6f, 0.3f), () => HandleEmergency(30, "Phone screen repair"));
    }

    // ==================== WEEK 4: Bonus +£40 ====================

    void ShowWeek4Event()
    {
        BankAccountService.Instance.Earn(40, "Week 4 bonus");

        eventText.text = "BONUS!\n\n"
            + "You earned an extra \u00a340 this week.\n\n"
            + "Emergency fund: \u00a3" + emergencyFundBalance + "\n\n"
            + "What will you do with it?";

        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Excited, "Bonus week!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Bonus week!");
        }

        ClearChoices();
        CreateActionButton("Add \u00a340 to fund", new Color(0.2f, 0.9f, 0.3f), () =>
        {
            _idleTimer = 0f;
            BankAccountService.Instance.Spend(40, "Bonus to emergency fund", "Emergency");
            emergencyFundBalance += 40;
            _totalSavedToFund += 40;
            SaveEmergencyFund();

            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Fund boosted!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Fund boosted!");
            }

            UpdateUI();
            ShowWeekFeedback("You added the \u00a340 bonus to your fund.");
        });
        CreateActionButton("Keep in bank", new Color(0.6f, 0.7f, 0.9f), () =>
        {
            _idleTimer = 0f;
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Kept the bonus.");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Kept the bonus.");
            }

            UpdateUI();
            ShowWeekFeedback("You kept the \u00a340 bonus in your bank.");
        });
    }

    // ==================== WEEK 5: Larger emergency (£60) ====================

    void ShowWeek5Event()
    {
        eventText.text = "BIG EMERGENCY!\n\n"
            + "Your laptop broke.\nRepair costs \u00a360.\n\n"
            + "Emergency fund: \u00a3" + emergencyFundBalance;

        if (duckReaction != null)
        {
            if (emergencyFundBalance >= 60)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Your fund can cover this!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Your fund can cover this!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Worried, "This is a big one...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("This is a big one...");
            }
        }

        ClearChoices();
        CreateActionButton("Pay \u00a360 repair", new Color(0.9f, 0.5f, 0.3f), () => HandleEmergency(60, "Laptop repair"));
    }

    // ==================== WEEK 6: Crisis — £20 travel + £20 social ====================

    void ShowWeek6Event()
    {
        if (_crisisPhase == 0)
        {
            eventText.text = "CRISIS WEEK!\n\n"
                + "Family event coming up.\nTravel costs \u00a320.\n\n"
                + "Emergency fund: \u00a3" + emergencyFundBalance;

            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Worried, "Tough week ahead...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Tough week ahead...");
            }

            ClearChoices();
            CreateActionButton("Pay \u00a320 travel", new Color(0.8f, 0.6f, 0.3f), () => HandleCrisisTravel(true));
            CreateActionButton("Skip the trip", new Color(0.3f, 0.8f, 0.3f), () => HandleCrisisTravel(false));
        }
        else
        {
            eventText.text = "CRISIS CONTINUES\n\n"
                + "Friends invite you out.\nCost: \u00a320.\n\n"
                + "Emergency fund: \u00a3" + emergencyFundBalance;

            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "One more decision...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("One more decision...");
            }

            ClearChoices();
            CreateActionButton("Go out (\u00a320)", new Color(0.6f, 0.7f, 0.9f), () => HandleCrisisSocial(true));
            CreateActionButton("Stay home (free)", new Color(0.3f, 0.8f, 0.3f), () => HandleCrisisSocial(false));
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
            result += "Paid \u00a3" + coveredByFund + " from emergency fund.\n";

        if (remaining > 0)
        {
            float bank = BankAccountService.Instance.GetBalance();
            if (bank >= remaining)
            {
                BankAccountService.Instance.Spend(remaining, description, "Emergency");
                result += "Paid \u00a3" + remaining + " from bank.\n";
            }
            else
            {
                int fromBank = (int)bank;
                if (fromBank > 0)
                    BankAccountService.Instance.Spend(fromBank, description, "Emergency");
                int deficit = remaining - fromBank;
                _wentIntoDebt = true;
                if (fromBank > 0)
                    result += "Paid \u00a3" + fromBank + " from bank.\n";
                result += "Couldn't cover \u00a3" + deficit + " — you went into debt!\n";
            }
        }

        SaveEmergencyFund();

        if (duckReaction != null)
        {
            if (coveredByFund >= cost)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Your fund saved you!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Your fund saved you!");
            }
            else if (!_wentIntoDebt)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Worried, "That was close...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("That was close...");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Sad, "You're in debt...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("You're in debt...");
            }
        }

        UpdateUI();
        ShowWeekFeedback(result);
    }

    // ==================== CRISIS HANDLERS ====================

    void HandleCrisisTravel(bool pay)
    {
        _idleTimer = 0f;

        if (pay)
        {
            float bank = BankAccountService.Instance.GetBalance();
            if (bank >= 20)
            {
                BankAccountService.Instance.Spend(20, "Family event travel", "Needs");
                _totalOptionalSpend += 20;
                if (duckReaction != null)
                {
                    duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Family comes first.");
                    if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Family comes first.");
                }
            }
            else
            {
                int canPay = (int)bank;
                if (canPay > 0)
                    BankAccountService.Instance.Spend(canPay, "Family event travel", "Needs");
                _totalOptionalSpend += 20;
                _wentIntoDebt = true;
                if (duckReaction != null)
                {
                    duckReaction.ShowReaction(DuckReaction.Emotion.Sad, "Tight budget...");
                    if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Tight budget...");
                }
            }
        }
        else
        {
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Sad, "Missed the family event...");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Missed the family event...");
            }
        }

        _crisisPhase = 1;
        UpdateUI();
        Invoke("ShowWeek6Event", 1.5f);
    }

    void HandleCrisisSocial(bool go)
    {
        _idleTimer = 0f;

        if (go)
        {
            float bank = BankAccountService.Instance.GetBalance();
            if (bank >= 20)
            {
                BankAccountService.Instance.Spend(20, "Going out with friends", "Social");
                _totalOptionalSpend += 20;
                if (duckReaction != null)
                {
                    duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Great time out!");
                    if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Great time out!");
                }
            }
            else
            {
                int canPay = (int)bank;
                if (canPay > 0)
                    BankAccountService.Instance.Spend(canPay, "Going out with friends", "Social");
                _totalOptionalSpend += 20;
                _wentIntoDebt = true;
                if (duckReaction != null)
                {
                    duckReaction.ShowReaction(DuckReaction.Emotion.Sad, "Couldn't quite afford it...");
                    if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Couldn't quite afford it...");
                }
            }
        }
        else
        {
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Saved \u00a320.");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Saved \u00a320.");
            }
        }

        UpdateUI();
        ShowWeekFeedback("End of the season!");
    }

    // ==================== FEEDBACK & CONSEQUENCE ====================

    void ShowWeekFeedback(string message)
    {
        string summary = message + "\n\nEmergency fund: \u00a3" + emergencyFundBalance;
        if (_wentIntoDebt)
            summary += "\n(in debt)";

        eventText.text = summary;
        ClearChoices();

        if (currentWeek >= totalWeeks)
        {
            CreateActionButton("See Your Results", new Color(0.9f, 0.7f, 0.2f), () =>
            {
                ClearChoices();
                ShowReflection();
            });
        }
        else
        {
            CreateActionButton("Continue", new Color(0.5f, 0.7f, 1f), () =>
            {
                ClearChoices();
                StartNewWeek();
            });
        }
    }

    // ==================== END-OF-SEASON REFLECTION ====================

    void ShowReflection()
    {
        Debug.Log("Season ended! Fund: \u00a3" + emergencyFundBalance + " Debt: " + _wentIntoDebt);

        if (PlayerModelService.Instance != null)
            PlayerModelService.Instance.RecordEmergencyFundRound(emergencyFundBalance, goalAmount);

        int stars = CalculateStars();

        string reflection = "SEASON COMPLETE!\n\n"
            + "Emergency fund: \u00a3" + emergencyFundBalance + "\n"
            + "Total saved to fund: \u00a3" + _totalSavedToFund + "\n"
            + "Optional spending: \u00a3" + _totalOptionalSpend + "\n\n";

        if (_wentIntoDebt)
            reflection += "You went into debt during the season.\nA bigger emergency fund would have helped!\n";
        else if (emergencyFundBalance > 0)
            reflection += "You stayed out of debt and still have savings!\nGreat money management.\n";
        else
            reflection += "You avoided debt, but your fund is empty.\nTry saving more next time!\n";

        eventText.text = reflection;

        DuckReaction.Emotion duckEmotion;
        string duckLine;
        if (stars >= 3)
        {
            duckEmotion = DuckReaction.Emotion.Celebrating;
            duckLine = "Amazing season!";
        }
        else if (stars >= 2)
        {
            duckEmotion = DuckReaction.Emotion.Happy;
            duckLine = "Well done!";
        }
        else if (stars >= 1)
        {
            duckEmotion = DuckReaction.Emotion.Neutral;
            duckLine = "Room to improve!";
        }
        else
        {
            duckEmotion = DuckReaction.Emotion.Worried;
            duckLine = "Try again!";
        }

        if (duckReaction != null)
        {
            duckReaction.ShowReaction(duckEmotion, duckLine);
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(duckLine);
        }

        if (starRating != null)
            starRating.SetRating(stars);

        ClearChoices();
        CreateActionButton("See Full Results", new Color(0.9f, 0.7f, 0.2f), () =>
        {
            ClearChoices();
            ShowConsequencePanel();
        });
    }

    int CalculateStars()
    {
        if (!_wentIntoDebt && emergencyFundBalance > 0) return 3;
        if (!_wentIntoDebt) return 2;
        if (_totalSavedToFund > 0) return 1;
        return 0;
    }

    void ShowConsequencePanel()
    {
        int stars = CalculateStars();

        if (consequencePanel != null)
        {
            consequencePanel.ShowConsequences(emergencyFundBalance, stars);
        }
        else
        {
            Debug.LogError("[X] Consequence panel is NULL! Not wired!");
        }
    }

    // ==================== UI HELPERS ====================

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = "\u00a3" + emergencyFundBalance + " / \u00a3" + goalAmount;

        float progress = progressBarMax > 0 ? (float)emergencyFundBalance / progressBarMax : 0f;
        progress = Mathf.Clamp01(progress);

        if (progressBarFill != null)
        {
            RectTransform rect = progressBarFill.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(650 * progress, 40);
        }
    }

    void SaveEmergencyFund()
    {
        PlayerPrefs.SetInt("EmergencyFundBalance", emergencyFundBalance);
        PlayerPrefs.Save();
    }

    void ClearChoices()
    {
        foreach (Transform child in choicesPanel)
            Destroy(child.gameObject);
    }

    void CreateActionButton(string text, Color buttonColor, System.Action callback)
    {
        GameObject button = new GameObject("ChoiceButton");
        button.transform.SetParent(choicesPanel, false);

        RectTransform rect = button.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 120);

        Image img = button.AddComponent<Image>();
        img.color = buttonColor;

        Button btn = button.AddComponent<Button>();

        GameObject btnText = new GameObject("Text");
        btnText.transform.SetParent(button.transform, false);
        RectTransform textRect = btnText.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -20);

        TextMeshProUGUI tmp = btnText.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        btn.onClick.AddListener(() => callback());
    }

    // ==================== RESTART ====================

    public void RestartGame()
    {
        currentWeek = 0;
        emergencyFundBalance = 0;
        _wentIntoDebt = false;
        _totalOptionalSpend = 0;
        _totalSavedToFund = 0;
        _weeklyLeftover = 0;
        _crisisPhase = 0;

        PlayerPrefs.SetInt("EmergencyFundBalance", 0);
        PlayerPrefs.Save();

        UpdateUI();
        ClearChoices();

        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Let's try again!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Let's try again!");
        }

        Invoke("StartNewWeek", 1.5f);
    }

    // ==================== RE-ENGAGEMENT POPUP ====================

    void BuildReengagementPopup()
    {
        GameObject canvasGO = new GameObject("ReengageCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

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
        if (_reengagePanel == null) return;
        if (_reengagePanel.activeSelf) return;
        if (_reengageCooldown > 0f) return;
        if (currentWeek <= 0 || currentWeek > totalWeeks) return;

        bool idleTrigger = _idleTimer >= ReengageIdleThreshold;
        bool streakTrigger = PlayerModelService.Instance != null
            && PlayerModelService.Instance.failedRoundsStreak >= 2;

        if (idleTrigger || streakTrigger)
        {
            ShowReengagementPopup(idleTrigger
                ? "Take your time.\nThere's no wrong answer!"
                : "Tough stretch!\nSaving is hard — keep going!");
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

        if (duckReaction != null)
        {
            string hint;
            switch (currentWeek)
            {
                case 1: hint = "Saving more now means safety later!"; break;
                case 2: hint = "Saving more now means safety later!"; break;
                case 3: hint = "This is why we save!"; break;
                case 4: hint = "A bonus is a chance to build your fund."; break;
                case 5: hint = "Big costs show why funds matter."; break;
                case 6: hint = "Tough week — choose carefully!"; break;
                default: hint = "Think about what matters most."; break;
            }
            duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, hint);
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(hint);
        }
    }

    void OnKeepGoing()
    {
        DismissReengagementPopup();
    }
}
