using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public Toggle calmModeToggle;
    public string backSceneName = "Home";

    void Start()
    {
        if (calmModeToggle != null)
            calmModeToggle.isOn = GameSettings.CalmMode;
    }

    // Hook this to the Toggle's OnValueChanged(bool)
    public void OnCalmModeChanged(bool isOn)
    {
        GameSettings.CalmMode = isOn;
    }

    public void Back()
    {
        SceneManager.LoadScene(backSceneName);
    }
}