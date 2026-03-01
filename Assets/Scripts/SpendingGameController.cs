using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FinancialLiteracy.UI;


public class SpendingGameController : MonoBehaviour
{
    private bool _hasLoggedThisRound = false;


private void Start()
    {
        // Auto-wire toggle listeners on start
        foreach (var item in items)
        {
            if (item.toggle != null)
            {
                item.toggle.onValueChanged.AddListener((bool value) => OnItemToggled());
            }
        }
        
        // Initialize money counter
        if (moneyCounter != null)
        {
            moneyCounter.SetSpent(0, false);
        }
        
        Debug.Log("SpendingGameController: Started and wired up!");
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public float price;
        public bool isNecessary;
        public Toggle toggle;
    }

    [Header("Config")]
    public float weeklyBudgetPounds = 8;

    [Header("Items")]
    public List<ShopItem> items = new List<ShopItem>();

    
    [Header("UI Manager")]
    public SpendingGameUI uiManager;
[Header("Feedback UI")]
    public GameObject feedbackPanel;
    public TMP_Text totalText;
    [Header("New UI Components")]
    public MoneyCounter moneyCounter;
    public StarRating starRating;
    public DuckReaction duckReaction;
    public DuckReactionBackgroundChanger backgroundChanger;
    public ConsequencePanel consequencePanel;
public TMP_Text feedbackText;

public void OnContinue()
    {
        float total = 0;
        int unnecessaryCount = 0;
        int necessaryCount = 0;

        foreach (var it in items)
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

        // Calculate stars
        
        // Log spend to persistent bank account
        if (!_hasLoggedThisRound && BankAccountService.Instance != null)
        {
            _hasLoggedThisRound = true;
            string category = (unnecessaryCount == 0) ? "Needs" : "Mixed";
            BankAccountService.Instance.Spend(total, "Weekly Shopping", category);            var hud = FindObjectOfType<BankHud>();
            if (hud != null) hud.Refresh();
            // Record round in player model
            if (PlayerModelService.Instance != null)
            {
                int totalItems = necessaryCount + unnecessaryCount;
                PlayerModelService.Instance.RecordSpendingRound(total, weeklyBudgetPounds, unnecessaryCount, totalItems);
            }


        }
int stars = CalculateStars(total, unnecessaryCount, necessaryCount);
        
        // Show star rating
        if (starRating != null)
        {
            starRating.SetRating(stars);
        }
        
        // SHORT DUCK REACTIONS FOR FINAL FEEDBACK
        if (duckReaction != null)
        {
            bool withinBudget = (total <= weeklyBudgetPounds);
            bool allEssentials = (necessaryCount == 4);
            bool oneTreat = (unnecessaryCount == 1);
            bool overBudget = (total > weeklyBudgetPounds);
            bool tooManyTreats = (unnecessaryCount > 1);
            bool calm = GameSettings.CalmMode;

            // PERFECT
            if (allEssentials && oneTreat && withinBudget)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Celebrating, "PERFECT! ***");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("PERFECT! ***");
            }
            // EXCELLENT
            else if (allEssentials && unnecessaryCount == 0 && withinBudget)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Excellent! **");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Excellent! **");
            }
            // GOOD
            else if (allEssentials && withinBudget)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Good! **");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Good! **");
            }
            // OVER BUDGET
            else if (overBudget)
            {
                string msg = calm ? "A bit over — try removing a treat." : "Over budget!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Shocked;
                duckReaction.ShowReaction(emo, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
            // TOO MANY TREATS
            else if (tooManyTreats && withinBudget)
            {
                string msg = calm ? "Try picking just one treat." : "Too many treats! *";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Worried;
                duckReaction.ShowReaction(emo, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
            // MISSING ESSENTIALS
            else if (!allEssentials && withinBudget)
            {
                string msg = calm ? "Don't forget the essentials!" : "Missing essentials!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Neutral : DuckReaction.Emotion.Sad;
                duckReaction.ShowReaction(emo, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
            // DEFAULT
            else
            {
                string msg = calm ? "Give it another go!" : "Try again!";
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
        }

        // Legacy support
        if (totalText != null)
            totalText.text = string.Format("Total: £{0:F2}", total);

        string feedback = BuildFeedback(total, unnecessaryCount);
        
        if (feedbackText != null)
            feedbackText.text = feedback;
        
        // Show consequences panel
        if (consequencePanel != null)
        {
            float saved = Mathf.Max(0, weeklyBudgetPounds - total);
            bool hasDebt = total > weeklyBudgetPounds;
            consequencePanel.ShowConsequences(total, weeklyBudgetPounds, saved, hasDebt);
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
            
        // New UI system
        if (uiManager != null)
        {
            string title = total <= weeklyBudgetPounds ? "Good Job!" : "Over Budget";
            uiManager.ShowFeedbackPanel(title, feedback, string.Format("Total: £{0:F2}", total));
        }
    }

public void ShowConsequences()
    {
        float total = 0;
        foreach (var it in items)
        {
            if (it.toggle != null && it.toggle.isOn)
            {
                total += it.price;
            }
        }
        
        if (uiManager != null)
        {
            float moneyPercent = (float)total / 100f; // Out of £100
            float budgetPercent = (float)total / (float)weeklyBudgetPounds;
            
            string title = total <= weeklyBudgetPounds ? "Financial Impact: Positive" : "Financial Impact: Warning";
            string message = total <= weeklyBudgetPounds 
                ? "You stayed within budget! Your savings account is growing and you have money left for emergencies."
                : $"You overspent by £{total - weeklyBudgetPounds:F2}. This could lead to debt or using savings meant for other goals.";
            
            uiManager.HideFeedbackPanel();
            uiManager.ShowConsequencePanel(title, message, moneyPercent, budgetPercent);
        }
    }


    public void CloseFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

private string BuildFeedback(float total, int unnecessaryCount)
    {
        string feedback = "";
        float over = total - weeklyBudgetPounds;
        int necessaryCount = 0;
        
        foreach (var it in items)
        {
            if (it.toggle != null && it.toggle.isOn && it.isNecessary)
            {
                necessaryCount++;
            }
        }
        
        // Nothing chosen
        if (total == 0)
        {
            return "Nothing in your basket!\n\nPick some essentials";
        }
        
        // Within budget - GOOD!
        if (total <= weeklyBudgetPounds)
        {
            float saved = weeklyBudgetPounds - total;
            // feedback = $"£ Spent: £{total:F2}\n£ Saved: £{saved:F2}\n";
            
            if (unnecessaryCount == 0 && necessaryCount == 4)
            {
                feedback += $"Spent: £{total:F2} of £{weeklyBudgetPounds}\n\nPerfect! All essentials, no treats!";
            }
            else if (unnecessaryCount == 1 && necessaryCount == 4)
            {
                feedback += $"Spent: £{total:F2} of £{weeklyBudgetPounds}\n\nAll essentials + 1 treat!";
            }
            else if (unnecessaryCount > 1)
            {
                feedback += $"Spent: £{total:F2} of £{weeklyBudgetPounds}\n\n{unnecessaryCount} treats — try just 1!";
            }
            else if (necessaryCount < 4)
            {
                int missing = 4 - necessaryCount;
                feedback += $"Spent: £{total:F2} of £{weeklyBudgetPounds}\n\nMissing {missing} essential(s)!";
            }
        }
        // Over budget - BAD!
        else
        {
            feedback = $"Spent: £{total:F2} of £{weeklyBudgetPounds}\n\nOver by £{over:F2}! Remove treats!";
        }
        
        return feedback;
    }


public void OnItemToggled()
    {
        // Calculate current totals
        float total = CalculateCurrentTotal();
        int treatsCount = 0;
        int essentialsCount = 0;
        
        foreach (var it in items)
        {
            if (it.toggle != null && it.toggle.isOn)
            {
                if (!it.isNecessary)
                    treatsCount++;
                else
                    essentialsCount++;
            }
        }
        
        // Update money counter in real-time
        if (moneyCounter != null)
        {
            moneyCounter.SetSpent(total, true);
        }
        
        // DYNAMIC DUCK REACTIONS - SHORT TEXT
        if (duckReaction != null)
        {
            float remaining = weeklyBudgetPounds - total;
            bool calm = GameSettings.CalmMode;

            // PRIORITY 1: Over budget
            if (total > weeklyBudgetPounds)
            {
                string msg = calm ? "Try removing something." : "TOO MUCH!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Shocked;
                duckReaction.ShowReaction(emo, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
            // PRIORITY 2: Too many treats
            else if (treatsCount > 1)
            {
                string msg = calm ? "Maybe fewer treats?" : "Too many treats!";
                DuckReaction.Emotion emo = calm ? DuckReaction.Emotion.Thinking : DuckReaction.Emotion.Worried;
                duckReaction.ShowReaction(emo, msg);
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground(msg);
            }
            // PRIORITY 3: Close to budget
            else if (remaining <= 1.5f && total > 0)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Thinking, "Almost done!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Almost done!");
            }
            // PRIORITY 4: Good progress
            else if (total >= 5f)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Happy, "Good job!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Good job!");
            }
            // PRIORITY 5: Just started
            else if (total > 0f)
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Excited, "Great!");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Great!");
            }
            // PRIORITY 6: Nothing selected
            else
            {
                duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Pick items");
                if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Pick items");
            }
        }
    }
    
private float CalculateCurrentTotal()
    {
        float total = 0f;
        foreach (var it in items)
        {
            if (it.toggle != null && it.toggle.isOn)
            {
                total += it.price;
            }
        }
        return total;
    }
    
    private int CalculateStars(float total, int unnecessaryCount, int necessaryCount)
    {
        if (GameSettings.CalmMode)
        {
            // Calm mode: more forgiving thresholds
            // 3 stars: Within budget with at most 1 treat
            if (total <= weeklyBudgetPounds && unnecessaryCount <= 1)
                return 3;
            // 2 stars: Within budget
            if (total <= weeklyBudgetPounds)
                return 2;
            // 1 star: Only slightly over (up to £3)
            if (total <= weeklyBudgetPounds + 3)
                return 1;
            return 0;
        }

        // 3 stars: Perfect budget, only essentials
        if (total <= weeklyBudgetPounds && unnecessaryCount == 0 && necessaryCount >= 4)
        {
            return 3;
        }

        // 2 stars: In budget, 1-2 treats
        if (total <= weeklyBudgetPounds && unnecessaryCount <= 2)
        {
            return 2;
        }

        // 1 star: Slightly over budget OR too many treats
        if (total <= weeklyBudgetPounds + 2 || (unnecessaryCount >= 3 && total <= weeklyBudgetPounds))
        {
            return 1;
        }

        // 0 stars: Way over budget or chose nothing
        return 0;
    }





public void ResetGame()
    {
        // Uncheck all items
        foreach (var it in items)
        {
            if (it.toggle != null)
            {
                it.toggle.isOn = false;
            }
        }
        
        // Reset money counter
        if (moneyCounter != null)
        {
            moneyCounter.SetSpent(0, false);
        }
        
        // Reset duck to neutral
        if (duckReaction != null)
        {
            duckReaction.ShowReaction(DuckReaction.Emotion.Neutral, "Ready to shop!");
            if (backgroundChanger != null) backgroundChanger.CheckAndChangeBackground("Ready to shop!");
        }
        
        // Reset stars
        if (starRating != null)
        {
            starRating.SetRating(0);
        }
        
        // Close feedback panel
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
        
        
        _hasLoggedThisRound = false;
Debug.Log("SpendingGameController: Game reset - ready to play again!");
    }
}
