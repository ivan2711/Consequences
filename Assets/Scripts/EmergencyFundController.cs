using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    
    [Header("Game Settings")]
    public int maxWeeks = 9;
    public int goalAmount = 600;
    public int progressBarMax =6000; // Progress bar max
    public int weeklyLeftover = 120; // £200 income - £80 costs
    
    [Header("Game State")]
    public int emergencyFund = 0;
    public int currentWeek = 0;
    
    // Week types: normal (gets £120), event (emergency/bonus/etc)
        // --- Inactivity tracking ---
    private float _idleTimer = 0f;
    private float _idleCooldown = 0f;
    private const float IdleThresholdSeconds = 60f;

    // --- Re-engagement popup ---
    private GameObject _reengagePanel;
    private TextMeshProUGUI _reengageText;
    private float _reengageCooldown = 0f;
    private bool _forceEasierNext = false;
    private string _lastWeekType = "";
    private string _lastEventId = "";

    private const float ReengageCooldownNormal = 60f;
    private const float ReengageCooldownCalm = 120f;
    private const float ReengageIdleThreshold = 20f;


    
private string[] weekSequence = {
        "normal",      // Week 1: Regular payday
        "choice",      // Week 2: Choice event (cinema, etc)
        "bonus",       // Week 3: Extra shifts (+£100 bonus)
        "emergency",   // Week 4: Emergency (phone broke)
        "choice",      // Week 5: Choice event
        "normal",      // Week 6: Regular payday
        "crisis",      // Week 7: Crisis (two emergencies)
        "lucky",       // Week 8: Lucky (birthday money)
        "normal"       // Week 9: Regular payday
    };
    
