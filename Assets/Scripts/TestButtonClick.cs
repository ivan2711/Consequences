using UnityEngine;
using UnityEngine.UI;

public class TestButtonClick : MonoBehaviour
{
void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClicked);
            Debug.Log("TestButtonClick: Button listener added!");
        }
        else
        {
            Debug.LogError("TestButtonClick: No Button component found!");
        }
        
        // Wire up close button - search in inactive objects too
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach(Button b in allButtons)
        {
            if(b.gameObject.name == "CloseButton")
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(CloseFeedbackPanel);
                Debug.Log("Close button wired up!");
                break;
            }
        }
    }
    
    void OnButtonClicked()
    {
        Debug.Log("✅ BUTTON CLICKED SUCCESSFULLY!");
        
        // Find ALL feedback panels
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach(GameObject obj in allObjects)
        {
            if(obj.name == "FeedbackPanel")
            {
                Debug.Log("Found FeedbackPanel: " + obj.name + ", active=" + obj.activeSelf);
                obj.SetActive(true);
                Debug.Log("Set FeedbackPanel to ACTIVE!");
            }
        }
        
        // Find the controller
        SpendingGameController controller = FindObjectOfType<SpendingGameController>();
        if (controller != null)
        {
            Debug.Log("Found SpendingGameController, calling OnContinue...");
            controller.OnContinue();
        }
        else
        {
            Debug.LogError("SpendingGameController not found!");
        }
    }
    
void CloseFeedbackPanel()
    {
        // Close via controller if available
        SpendingGameController controller = FindObjectOfType<SpendingGameController>();
        if (controller != null)
        {
            controller.CloseFeedback();
            Debug.Log("Closed via controller!");
        }
        
        // Also manually close all feedback panels
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach(GameObject obj in allObjects)
        {
            if(obj.name == "FeedbackPanel")
            {
                obj.SetActive(false);
                Debug.Log("Closed FeedbackPanel!");
            }
        }
    }
}
