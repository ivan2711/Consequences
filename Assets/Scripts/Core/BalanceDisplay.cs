using UnityEngine;
using TMPro;

public class BalanceDisplay : MonoBehaviour
{
    public TextMeshProUGUI balanceText;

    private void Update()
    {
        if (balanceText == null)
        {
            return;
        }

        if (BankAccountService.Instance == null)
        {
            return;
        }

        balanceText.text = string.Format("Balance: £{0:F2}", BankAccountService.Instance.GetBalance());
    }
}
