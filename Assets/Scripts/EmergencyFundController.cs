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
        CreatePlayAgainButton();
        
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
    
    public void RestartGame()
    {
        currentWeek = 0;
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

