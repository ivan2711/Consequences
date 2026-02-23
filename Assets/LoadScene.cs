using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadGameChoice()
    {
        SceneManager.LoadScene("GameChoice");
    }

    public void LoadSpending()
    {
        SceneManager.LoadScene("Spending");
    }

    public void LoadProgress()
    {
        SceneManager.LoadScene("Progress");
    }
    public void LoadMonthlyChoices()
    {
        SceneManager.LoadScene("Monthly Choices");
    }

    public void LoadHome()
    {
        SceneManager.LoadScene("Home");
    }

        public void LoadSettings()
    {
        SceneManager.LoadScene("Settings");
    }
}
