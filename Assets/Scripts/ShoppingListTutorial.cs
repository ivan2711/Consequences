using UnityEngine;
using UnityEngine.UI;

public class ShoppingListTutorial : MonoBehaviour
{
    public GameObject tutorialPanel;
    public Button startButton;
    
    void Start()
    {
        // Don't auto-show — SpendingGameController.StartRound(1) handles showing the tutorial
    }
    
    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }
}
