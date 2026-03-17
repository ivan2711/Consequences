using UnityEngine;
using System.Collections;
using TMPro;

public class EmergencyBankHud : MonoBehaviour
{
    public TextMeshProUGUI bankText;

    private void Start()
    {
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (bankText == null)
            return;

        int balance = PlayerPrefs.GetInt("EmergencyFundBalance", 0);
        bankText.text = $"Emergency:\n£{balance}";
    }
}