void Start()
    {
        currentWeek = 0;
        emergencyFund = 0;
                BuildReengagementPopup();
        
UpdateUI();
        
        // FOR TESTING: Always wait for tutorial
        // Tutorial will call StartGameAfterTutorial() when done
        
        /* ORIGINAL CODE (re-enable for final build):
        bool tutorialShown = PlayerPrefs.GetInt("EmergencyFundTutorialShown", 0) == 1;
        
        if (tutorialShown)
        {
            StartGameAfterTutorial();
        }
        */
    }

    void Update()
    {
        // Detect any user interaction
        bool hasInput = Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (hasInput)
        {
            _idleTimer = 0f;
            return;
        }

        _idleTimer += Time.deltaTime;

        // Tick down calm-mode cooldown
        if (_idleCooldown > 0f)
            _idleCooldown -= Time.deltaTime;

                // Re-engagement cooldown tick & trigger check
        if (_reengageCooldown > 0f)
            _reengageCooldown -= Time.deltaTime;
        CheckReengagementTrigger();

        
if (_idleTimer >= IdleThresholdSeconds)
        {
            // In calm mode, enforce a 60s cooldown between recordings
            if (GameSettings.CalmMode && _idleCooldown > 0f)
                return;

            if (PlayerModelService.Instance != null)
            {
                PlayerModelService.Instance.RecordInactivity();
            }

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
    
void StartNewWeek()
    {
        currentWeek++;
        
        if (roundCounterText != null)
        {
            roundCounterText.text = "Week " + currentWeek + " of " + maxWeeks;
        }
        
        if (currentWeek > maxWeeks)
        {
            EndGame();
            return;
        }
        
        string weekType = weekSequence[currentWeek - 1];
        
        // --- Re-engagement popup override ---
        if (_forceEasierNext)
        {
            Debug.Log($"[Reengage] Force easier: {weekType} -> bonus (week {currentWeek})");
            weekType = "bonus";
            _forceEasierNext = false;
        }

        // --- Engagement-based override ---
        if (PlayerModelService.Instance != null)
        {
            var state = PlayerModelService.Instance.GetEngagementState();
            if (state == PlayerModelService.EngagementState.Frustrated)
            {
                Debug.Log($"[Adaptive] Frustrated override: {weekType} -> bonus (week {currentWeek})");
                weekType = "bonus";
            }
            else if (state == PlayerModelService.EngagementState.Bored)
            {
                Debug.Log($"[Adaptive] Bored override: {weekType} -> choice (week {currentWeek})");
                weekType = "choice";
            }
        }

        // --- No consecutive duplicate types ---
        if (weekType == _lastWeekType)
        {
            string[] alternatives = { "normal", "choice", "bonus", "lucky" };
            foreach (string alt in alternatives)
            {
                if (alt != _lastWeekType)
                {
                    Debug.Log($"[Variety] Avoiding repeat: {weekType} -> {alt} (week {currentWeek})");
                    weekType = alt;
                    break;
                }
            }
        }
        _lastWeekType = weekType;
        
        // --- Data-driven event loading ---
        if (EventLoader.Instance != null && EventLoader.Instance.TotalEventsLoaded > 0)
        {
            EmergencyFundEvent evt = EventLoader.Instance.GetEvent(weekType, _lastEventId);
            if (evt != null)
            {
                _lastEventId = evt.id;
                ShowEventFromData(evt);
                return;
            }
            Debug.LogWarning($"[EF] No data for type '{weekType}', falling back to hardcoded");
        }

        // Fallback to hardcoded if no JSON data loaded
        switch (weekType)
        {
            case "normal": ShowNormalWeek(); break;
            case "choice": ShowChoiceWeek(); break;
            case "bonus": ShowBonusWeek(); break;
            case "emergency": ShowEmergencyWeek(); break;
            case "crisis": ShowCrisisWeek(); break;
            case "lucky": ShowLuckyWeek(); break;
        }
    }
    
void ShowNormalWeek()
    {
        eventText.text = "PAYDAY!\n\nYou have £120\n\nHow much will you save?";
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Pay day! £");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Pay day! £");
        }
        
        ClearChoices();
        CreateChoiceButton("£ Save all £120", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
        CreateChoiceButton("£ Save half £60", weeklyLeftover / 2, new Color(0.5f, 0.7f, 0.5f));
        CreateChoiceButton("£ Spend all", 0, new Color(0.9f, 0.5f, 0.3f));
    }
    
    void ShowChoiceWeek()
    {
        int choice = Random.Range(0, 5);
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Decide wisely...");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Decide wisely...");
        }
        
        ClearChoices();
        
        switch(choice)
        {
            case 0:
                eventText.text = "WEEKEND!\n\nCinema + meal: £30\n\nYou have £120";
                CreateChoiceButton("[OK] Go\n(Save £90)", 90, new Color(0.6f, 0.7f, 0.9f));
                CreateChoiceButton("£ Stay home\n(Save £120)", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
                break;
            case 1:
                eventText.text = "GAME SALE!\n\nNew game on sale: £25\n\nYou have £120";
                CreateChoiceButton("£ Buy game\n(Save £95)", 95, new Color(0.6f, 0.7f, 0.9f));
                CreateChoiceButton("£ Skip it\n(Save £120)", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
                break;
            case 2:
                eventText.text = "SHOPPING!\n\nNew trainers you want: £40\n\nYou have £120";
                CreateChoiceButton("£ Buy trainers\n(Save £80)", 80, new Color(0.6f, 0.7f, 0.9f));
                CreateChoiceButton("£ Wait\n(Save £120)", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
                break;
            case 3:
                eventText.text = "HUNGRY!\n\nTakeaway or cook at home?\nTakeaway: £20\n\nYou have £120";
                CreateChoiceButton("Takeaway\n(Save £100)", 100, new Color(0.6f, 0.7f, 0.9f));
                CreateChoiceButton("Cook home\n(Save £120)", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
                break;
            case 4:
                eventText.text = "BIRTHDAY!\n\nFriend's birthday gift: £30\n\nYou have £120";
                CreateChoiceButton("Buy gift\n(Save £90)", 90, new Color(0.6f, 0.7f, 0.9f));
                CreateChoiceButton("Make gift\n(Save £120)", weeklyLeftover, new Color(0.3f, 0.8f, 0.3f));
                break;
        }
    }
    
    void ShowBonusWeek()
    {
        int bonusAmount = 100;
        int total = weeklyLeftover + bonusAmount;
        
        eventText.text = "BONUS!\n\nExtra shifts: +£100\n\nYou have £220";
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Excited, "Bonus week!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Bonus week!");
        }
        
        ClearChoices();
        CreateChoiceButton("£ Save all £220", total, new Color(0.2f, 0.9f, 0.3f));
        CreateChoiceButton("£ Save half £110", total / 2, new Color(0.5f, 0.7f, 0.5f));
        CreateChoiceButton("£ Spend all", 0, new Color(0.9f, 0.6f, 0.3f));
    }
    
    void ShowEmergencyWeek()
    {
        int emergencyCost = 80;
        int remaining = weeklyLeftover - emergencyCost;
        
        eventText.text = "EMERGENCY!\n\nPhone broke: -£80\n\nLeftover: £40";
        
        if (duckReaction != null)
        {
            if (emergencyFund >= emergencyCost)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Use your fund!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Use your fund!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Shocked, "Unexpected!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Unexpected!");
            }
        }
        
        ClearChoices();
        CreateChoiceButton("Paid £80\n(Save £40)", remaining, new Color(0.8f, 0.6f, 0.3f));
    }
    
    void ShowCrisisWeek()
    {
        int crisisCost = 90;
        int remaining = weeklyLeftover - crisisCost;
        
        eventText.text = "CRISIS!\n\nTextbook + Bus pass: -£90\n\nLeftover: £30";
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Worried, "Two things!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Two things!");
        }
        
        ClearChoices();
        CreateChoiceButton("Paid £90\n(Save £30)", remaining, new Color(0.9f, 0.5f, 0.3f));
    }
    
    void ShowLuckyWeek()
    {
        int luckyBonus = 60;
        int total = weeklyLeftover + luckyBonus;
        
        eventText.text = "LUCKY!\n\nBirthday money: +£60\n\nYou have £180";
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Lucky you!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Lucky you!");
        }
        
        ClearChoices();
        CreateChoiceButton("£ Save all £180", total, new Color(0.2f, 0.9f, 0.3f));
        CreateChoiceButton("£ Save half £90", total / 2, new Color(0.5f, 0.7f, 0.5f));
        CreateChoiceButton("£ Spend all", 0, new Color(0.9f, 0.6f, 0.3f));
    }

