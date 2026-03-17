using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinancialLiteracy.UI
{
    public class EmergencyFundConsequencePanel : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelContainer;
        public GameObject bridgePanel;
        public GameObject panel1;
        public GameObject panel2;
        public GameObject panel3;
        
        [Header("Bridge Panel")]
        public TextMeshProUGUI bridgeAmountText;
        public TextMeshProUGUI bridgeStarsText;
        public Button bridgeNextButton;
        
        [Header("Panel 1 - Today")]
        public TextMeshProUGUI panel1AmountText;
        public TextMeshProUGUI panel1GoalText;
        public Button panel1NextButton;
        
        [Header("Panel 2 - Journey")]
        public TextMeshProUGUI panel2JourneyText;
        public Button panel2NextButton;
        
        [Header("Panel 3 - Lesson")]
        public TextMeshProUGUI panel3LessonText;
        public Button panel3PlayAgainButton;
        
        private int finalAmount;
        private int weeklyCosts = 80;
        
        void Start()
        {
            if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
            
            // Wire up buttons
            if (bridgeNextButton != null)
            {
                bridgeNextButton.onClick.RemoveAllListeners();
                bridgeNextButton.onClick.AddListener(ShowPanel1);
            }
            
            if (panel1NextButton != null)
            {
                panel1NextButton.onClick.RemoveAllListeners();
                panel1NextButton.onClick.AddListener(ShowPanel2);
            }
            
            if (panel2NextButton != null)
            {
                panel2NextButton.onClick.RemoveAllListeners();
                panel2NextButton.onClick.AddListener(ShowPanel3);
            }
            
            if (panel3PlayAgainButton != null)
            {
                panel3PlayAgainButton.onClick.RemoveAllListeners();
                panel3PlayAgainButton.onClick.AddListener(PlayAgain);
            }
        }
        
public void ShowConsequences(int emergencyFund, int stars)
        {
            Debug.Log($"🔵 ShowConsequences called! Fund: £{emergencyFund}, Stars: {stars}");
            
            finalAmount = emergencyFund;
            
            if (panelContainer != null)
            {
                Debug.Log("✅ Activating panel container...");
                panelContainer.SetActive(true);
            }
            else
            {
                Debug.LogError("❌ panelContainer is NULL!");
            }
            
            // Show bridge panel first
            ShowBridgePanel(stars);
        }
        
void ShowBridgePanel(int stars)
        {
            Debug.Log($"🔶 ShowBridgePanel called with {stars} stars");
            
            if (bridgePanel != null) 
            {
                Debug.Log("✅ Activating bridge panel...");
                bridgePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("❌ bridgePanel is NULL!");
            }
            
            if (panel1 != null) panel1.SetActive(false);
            if (panel2 != null) panel2.SetActive(false);
            if (panel3 != null) panel3.SetActive(false);
            
            if (bridgeAmountText != null)
            {
                bridgeAmountText.text = "Emergency Fund: £" + finalAmount;
                Debug.Log($"✅ Set bridge amount text: £{finalAmount}");
            }
            else
            {
                Debug.LogError("❌ bridgeAmountText is NULL!");
            }
            
            if (bridgeStarsText != null)
            {
                string starDisplay = "";
                for (int i = 0; i < stars; i++)
                {
                    starDisplay += "⭐";
                }
                bridgeStarsText.text = starDisplay;
                Debug.Log($"✅ Set bridge stars: {starDisplay}");
            }
            else
            {
                Debug.LogError("❌ bridgeStarsText is NULL!");
            }
            
            Debug.Log("✅ ShowBridgePanel complete!");
        }
        
        void ShowPanel1()
        {
            if (bridgePanel != null) bridgePanel.SetActive(false);
            if (panel1 != null) panel1.SetActive(true);
            if (panel2 != null) panel2.SetActive(false);
            if (panel3 != null) panel3.SetActive(false);
            
            if (panel1AmountText != null)
            {
                panel1AmountText.text = "Emergency Fund: £" + finalAmount + "\n\nGreat start! 😊";
            }
            
            if (panel1GoalText != null)
            {
                int basicGoal = weeklyCosts * 12; // 3 months
                int strongGoal = weeklyCosts * 24; // 6 months
                
                panel1GoalText.text = 
                    "THE GOAL:\n" +
                    "Keep building your emergency fund:\n\n" +
                    "Basic protection: £" + basicGoal + "\n" +
                    "(That's 3 months of your costs)\n\n" +
                    "Strong protection: £" + strongGoal + "\n" +
                    "(That's 6 months of your costs)\n\n" +
                    "You're on the right path!\n" +
                    "Keep saving...";
            }
        }
        
        void ShowPanel2()
        {
            if (bridgePanel != null) bridgePanel.SetActive(false);
            if (panel1 != null) panel1.SetActive(false);
            if (panel2 != null) panel2.SetActive(true);
            if (panel3 != null) panel3.SetActive(false);
            
            if (panel2JourneyText != null)
            {
                panel2JourneyText.text = 
                    "⏰ WHAT HAPPENS NEXT\n\n" +
                    "1 MONTH LATER:\n" +
                    "You kept saving... Fund: £840\n" +
                    "🚨 Emergency! Charger broke (£40)\n" +
                    "✅ Used fund → Now: £800\n" +
                    "✅ No debt! Keep rebuilding\n\n" +
                    "3 MONTHS LATER:\n" +
                    "Kept saving... Fund: £1,100\n" +
                    "🎉 GOAL REACHED! (3+ months covered!)\n" +
                    "🚨 Emergency! Phone screen (£80)\n" +
                    "✅ Used fund → Now: £1,020\n" +
                    "✅ Still protected! This is how it works!\n\n" +
                    "6 MONTHS LATER:\n" +
                    "Kept saving... Fund: £1,500\n" +
                    "🚨 BIG Emergency! Laptop died (£200)\n" +
                    "✅ Used fund → Now: £1,300\n" +
                    "✅ Fund saved you! No stress, no debt!";
            }
        }
        
        void ShowPanel3()
        {
            if (bridgePanel != null) bridgePanel.SetActive(false);
            if (panel1 != null) panel1.SetActive(false);
            if (panel2 != null) panel2.SetActive(false);
            if (panel3 != null) panel3.SetActive(true);

            if (panel3LessonText != null)
            {
                panel3LessonText.text =
                    "WHAT YOU LEARNED\n\n" +
                    "Emergency Fund = PROTECTION\n\n" +
                    "The Cycle:\n" +
                    "1. BUILD IT (save weekly)\n" +
                    "2. USE IT (emergencies happen)\n" +
                    "3. REBUILD IT (keep saving)\n" +
                    "4. STAY PROTECTED (repeat!)\n\n" +
                    "YOUR RULES:\n" +
                    "- Save until 3-6 months covered\n" +
                    "- Use ONLY for emergencies\n" +
                    "- Rebuild after each use\n" +
                    "- Never go into debt!";
            }

            // Hide existing Play Again button and add Home + Play Again
            if (panel3PlayAgainButton != null)
                panel3PlayAgainButton.gameObject.SetActive(false);

            EndGameButtons.Create(panel3.transform, PlayAgain);
        }
        
        void PlayAgain()
        {
            if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
            
            // Find and restart the game controller
            EmergencyFundController controller = FindObjectOfType<EmergencyFundController>();
            if (controller != null)
            {
                controller.RestartGame();
            }
        }
    }
}
