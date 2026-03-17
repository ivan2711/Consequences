using UnityEngine;
using System.Collections;
using TMPro;

public class BankHud : MonoBehaviour
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

        BankAccountService svc = BankAccountService.Instance;

        if (svc == null)
            svc = FindObjectOfType<BankAccountService>();

        if (svc == null)
        {
            bankText.text = "Balance: —";
            return;
        }

        float balance = svc.GetBalance();
        bankText.text = $"Balance: £{balance:0.00}";
    }
}