/// <summary>
    /// Data-driven event display. Reads title, description, choices, duck reactions
    /// all from the EmergencyFundEvent JSON data instead of hardcoded values.
    /// </summary>
    void ShowEventFromData(EmergencyFundEvent evt)
    {
        Debug.Log($"[EF] Showing data event: {evt.id} ({evt.type}) - {evt.title}");

        // Build display text
        int totalIncome = evt.weeklyIncomePounds + evt.bonusPounds;
        string displayText = evt.title.ToUpper() + "\n\n" + evt.description;

        if (evt.costPounds > 0)
            displayText += "\n\nCost: \u00a3" + evt.costPounds;
        if (evt.bonusPounds > 0)
            displayText += "\n\nBonus: +\u00a3" + evt.bonusPounds;

        displayText += "\n\nYou have: \u00a3" + totalIncome;
        eventText.text = displayText;

        // Duck reaction from data
        if (duckReaction != null)
        {
            DuckReaction.Emotion emotion = MapDuckEmotion(evt.duckEmotion);
            string duckLine = string.IsNullOrEmpty(evt.duckLine) ? evt.title : evt.duckLine;
            duckReaction.ShowReaction(emotion, duckLine);
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(duckLine);
        }

        // Build choice buttons from data
        ClearChoices();
        for (int i = 0; i < evt.choices.Length; i++)
        {
            EventChoice choice = evt.choices[i];
            Color btnColor = GetChoiceColor(choice.savePounds, totalIncome, i, evt.choices.Length);
            string btnLabel = choice.label + "\n(Save \u00a3" + choice.savePounds + ")";
            CreateChoiceButton(btnLabel, choice.savePounds, btnColor);
        }
    }

    DuckReaction.Emotion MapDuckEmotion(string emotionStr)
    {
        switch (emotionStr)
        {
            case "happy": return DuckReaction.Emotion.Happy;
            case "sad": return DuckReaction.Emotion.Sad;
            case "excited": return DuckReaction.Emotion.Excited;
            case "shocked": return DuckReaction.Emotion.Shocked;
            case "worried": return DuckReaction.Emotion.Worried;
            case "thinking": return DuckReaction.Emotion.Thinking;
            case "celebrating": return DuckReaction.Emotion.Celebrating;
            default: return DuckReaction.Emotion.Neutral;
        }
    }

    Color GetChoiceColor(int savePounds, int totalIncome, int index, int totalChoices)
    {
        // High save = green, medium = blue, low/zero = orange
        float saveRatio = totalIncome > 0 ? (float)savePounds / totalIncome : 0f;
        if (saveRatio >= 0.9f) return new Color(0.2f, 0.9f, 0.3f);   // Green - save most/all
        if (saveRatio >= 0.5f) return new Color(0.5f, 0.7f, 0.5f);   // Muted green - save half+
        if (savePounds > 0)    return new Color(0.6f, 0.7f, 0.9f);   // Blue - some saving
        return new Color(0.9f, 0.5f, 0.3f);                          // Orange - spend all
    }

    
    void ClearChoices()
    {
        foreach (Transform child in choicesPanel)
        {
            Destroy(child.gameObject);
        }
    }
    
    void CreateChoiceButton(string text, int saveAmount, Color buttonColor)
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
        
        int saveAmountCopy = saveAmount;
        btn.onClick.AddListener(() => MakeChoice(saveAmountCopy));
    }
    
