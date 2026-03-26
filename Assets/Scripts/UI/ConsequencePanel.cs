using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ConsequencePanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI futureText;
    public Image savingsBar;
    public Image debtBar;
    public TextMeshProUGUI savingsAmountText;
    public TextMeshProUGUI debtAmountText;
    
    [Header("Colors")]
    public Color goodColor = Color.green;
    public Color badColor = Color.red;
    
public void ShowConsequences(float totalSpent, float budget, float saved, bool hasDebt)
    {
        // Calculate future projection (simplified)
        float monthlySavings = saved * 4; // Assuming weekly shopping
        float monthlyDebt = hasDebt ? (totalSpent - budget) * 4 : 0;
        
        bool calm = GameSettings.CalmMode;

        // Update title
        if (titleText != null)
        {
            if (calm)
            {
                titleText.text = "Your Financial Journey";
                titleText.color = goodColor;
            }
            else
            {
                titleText.text = saved > 0 ? "Your Financial Future" : "Consequences";
                titleText.color = saved > 0 ? goodColor : badColor;
            }
        }

        // Update future projection text
        if (futureText != null)
        {
            string future = "";

            if (saved > 0)
            {
                future = $"<b>One Month Later:</b>\n\n";
                future += $"Savings: £{monthlySavings:F2}\n";
                future += $"You can afford emergencies!\n";
                future += $"Building good habits!\n\n";
                future += $"<b>One Year Later:</b>\n";
                future += $"Potential Savings: £{(monthlySavings * 12):F2}\n";
                future += $"Could buy: New phone, laptop, or save for college!";
            }
            else if (hasDebt)
            {
                if (calm)
                {
                    future = $"<b>One Month Later:</b>\n\n";
                    future += $"You spent £{monthlyDebt:F2} more than planned.\n";
                    future += $"That's okay — now you know!\n\n";
                    future += $"<b>Next Time:</b>\n";
                    future += $"Try picking fewer treats.\n";
                    future += $"Small changes make a big difference!";
                }
                else
                {
                    future = $"<b>One Month Later:</b>\n\n";
                    future += $"Debt: £{monthlyDebt:F2}\n";
                    future += $"Stressed about money\n";
                    future += $"Can't handle emergencies\n\n";
                    future += $"<b>One Year Later:</b>\n";
                    future += $"Potential Debt: £{(monthlyDebt * 12):F2}\n";
                    future += $"Money problems pile up!";
                }
            }
            else
            {
                if (calm)
                {
                    future = $"<b>One Month Later:</b>\n\n";
                    future += $"You broke even — not bad!\n";
                    future += $"Even £1 saved each week adds up.\n\n";
                    future += $"Give it another go!";
                }
                else
                {
                    future = $"<b>One Month Later:</b>\n\n";
                    future += $"No savings, no debt\n";
                    future += $"Living paycheck to paycheck\n";
                    future += $"Could do better!\n\n";
                    future += $"Try saving just a little each week!";
                }
            }

            futureText.text = future;
        }
        
        // Update savings bar
        if (savingsBar != null && savingsAmountText != null)
        {
            float savingsPercentage = Mathf.Clamp01(monthlySavings / 100f);
            savingsBar.fillAmount = savingsPercentage;
            savingsBar.color = goodColor;
            savingsAmountText.text = $"£{monthlySavings:F2}";
        }
        
        // Update debt bar
        if (debtBar != null && debtAmountText != null)
        {
            if (hasDebt)
            {
                float debtPercentage = Mathf.Clamp01(monthlyDebt / 50f);
                debtBar.fillAmount = debtPercentage;
                debtBar.color = badColor;
                debtAmountText.text = $"£{monthlyDebt:F2}";
                debtBar.gameObject.SetActive(true);
            }
            else
            {
                debtBar.gameObject.SetActive(false);
            }
        }
    }

    public void ShowFinalConsequences(float totalSaved, float totalOverspent, int overallStars,
                                       int roundsAllEssentials = 3, int totalTreats = 0)
    {
        bool calm = GameSettings.CalmMode;
        bool fedAllRounds = roundsAllEssentials == 3;
        bool withinBudgetOverall = totalOverspent == 0f;
        float monthlySavings = totalSaved * 4f / 3f;
        float monthlyDebt = totalOverspent * 4f / 3f;

        // Ensure panel background is solid
        Image panelBg = GetComponent<Image>();
        if (panelBg != null)
            panelBg.color = new Color(0.10f, 0.12f, 0.20f, 1f);

        // Nudge text elements down to make room for stars/jar/award
        if (titleText != null)
        {
            RectTransform tRT = titleText.GetComponent<RectTransform>();
            if (tRT != null)
                tRT.anchoredPosition = new Vector2(tRT.anchoredPosition.x, tRT.anchoredPosition.y - 75f);
        }
        if (futureText != null)
        {
            RectTransform fRT = futureText.GetComponent<RectTransform>();
            if (fRT != null)
                fRT.anchoredPosition = new Vector2(fRT.anchoredPosition.x, fRT.anchoredPosition.y - 75f);
        }

        // Title — driven by essentials coverage first, then overall stars
        if (titleText != null)
        {
            titleText.fontSize = 52;
            titleText.fontStyle = FontStyles.Bold;
            if (!fedAllRounds && overallStars <= 1)
            {
                titleText.text = calm ? "Let's Try Again" : "Essentials Missed";
                titleText.color = badColor;
            }
            else if (!fedAllRounds)
            {
                titleText.text = calm ? "Room to Improve" : "Needs Improvement";
                titleText.color = Color.yellow;
            }
            else if (overallStars >= 3) { titleText.text = calm ? "Your 3-Week Journey" : "Excellent Shopping!"; titleText.color = goodColor; }
            else if (overallStars == 2) { titleText.text = calm ? "Your 3-Week Journey" : "Good Effort!";        titleText.color = goodColor; }
            else if (overallStars == 1) { titleText.text = calm ? "Room to Improve"     : "Some Issues";         titleText.color = Color.yellow; }
            else                        { titleText.text = calm ? "Let's Try Again"      : "Needs Work";          titleText.color = badColor; }
        }

        // Scorecard + projection
        if (futureText != null)
        {
            futureText.fontSize = 46;
            futureText.color = Color.white;

            string good  = "<color=#88FF88>";
            string bad   = "<color=#FF8888>";
            string warn  = "<color=#FFDD44>";
            string end   = "</color>";

            float avgTreats = totalTreats / 3f;
            bool overTreated = avgTreats > 2f;
            bool modTreated  = avgTreats > 1f;

            // --- Criterion 1: Essentials ---
            string essLine = fedAllRounds
                ? $"{good}Essentials: All 4 covered every week{end}"
                : $"{bad}Essentials: Only {roundsAllEssentials}/3 weeks fully covered{end}";

            // --- Criterion 2: Treats ---
            string treatLine;
            if (avgTreats <= 1f)
                treatLine = $"{good}Treats: Well balanced ({totalTreats} total){end}";
            else if (avgTreats <= 2f)
                treatLine = $"{warn}Treats: A few too many ({totalTreats} total) \u2014 try cutting back{end}";
            else
                treatLine = $"{bad}Treats: Too many ({totalTreats} total) \u2014 they eat into your budget{end}";

            // --- Criterion 3: Budget ---
            string budgetLine;
            if (withinBudgetOverall && totalSaved > 0)
                budgetLine = $"{good}Budget: Within budget all 3 weeks, saved \u00a3{totalSaved:F2}{end}";
            else if (withinBudgetOverall)
                budgetLine = $"{good}Budget: Within budget every week{end}";
            else if (totalOverspent > 0 && totalSaved > 0)
                budgetLine = $"{warn}Budget: Overspent \u00a3{totalOverspent:F2} some weeks, saved \u00a3{totalSaved:F2} others{end}";
            else
                budgetLine = $"{bad}Budget: Overspent \u00a3{totalOverspent:F2} across 3 weeks{end}";

            // --- Narrative consequence (all criteria combined) ---
            string narrative;
            if (!fedAllRounds && totalOverspent > 0)
                narrative = calm
                    ? "Some essentials were missed and you went over budget. Focus on needs first, then see what's left."
                    : "Missing essentials AND overspending is a serious problem. Essentials come before treats — always.";
            else if (!fedAllRounds && overTreated)
                narrative = calm
                    ? "Some essentials were skipped while spending on treats. Swap a treat for a need first."
                    : "Spending on treats while skipping essentials gets priorities backwards. Needs before wants.";
            else if (!fedAllRounds)
                narrative = calm
                    ? "You stayed in budget, but skipped some essentials. Saving only works if basic needs are met first."
                    : "Cutting essentials to save money isn't a win — your household still needs those basics every week.";
            else if (fedAllRounds && totalOverspent > 0 && overTreated)
                narrative = calm
                    ? "Essentials covered, but treats pushed you over budget. Picking one treat per week keeps things balanced."
                    : "All essentials covered, but too many treats pushed you into debt. Stick to one treat and keep the rest.";
            else if (fedAllRounds && totalOverspent > 0)
                narrative = calm
                    ? "Essentials covered every week, but you went a little over budget. You're close \u2014 a small swap would fix it."
                    : "Essentials covered but over budget. Check what tipped you over and cut there next time.";
            else if (fedAllRounds && totalSaved == 0f)
                narrative = calm
                    ? "Essentials covered and no debt \u2014 that's a solid start. Even \u00a31 saved each week adds up over time."
                    : "All essentials covered and no debt. Try setting aside a small amount each week \u2014 it builds up fast.";
            else
                narrative = calm
                    ? "Well done \u2014 all essentials covered and money saved. That's exactly what good budgeting looks like."
                    : "Essentials covered, under budget, and money saved. That's genuine financial progress.";

            // --- Projection (standard mode only) ---
            string projection = "";
            if (!calm)
            {
                if (totalSaved > 0 && totalOverspent == 0f)
                    projection = $"\n<b>At this rate (1 year):</b>\nPotential savings: \u00a3{(monthlySavings * 12):F2}";
                else if (totalOverspent > 0 && totalSaved == 0f)
                    projection = $"\n<b>At this rate (1 year):</b>\nPotential debt: \u00a3{(monthlyDebt * 12):F2}";
            }

            futureText.text = $"<b>3-Week Scorecard:</b>\n\n{essLine}\n{treatLine}\n{budgetLine}\n\n{narrative}{projection}";
        }

        // Savings bar
        if (savingsBar != null && savingsAmountText != null)
        {
            float pct = Mathf.Clamp01(totalSaved / 30f);
            savingsBar.fillAmount = pct;
            savingsBar.color = goodColor;
            savingsAmountText.text = $"\u00a3{totalSaved:F2}";
            savingsAmountText.fontSize = 44;
            savingsAmountText.fontStyle = FontStyles.Bold;
        }

        // Debt bar
        if (debtBar != null && debtAmountText != null)
        {
            if (totalOverspent > 0)
            {
                float pct = Mathf.Clamp01(totalOverspent / 20f);
                debtBar.fillAmount = pct;
                debtBar.color = badColor;
                debtAmountText.text = $"\u00a3{totalOverspent:F2}";
                debtAmountText.fontSize = 44;
                debtAmountText.fontStyle = FontStyles.Bold;
                debtBar.gameObject.SetActive(true);
            }
            else
            {
                debtBar.gameObject.SetActive(false);
            }
        }

        // Make panel visible
        gameObject.SetActive(true);
    }
}
