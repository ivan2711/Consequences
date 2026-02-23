using UnityEngine;
using UnityEngine.UI;

public class ShoppingListTutorial : MonoBehaviour
{
    public GameObject tutorialPanel;
    public Button startButton;
    
    void Start()
    {
        // Show tutorial on start
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }
    
    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}
