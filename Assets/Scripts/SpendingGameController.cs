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

    [Header("Round UI")]
    public TextMeshProUGUI roundText;

    [Header("New UI Components")]
    public MoneyCounter moneyCounter;
    public StarRating starRating;
    public DuckReaction duckReaction;
    public DuckReactionBackgroundChanger backgroundChanger;
    public ConsequencePanel consequencePanel;

    // Round system
    private int currentRound = 0;
    private const int TotalRounds = 3;
    private readonly float[] RoundBudgets = { 8.50f, 7.50f, 12.00f };
    private readonly string[] RoundNames = { "Normal Week", "Tight Week", "Payday Week" };
    private readonly string[] RoundDuckLines = {
        "Normal week \u2014 buy what you need!",
        "Tight week \u2014 budget is smaller!",
        "Payday! But don't splurge!"
    };

    // Treat definitions per round: [round-1][treatIndex] = name/price
    private readonly string[][] RoundTreatNames = {
        new[] { "Crisps", "Chocolate" },       // Round 1
        new[] { "Biscuits", "Juice" },         // Round 2
        new[] { "Crisps", "Chocolate" }        // Round 3 (base treats)
    };
    private readonly float[][] RoundTreatPrices = {
        new[] { 1.20f, 1.50f },               // Round 1
        new[] { 1.00f, 1.30f },               // Round 2
        new[] { 1.20f, 1.50f }                // Round 3 (base treats)
    };

    private float[] _roundTotals = new float[3];
    private int[] _roundStars = new int[3];
    private bool _hasLoggedThisRound = false;
    private bool _suppressToggleReaction = false;
    private bool _roundInProgress = false; // true while player is shopping, false after Check Out
    private int _roundsCompleted = 0; // only increments — used by AdvanceRound to determine next round

    // ==================== LIFECYCLE ====================

    private void Start()
    {
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

        // Auto-create round text if not wired
        if (roundText == null)
            CreateRoundText();

        // Fit items within the panel
        FitItemsInPanel();

        // Hide round 3 items initially
        SetRound3ItemsVisible(false);

        StartRound(1);
        Debug.Log("SpendingGameController: Started with 3-round system!");
    }

    // ==================== ROUND SYSTEM ====================

    void StartRound(int round)
    {
        currentRound = round;
        _roundInProgress = true;
        _hasLoggedThisRound = false;
        Debug.Log("[Spending] === StartRound(" + round + ") called ===");

        // Set budget for this round
        weeklyBudgetPounds = RoundBudgets[round - 1];

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

        // Show/hide round 3 extras
        SetRound3ItemsVisible(round == 3);

        // Fit items within panel
        FitItemsInPanel();

        // Update round text
        if (roundText != null)
            roundText.text = "Round " + round + " of " + TotalRounds + ": " + RoundNames[round - 1];

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

        // Duck intro for this round
        if (duckReaction != null)
        {
            DuckReaction.Emotion emo = round == 2 ? DuckReaction.Emotion.Worried
                : round == 3 ? DuckReaction.Emotion.Excited
                : DuckReaction.Emotion.Neutral;
            duckReaction.ShowReaction(emo, RoundDuckLines[round - 1]);
        }

        Debug.Log("[Spending] Round " + round + " started: " + RoundNames[round - 1] + " (Budget: \u00a3" + weeklyBudgetPounds + ")");
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
        int treatIndex = 0;
        foreach (var item in items)
        {
            if (!item.isNecessary && treatIndex < RoundTreatNames[round - 1].Length)
            {
                item.itemName = RoundTreatNames[round - 1][treatIndex];
                item.price = RoundTreatPrices[round - 1][treatIndex];

                // Search from the ROW parent (text objects are siblings of the toggle, not children)
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
                // Fallback: positional matching if name-based didn't work
                if (!nameSet && texts.Length >= 1)
                    texts[0].text = item.itemName;
                if (!priceSet && texts.Length >= 2)
                    texts[texts.Length - 1].text = "\u00a3" + item.price.ToString("F2");

                Debug.Log("[Spending] Treat " + treatIndex + " -> " + item.itemName + " @ " + item.price
                    + " (nameSet=" + nameSet + ", priceSet=" + priceSet + ", texts=" + texts.Length + ")");
                treatIndex++;
            }
        }
        Debug.Log("[Spending] UpdateTreatsForRound(" + round + "): set " + treatIndex + " treat(s)");
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
        _roundsCompleted++;
        Debug.Log("[Spending] Round " + currentRound + " completed. Total rounds completed: " + _roundsCompleted);

        // Duck reaction
        ShowDuckFeedback(total, unnecessaryCount, necessaryCount);

        // Build feedback text
        string feedback = BuildFeedback(total, unnecessaryCount, necessaryCount);

        if (feedbackText != null)
            feedbackText.text = feedback;

        if (totalText != null)
        {
            totalText.text = string.Format("Total: \u00a3{0:F2}", total);

            // Nudge total text 10% up and 10% left
            RectTransform trt = totalText.GetComponent<RectTransform>();
            if (trt != null)
            {
                RectTransform parentRT = trt.parent as RectTransform;
                float parentW = parentRT != null ? parentRT.rect.width : 0f;
                float parentH = parentRT != null ? parentRT.rect.height : 0f;
                trt.anchoredPosition += new Vector2(-parentW * 0.10f, parentH * 0.10f);
            }
        }

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
            uiManager.ShowFeedbackPanel(title, feedback, string.Format("Total: \u00a3{0:F2}", total));
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
            Debug.Log("[Spending] Final — Round " + (i+1) + ": spent=£" + _roundTotals[i].ToString("F2") + " budget=£" + RoundBudgets[i] + " stars=" + _roundStars[i]);

        float totalSaved = 0f;
        float totalOverspent = 0f;
        float totalBudgetAll = 0f;

        for (int i = 0; i < TotalRounds; i++)
        {
            float budget = RoundBudgets[i];
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

        // Show consequence panel with combined results
        if (consequencePanel != null)
        {
            consequencePanel.ShowFinalConsequences(totalSaved, totalOverspent, overallStars);
        }

        // Show summary in feedback text
        string summary = "3-Week Summary\n\n";
        for (int i = 0; i < TotalRounds; i++)
        {
            string status = _roundTotals[i] <= RoundBudgets[i] ? "Under budget" : "Over budget";
            float diff = Mathf.Abs(_roundTotals[i] - RoundBudgets[i]);
            summary += RoundNames[i] + ": \u00a3" + _roundTotals[i].ToString("F2")
                + " / \u00a3" + RoundBudgets[i].ToString("F2")
                + " (" + status;
            if (diff > 0.01f)
                summary += " by \u00a3" + diff.ToString("F2");
            summary += ")\n";
        }

        summary += "\n";
        if (totalSaved > 0 && totalOverspent == 0)
            summary += "You saved \u00a3" + totalSaved.ToString("F2") + " across all 3 weeks. Your family is in great shape!";
        else if (totalSaved > 0 && totalOverspent > 0)
            summary += "Saved \u00a3" + totalSaved.ToString("F2") + " but overspent \u00a3" + totalOverspent.ToString("F2") + " in other weeks. Try to stay under budget every week!";
        else if (totalOverspent > 0)
            summary += "Overspent \u00a3" + totalOverspent.ToString("F2") + " total. Remember, you can't spend money you don't have!";
        else
            summary += "You spent exactly your budget each week. Try saving a little for a rainy day!";

        if (feedbackText != null)
            feedbackText.text = summary;

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

            // Update button text and re-wire to ResetGame for replay
            Button[] btns = feedbackPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in btns)
            {
                TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                    btnLabel.text = "Play Again";

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ResetGame);
            }
            EnlargePanelButtons(btns);
        }

        if (uiManager != null)
            uiManager.ShowFeedbackPanel("Season Complete!", summary, "");
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
        if (GameSettings.CalmMode)
        {
            if (total <= weeklyBudgetPounds && unnecessaryCount <= 1)
                return 3;
            if (total <= weeklyBudgetPounds)
                return 2;
            if (total <= weeklyBudgetPounds + 3)
                return 1;
            return 0;
        }

        if (total <= weeklyBudgetPounds && unnecessaryCount == 0 && necessaryCount >= 4)
            return 3;
        if (total <= weeklyBudgetPounds && unnecessaryCount <= 2)
            return 2;
        if (total <= weeklyBudgetPounds + 2 || (unnecessaryCount >= 3 && total <= weeklyBudgetPounds))
            return 1;
        return 0;
    }

    private string BuildFeedback(float total, int unnecessaryCount, int necessaryCount)
    {
        if (total == 0)
            return "Nothing in your basket!\n\nYour family needs essentials like milk, eggs, and vegetables to get through the week.";

        string header = "Round " + currentRound + ": " + RoundNames[currentRound - 1] + "\n";
        float remaining = weeklyBudgetPounds - total;
        int missingEssentials = 4 - necessaryCount;

        if (total <= weeklyBudgetPounds)
        {
            if (necessaryCount == 4 && unnecessaryCount == 0)
                return header + string.Format(
                    "Spent: \u00a3{0:F2} of \u00a3{1:F2}  (Saved: \u00a3{2:F2})\n\n" +
                    "All essentials bought -- your family has everything they need this week. Saving the rest is a smart choice.",
                    total, weeklyBudgetPounds, remaining);
            if (necessaryCount == 4 && unnecessaryCount == 1)
                return header + string.Format(
                    "Spent: \u00a3{0:F2} of \u00a3{1:F2}\n\n" +
                    "All essentials plus one treat -- great balance! Your family is fed and you stayed within budget.",
                    total, weeklyBudgetPounds);
            if (unnecessaryCount > 1)
                return header + string.Format(
                    "Spent: \u00a3{0:F2} of \u00a3{1:F2}\n\n" +
                    "You picked {2} treats. Try to stick to just one -- treats are fun but essentials come first.",
                    total, weeklyBudgetPounds, unnecessaryCount);
            if (missingEssentials > 0)
                return header + string.Format(
                    "Spent: \u00a3{0:F2} of \u00a3{1:F2}\n\n" +
                    "You're missing {2} essential(s). Without basics like milk, eggs, or rice your family won't have enough food this week.",
                    total, weeklyBudgetPounds, missingEssentials);
        }

        float over = total - weeklyBudgetPounds;
        if (missingEssentials > 0)
            return header + string.Format(
                "Spent: \u00a3{0:F2} of \u00a3{1:F2}\n\n" +
                "Over budget by \u00a3{2:F2} and missing {3} essential(s). Focus on what your family needs before adding treats.",
                total, weeklyBudgetPounds, over, missingEssentials);
        return header + string.Format(
            "Spent: \u00a3{0:F2} of \u00a3{1:F2}\n\n" +
            "Over budget by \u00a3{2:F2}! You can't spend more than you have. Try removing some treats to get back on track.",
            total, weeklyBudgetPounds, over);
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
        currentRound = 0;
        _roundInProgress = false;
        _roundsCompleted = 0;
        _roundTotals = new float[3];
        _roundStars = new int[3];
        _hasLoggedThisRound = false;

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
            feedbackPanel.SetActive(false);

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
                    t.color = new Color(1f, 0.85f, 0.2f);
                    t.fontSize = 44;
                    t.fontStyle = FontStyles.Bold;
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

            // Hide StarRatingPanel — stars not used
            Transform starPanel = feedbackPanel.transform.Find("StarRatingPanel");
            if (starPanel != null)
                starPanel.gameObject.SetActive(false);

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

        string[] extraNames = { "Toy", "Video Game", "Sweets" };
        float[] extraPrices = { 4.00f, 5.00f, 2.00f };

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

    void CreateRoundText()
    {
        // Create round text near the top of the canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("RoundText");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.85f);
        rect.anchorMax = new Vector2(0.95f, 0.93f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        roundText = go.AddComponent<TMPro.TextMeshProUGUI>();
        roundText.text = "Round 1 of 3";
        roundText.fontSize = 40;
        roundText.color = new Color(0.15f, 0.15f, 0.15f);
        roundText.fontStyle = TMPro.FontStyles.Bold;
        roundText.alignment = TMPro.TextAlignmentOptions.Center;
        roundText.raycastTarget = false;
    }

}
