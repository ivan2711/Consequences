using UnityEngine;
using TMPro;

public class BankHud : MonoBehaviour
{
    public TextMeshProUGUI bankText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (bankText == null)
        {
            return;
        }

        if (BankAccountService.Instance == null)
        {
            bankText.text = "Bank: —";
            return;
        }

        float balance = BankAccountService.Instance.GetBalance();
        bankText.text = $"Bank: £{balance:0.00}";
    }
}
