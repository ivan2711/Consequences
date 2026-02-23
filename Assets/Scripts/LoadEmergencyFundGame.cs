using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadEmergencyFundGame : MonoBehaviour
{
    public void LoadScene()
    {
        Debug.Log("Loading Emergency Fund scene");
        SceneManager.LoadScene("EmergencyFund");
    }
}