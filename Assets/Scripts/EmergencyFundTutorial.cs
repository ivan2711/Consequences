using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmergencyFundTutorial : MonoBehaviour
{
    [Header("Tutorial Panels")]
    public GameObject tutorialContainer;
    public GameObject panel1;
    public GameObject panel2;
    public GameObject panel3;
    public Button nextButton1;
    public Button nextButton2;
    public Button startGameButton;
    
    private const string TUTORIAL_SHOWN_KEY = "EmergencyFundTutorialShown";
    
void Start()
    {
        // FOR TESTING: Always show tutorial (ignore PlayerPrefs)
        // TODO: Re-enable PlayerPrefs check before final build
        /*
        if (PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1)
        {
            if (tutorialContainer != null)
            {
                tutorialContainer.SetActive(false);
            }
            return;
        }
        */
        
        // Always show tutorial
        ShowTutorial();
    }
    
    void ShowTutorial()
    {
        if (tutorialContainer != null)
        {
            tutorialContainer.SetActive(true);
        }
        
        // Show panel 1, hide others
        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);
        
        // Setup button listeners
        if (nextButton1 != null)
        {
            nextButton1.onClick.RemoveAllListeners();
            nextButton1.onClick.AddListener(ShowPanel2);
        }
        
        if (nextButton2 != null)
        {
            nextButton2.onClick.RemoveAllListeners();
            nextButton2.onClick.AddListener(ShowPanel3);
        }
        
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(StartGame);
        }
    }
    
    void ShowPanel2()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(true);
        if (panel3 != null) panel3.SetActive(false);
    }
    
    void ShowPanel3()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(true);
    }
    
void StartGame()
    {
        // Mark tutorial as shown
        PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
        PlayerPrefs.Save();

        // Hide tutorial
        if (tutorialContainer != null)
        {
            tutorialContainer.SetActive(false);
        }

        // Note: Tutorial flow is now handled by EmergencyFundUIFlow.
        // This script is kept for backwards compatibility but is no longer used.
    }
    
    // For testing - reset tutorial
    [ContextMenu("Reset Tutorial (Show Again)")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_SHOWN_KEY);
        PlayerPrefs.Save();
        Debug.Log("Tutorial reset - will show again on next play");
    }
}