void MakeChoice(int saveAmount)
    {
                _idleTimer = 0f; // Reset inactivity on choice
int previousFund = emergencyFund;
        emergencyFund += saveAmount;
        
        // Show floating money text
        FloatingMoneyText floatingMoney = FindObjectOfType<FloatingMoneyText>();
        if (floatingMoney != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2 + 100, 0);
            floatingMoney.ShowMoneyChange(saveAmount, screenCenter);
        }
        
        if (duckReaction != null)
        {
            if (saveAmount >= 100)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Great save! £");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Great save! £");
            }
            else if (saveAmount >= 50)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Some saved!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Some saved!");
            }
            else if (saveAmount > 0)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Worried, "Save more!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Save more!");
            }
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Sad, "Nothing saved!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Nothing saved!");
            }
        }
        
        UpdateUI();
        
        // Milestone celebrations
        if (emergencyFund >= 150 && previousFund < 150)
        {
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "More than £150 saved!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("More than £150 saved!");
            }
        }
        else if (emergencyFund >= 300 && previousFund < 300)
        {
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "Halfway!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Halfway!");
            }
        }
        else if (emergencyFund >= 450 && previousFund < 450)
        {
            if (duckReaction != null)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Excited, "Almost there!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Almost there!");
            }
        }
        
        Invoke("StartNewWeek", 1.5f);
    }
    
void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = "£" + emergencyFund + " / £" + goalAmount;
        }
        
        // Progress bar fills to progressBarMax (600) which matches goalAmount (600)nt (600)
        float progress = (float)emergencyFund / progressBarMax;
        progress = Mathf.Clamp01(progress);
        
        if (progressBarFill != null)
        {
            RectTransform rect = progressBarFill.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(650 * progress, 40);
        }
    }
    
