using UnityEngine;
using TMPro;

public class BankTransactionList : MonoBehaviour
{
    public Transform contentParent;
    public GameObject transactionItemPrefab;

    private void OnEnable()
    {
        if (BankAccountService.Instance == null)
        {
            return;
        }

        if (contentParent == null || transactionItemPrefab == null)
        {
            return;
        }

        var transactions = BankAccountService.Instance.GetRecentTransactions(10);

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        for (int i = transactions.Count - 1; i >= 0; i--)
        {
            var tx = transactions[i];
            GameObject item = Instantiate(transactionItemPrefab, contentParent);

            TextMeshProUGUI descText = FindChildText(item.transform, "DescText");
            TextMeshProUGUI amountText = FindChildText(item.transform, "AmountText");
            TextMeshProUGUI timeText = FindChildText(item.transform, "TimeText");

            if (descText != null)
            {
                descText.text = tx.description;
            }

            if (amountText != null)
            {
                float abs = Mathf.Abs(tx.amountPounds);

                if (tx.amountPounds < 0)
                {
                    amountText.text = string.Format("-£{0:0.00}", abs);
                    amountText.color = new Color(0.9f, 0.3f, 0.3f);
                }
                else
                {
                    amountText.text = string.Format("+£{0:0.00}", abs);
                    amountText.color = new Color(0.3f, 0.8f, 0.3f);
                }
            }

            if (timeText != null)
            {
                timeText.text = tx.timestamp.ToString("HH:mm");
            }
        }
    }

    private TextMeshProUGUI FindChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child == null)
        {
            return null;
        }

        return child.GetComponent<TextMeshProUGUI>();
    }
}
