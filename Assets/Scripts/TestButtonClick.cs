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
    }

    void OnButtonClicked()
    {
        Debug.Log("[TestButtonClick] Check Out clicked!");

        // Let the controller handle everything (panel activation, button wiring, etc.)
        SpendingGameController controller = FindObjectOfType<SpendingGameController>();
        if (controller != null)
        {
            controller.OnContinue();
        }
        else
        {
            Debug.LogError("TestButtonClick: SpendingGameController not found!");
        }
    }
}