void EndGame()
    {
        Debug.Log("🎮 GAME ENDED! Final amount: £" + emergencyFund);
        
        
        // Record round in player model
        if (PlayerModelService.Instance != null)
        {
            PlayerModelService.Instance.RecordEmergencyFundRound(emergencyFund, goalAmount);
        }
int stars = CalculateStars();
        
        string resultMessage = "";
        DuckReaction.Emotion duckEmotion = DuckReaction.Emotion.Neutral;
        
        if (emergencyFund >= goalAmount)
        {
            resultMessage = "AMAZING!\n\nYou saved: £" + emergencyFund + "\n\nGoal reached!";
            duckEmotion = DuckReaction.Emotion.Celebrating;
        }
        else if (emergencyFund >= 400)
        {
            resultMessage = "GREAT!\n\nYou saved: £" + emergencyFund + "\n\nAlmost there!";
            duckEmotion = DuckReaction.Emotion.Happy;
        }
        else if (emergencyFund >= 250)
        {
            resultMessage = "GOOD!\n\nYou saved: £" + emergencyFund + "\n\nKeep building!";
            duckEmotion = DuckReaction.Emotion.Happy;
        }
        else
        {
            resultMessage = "TRY AGAIN!\n\nYou saved: £" + emergencyFund + "\n\nSave more next time!";
            duckEmotion = DuckReaction.Emotion.Neutral;
        }
        
        eventText.text = resultMessage;
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(duckEmotion, "Game over!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Game over!");
        }
        
        if (starRating != null)
        {
            starRating.SetRating(stars);
        }
        
        //ShowConsequencePanel();
        ClearChoices();
        CreateSeeResultsButton();
        
        Debug.Log("⏰ Scheduling consequence panel in 2 seconds...");
        if (consequencePanel != null)
        {
            Invoke("ShowConsequencePanel", 2f);
        }
        else
        {
            Debug.LogError("[X] Consequence panel is NULL in EndGame! Cannot show!");
        }
    }

    void CreatePlayAgainButton()
{
    GameObject button = new GameObject("PlayAgainButton");
    button.transform.SetParent(choicesPanel, false);
    
    RectTransform rect = button.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(280, 120);
    
    Image img = button.AddComponent<Image>();
    img.color = new Color(0.5f, 0.7f, 1f);
    
    Button btn = button.AddComponent<Button>();
    
    GameObject btnText = new GameObject("Text");
    btnText.transform.SetParent(button.transform, false);
    RectTransform textRect = btnText.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = new Vector2(-20, -20);
    
    TextMeshProUGUI tmp = btnText.AddComponent<TextMeshProUGUI>();
    tmp.text = "Play Again";
    tmp.fontSize = 24;
    tmp.color = Color.white;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.fontStyle = FontStyles.Bold;
    
    btn.onClick.AddListener(() => RestartGame());
}

void CreateSeeResultsButton()
{
    GameObject button = new GameObject("SeeResultsButton");
    button.transform.SetParent(choicesPanel, false);

    RectTransform rect = button.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(280, 120);

    Image img = button.AddComponent<Image>();
    img.color = new Color(0.9f, 0.7f, 0.2f);

    Button btn = button.AddComponent<Button>();

    GameObject btnText = new GameObject("Text");
    btnText.transform.SetParent(button.transform, false);
    RectTransform textRect = btnText.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = new Vector2(-20, -20);

    TextMeshProUGUI tmp = btnText.AddComponent<TextMeshProUGUI>();
    tmp.text = "See Your Results";
    tmp.fontSize = 24;
    tmp.color = Color.white;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.fontStyle = FontStyles.Bold;

    btn.onClick.AddListener(() =>
    {
        ClearChoices();
        ShowConsequencePanel();
    });
}
    
    int CalculateStars()
    {
        if (emergencyFund >= 600) return 3;
        if (emergencyFund >= 400) return 2;
        if (emergencyFund >= 250) return 1;
        return 0;
    }
    
