using UnityEngine;
using UnityEngine.UI;

public class PlayAgainButton : MonoBehaviour
{
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // Find the SpendingGameController
            SpendingGameController controller = FindObjectOfType<SpendingGameController>();
            if (controller != null)
            {
                button.onClick.AddListener(() => {
                    controller.ResetGame();
                    Debug.Log("PlayAgainButton: Reset game!");
                });
                Debug.Log("PlayAgainButton: Wired up to SpendingGameController.ResetGame()");
            }
            else
            {
                Debug.LogError("PlayAgainButton: Could not find SpendingGameController!");
            }
        }
        else
        {
            Debug.LogError("PlayAgainButton: No Button component found!");
        }
    }
}