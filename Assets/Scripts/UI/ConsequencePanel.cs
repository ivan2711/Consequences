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

    public void ShowFinalConsequences(float totalSaved, float totalOverspent, int overallStars)
    {
        bool calm = GameSettings.CalmMode;
        bool netPositive = totalSaved > totalOverspent;
        float netAmount = netPositive ? totalSaved - totalOverspent : totalOverspent - totalSaved;

        // Ensure panel background is solid
        Image panelBg = GetComponent<Image>();
        if (panelBg != null)
            panelBg.color = new Color(0.10f, 0.12f, 0.20f, 1f);

        // Title
        if (titleText != null)
        {
            titleText.fontSize = 44;
            titleText.fontStyle = FontStyles.Bold;
            if (calm)
            {
                titleText.text = "Your 3-Week Journey";
                titleText.color = goodColor;
            }
            else
            {
                titleText.text = netPositive ? "Great Financial Habits!" : "Financial Consequences";
                titleText.color = netPositive ? goodColor : badColor;
            }
        }

        // Future projection based on 3 weeks combined
        if (futureText != null)
        {
            futureText.fontSize = 44;
            futureText.color = Color.white;
            string future = "";

            if (netPositive && totalSaved > 0)
            {
                float monthlySavings = totalSaved * 4f / 3f; // weekly average × 4
                future = "<b>Over 3 Weeks:</b>\n\n";
                future += $"You saved \u00a3{totalSaved:F2} total!\n\n";
                future += "<b>At This Rate (1 Year):</b>\n";
                future += $"Potential Savings: \u00a3{(monthlySavings * 12):F2}\n";
                future += "That could buy something amazing!";
            }
            else if (totalOverspent > 0)
            {
                float monthlyDebt = totalOverspent * 4f / 3f;
                if (calm)
                {
                    future = "<b>Over 3 Weeks:</b>\n\n";
                    future += $"You overspent by \u00a3{totalOverspent:F2}.\n";
                    future += "That's okay \u2014 now you know!\n\n";
                    future += "<b>Next Time:</b>\n";
                    future += "Try sticking to essentials first.\n";
                    future += "Small changes add up!";
                }
                else
                {
                    future = "<b>Over 3 Weeks:</b>\n\n";
                    future += $"Overspent: \u00a3{totalOverspent:F2}\n\n";
                    future += "<b>At This Rate (1 Year):</b>\n";
                    future += $"Potential Debt: \u00a3{(monthlyDebt * 12):F2}\n";
                    future += "That adds up quickly!";
                }
            }
            else
            {
                future = "<b>Over 3 Weeks:</b>\n\n";
                future += "You broke even \u2014 spent exactly your budget.\n\n";
                future += calm
                    ? "Even \u00a31 saved each week adds up!"
                    : "Try saving a little next time!";
            }

            futureText.text = future;
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
