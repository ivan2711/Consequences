using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinancialLiteracy.UI
{
    public class SpendingGameUI : MonoBehaviour
    {
        [Header("References - Auto Setup")]
        public Canvas mainCanvas;
        public GameObject shoppingPanel;
        public GameObject feedbackPanel;
        public GameObject consequencePanel;
        
        [Header("Shopping Panel UI")]
        public TMP_Text titleText;
        public TMP_Text budgetText;
        public Transform itemsContainer;
        public Button continueButton;
        
        [Header("Feedback Panel UI")]
        public TMP_Text feedbackTitleText;
        public TMP_Text feedbackMessageText;
        public TMP_Text totalSpentText;
        public Button viewConsequencesButton;
        public Button closeFeedbackButton;
        
        [Header("Consequence Panel UI")]
        public TMP_Text consequenceTitleText;
        public Image moneyMeterFill;
        public TMP_Text moneyMeterText;
        public Image budgetBarFill;
        public TMP_Text consequenceMessageText;
        public Button closeConsequenceButton;

        private void Awake()
        {
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);
                
            if (consequencePanel != null)
                consequencePanel.SetActive(false);
        }

        public void ShowFeedbackPanel(string title, string message, string totalSpent)
        {
            if (feedbackPanel == null) return;
            
            feedbackPanel.SetActive(true);
            
            if (feedbackTitleText != null)
                feedbackTitleText.text = title;
                
            if (feedbackMessageText != null)
                feedbackMessageText.text = message;
                
            if (totalSpentText != null)
                totalSpentText.text = totalSpent;
        }

        public void HideFeedbackPanel()
        {
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);
        }

        public void ShowConsequencePanel(string title, string message, float moneyPercent, float budgetPercent)
        {
            if (consequencePanel != null)
            {
                consequencePanel.SetActive(true);
                
                if (consequenceTitleText != null)
                    consequenceTitleText.text = title;
                    
                if (consequenceMessageText != null)
                    consequenceMessageText.text = message;
                    
                if (moneyMeterFill != null)
                {
                    moneyMeterFill.fillAmount = moneyPercent;
                    
                    // Animate the fill
                    StartCoroutine(AnimateFillAmount(moneyMeterFill, moneyPercent));
                }
                
                if (budgetBarFill != null)
                {
                    budgetBarFill.fillAmount = budgetPercent;

                    // Color code: green if under budget; calm mode uses amber instead of red
                    if (budgetPercent <= 1f)
                        budgetBarFill.color = Color.green;
                    else
                        budgetBarFill.color = GameSettings.CalmMode ? new Color(0.9f, 0.7f, 0.2f) : Color.red;
                }
                
                if (moneyMeterText != null)
                    moneyMeterText.text = $"£{(moneyPercent * 100):F0}";
            }
        }

        public void HideConsequencePanel()
        {
            if (consequencePanel != null)
                consequencePanel.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateFillAmount(Image fillImage, float targetAmount)
        {
            float duration = GameSettings.CalmMode ? 1.8f : 1f;
            float elapsed = 0f;
            float startAmount = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fillImage.fillAmount = Mathf.Lerp(startAmount, targetAmount, elapsed / duration);
                yield return null;
            }

            fillImage.fillAmount = targetAmount;
        }

        public void SetBudgetText(int budget)
        {
            if (budgetText != null)
                budgetText.text = $"Weekly Budget: £{budget}";
        }
    }
}