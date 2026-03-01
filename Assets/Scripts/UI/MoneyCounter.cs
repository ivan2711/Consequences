using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MoneyCounter : MonoBehaviour
{
    [Header("References")]
    public Image budgetBarFill;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI budgetText;
    
    [Header("Settings")]
    public float totalBudget = 8.5f;
    public float animationSpeed = 0.5f;
    
    [Header("Colors")]
    public Color safeColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    
    [Header("Thresholds")]
    public float warningThreshold = 0.7f; // 70% of budget
    public float dangerThreshold = 0.9f;  // 90% of budget
    
    private float currentSpent = 0f;
    private float targetFillAmount = 0f;
    
    void Start()
    {
        UpdateDisplay();
    }
    
public void SetSpent(float amount, bool animate = true)
    {
        currentSpent = amount;
        targetFillAmount = Mathf.Clamp01(currentSpent / totalBudget);

        if (animate && !GameSettings.CalmMode)
        {
            StartCoroutine(AnimateToTarget());
        }
        else
        {
            budgetBarFill.fillAmount = targetFillAmount;
            UpdateDisplay();
        }
    }
    
    private IEnumerator AnimateToTarget()
    {
        float startFill = budgetBarFill.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < animationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationSpeed;
            
            budgetBarFill.fillAmount = Mathf.Lerp(startFill, targetFillAmount, t);
            UpdateDisplay();
            
            yield return null;
        }
        
        budgetBarFill.fillAmount = targetFillAmount;
        UpdateDisplay();
    }
    
private void UpdateDisplay()
    {
        // Update text
        if (moneyText != null)
        {
            moneyText.text = $"£{currentSpent:F2}";
        }
        
        if (budgetText != null)
        {
            float remaining = totalBudget - currentSpent;
            
            // Show negative values when over budget
            if (remaining < 0)
            {
                budgetText.text = $"Remaining: -£{Mathf.Abs(remaining):F2}";
            }
            else
            {
                budgetText.text = $"Remaining: £{remaining:F2}";
            }
        }
        
        // Update color based on spending
        if (budgetBarFill != null)
        {
            float percentage = currentSpent / totalBudget;

            if (GameSettings.CalmMode)
            {
                // Calm mode: stay on safe color, use gentle amber instead of red
                if (percentage >= dangerThreshold)
                    budgetBarFill.color = warningColor;
                else
                    budgetBarFill.color = safeColor;
            }
            else
            {
                if (percentage >= dangerThreshold)
                    budgetBarFill.color = dangerColor;
                else if (percentage >= warningThreshold)
                    budgetBarFill.color = warningColor;
                else
                    budgetBarFill.color = safeColor;
            }
        }
    }
    
public float GetSpent()
    {
        return currentSpent;
    }
    
public float GetRemaining()
    {
        return totalBudget - currentSpent;
    }
    
    public bool IsOverBudget()
    {
        return currentSpent > totalBudget;
    }
}
