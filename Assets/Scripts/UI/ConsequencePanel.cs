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
        
        // Update title
        if (titleText != null)
        {
            titleText.text = saved > 0 ? "Your Financial Future" : "Consequences";
            titleText.color = saved > 0 ? goodColor : badColor;
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
                future = $"<b>One Month Later:</b>\n\n";
                future += $"Debt: £{monthlyDebt:F2}\n";
                future += $"Stressed about money\n";
                future += $"Can't handle emergencies\n\n";
                future += $"<b>One Year Later:</b>\n";
                future += $"Potential Debt: £{(monthlyDebt * 12):F2}\n";
                future += $"Money problems pile up!";
            }
            else
            {
                future = $"<b>One Month Later:</b>\n\n";
                future += $"No savings, no debt\n";
                future += $"Living paycheck to paycheck\n";
                future += $"Could do better!\n\n";
                future += $"Try saving just a little each week!";
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
}