void ShowConsequencePanel()
    {
        Debug.Log(">>> Showing consequence panel...");
        
        int stars = CalculateStars();
        
        if (consequencePanel != null)
        {
            Debug.Log($"Consequence panel found! Stars: {stars}, Amount: {emergencyFund}");
            consequencePanel.ShowConsequences(emergencyFund, stars);
        }
        else
        {
            Debug.LogError("[X] Consequence panel is NULL! Not wired!");
        }
    }
    
    void BuildReengagementPopup()
    {
        // Create dedicated canvas at scene root (avoid parent transform issues)
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

        // Message text
        GameObject textGO = new GameObject("ReengageText");
        textGO.transform.SetParent(_reengagePanel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.55f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _reengageText = textGO.AddComponent<TextMeshProUGUI>();
        _reengageText.text = "Need a hand?\nWe can make the next week easier!";
        _reengageText.fontSize = 26f;
        _reengageText.color = Color.white;
        _reengageText.alignment = TextAlignmentOptions.Center;
        _reengageText.raycastTarget = false;
        _reengageText.enableWordWrapping = true;

        CreateReengageButton("Try easier", new Color(0.3f, 0.8f, 0.4f),
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.45f), OnTryEasier);
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
        if (_reengagePanel == null) { return; }
        if (_reengagePanel.activeSelf) return;
        if (_reengageCooldown > 0f) return;
        if (currentWeek <= 0 || currentWeek > maxWeeks) return;

        bool idleTrigger = _idleTimer >= ReengageIdleThreshold;
        bool streakTrigger = PlayerModelService.Instance != null
            && PlayerModelService.Instance.failedRoundsStreak >= 2;

        if (idleTrigger || streakTrigger)
        {
            string reason = idleTrigger ? "idle" : "failStreak";
            Debug.Log($"[Reengage] Showing popup (reason={reason}, idle={_idleTimer:F1}s, streak={PlayerModelService.Instance?.failedRoundsStreak})");
            ShowReengagementPopup(idleTrigger
                ? "Looks like you're thinking...\nWant an easier round?"
                : "Tough streak!\nWant to try something easier?");
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

void OnTryEasier()
    {
        Debug.Log("[Reengage] Player chose 'Try easier' -- loading bonus round now");
        DismissReengagementPopup();

        // Cancel any pending StartNewWeek invoke
        CancelInvoke("StartNewWeek");

        // Try data-driven easy event first
        if (EventLoader.Instance != null && EventLoader.Instance.TotalEventsLoaded > 0)
        {
            EmergencyFundEvent evt = EventLoader.Instance.GetEasyEvent(_lastEventId);
            if (evt != null)
            {
                _lastWeekType = evt.type;
                _lastEventId = evt.id;
                Debug.Log($"[Reengage] Instant data-driven easy round: {evt.id}");
                ShowEventFromData(evt);
                return;
            }
        }

        // Fallback to hardcoded
        string easyType = (_lastWeekType != "bonus") ? "bonus" : "normal";
        _lastWeekType = easyType;
        Debug.Log($"[Reengage] Instant easy round (hardcoded): {easyType}");

        if (easyType == "bonus")
            ShowBonusWeek();
        else
            ShowNormalWeek();
    }

    void OnKeepGoing()
    {
        Debug.Log("[Reengage] Player chose 'Keep going'");
        DismissReengagementPopup();
    }

    
public void RestartGame()
    {
        currentWeek = 0;
        _lastWeekType = "";
        _lastEventId = "";
        _forceEasierNext = false;
        
        emergencyFund = 0;
        UpdateUI();
        ClearChoices();
        
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Let's try again!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Let's try again!");
        }
        
        Invoke("StartNewWeek", 1.5f);
    }
}

