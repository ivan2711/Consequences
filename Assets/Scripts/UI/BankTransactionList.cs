using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BankTransactionList : MonoBehaviour
{
    public Transform contentParent;
    public GameObject transactionItemPrefab;

    private void OnEnable()
    {
        if (BankAccountService.Instance == null)
            return;

        if (contentParent == null || transactionItemPrefab == null)
            return;

        var transactions = BankAccountService.Instance.GetRecentTransactions(20);
        Debug.Log("[BankTxList] Found " + transactions.Count + " transactions. BankAccountService=" + (BankAccountService.Instance != null));
        var spendingTx = BankAccountService.Instance.GetRecentTransactions(100);
        var emergTx = BankAccountService.Instance.GetRecentEmergencyTransactions(100);
        Debug.Log("[BankTxList] Spending txs: " + (spendingTx != null ? spendingTx.Count.ToString() : "null") + ", Emergency txs: " + (emergTx != null ? emergTx.Count.ToString() : "null"));

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        if (transactions.Count == 0)
        {
            // Parent the message to the panel itself, not the scroll content
            Transform panel = contentParent.parent != null ? contentParent.parent : contentParent;
            // Go up until we find the "Recent Transactions" panel
            if (panel.parent != null) panel = panel.parent;

            var emptyGO = new GameObject("EmptyMessage");
            emptyGO.transform.SetParent(panel, false);
            var rt = emptyGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.1f);
            rt.anchorMax = new Vector2(0.95f, 0.75f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = emptyGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "No transactions yet.\nPlay a game to see activity here!";
            tmp.color = new Color(0.7f, 0.7f, 0.75f);
            tmp.fontSize = 40;
            tmp.fontStyle = FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            return;
        }

        for (int i = transactions.Count - 1; i >= 0; i--)
        {
            var tx = transactions[i];
            GameObject item = Instantiate(transactionItemPrefab, contentParent);

            // Alternating row background
            Image rowBg = item.GetComponent<Image>();
            if (rowBg != null)
            {
                bool even = (transactions.Count - 1 - i) % 2 == 0;
                rowBg.color = even
                    ? new Color(1f, 1f, 1f, 0.08f)
                    : new Color(1f, 1f, 1f, 0.04f);
            }

            TextMeshProUGUI descText = FindChildText(item.transform, "DescText");
            TextMeshProUGUI amountText = FindChildText(item.transform, "AmountText");
            TextMeshProUGUI timeText = FindChildText(item.transform, "TimeText");

            if (descText != null)
            {
                bool isFundTransfer = tx.category == "Emergency" && tx.amountPounds < 0;
                descText.text = isFundTransfer
                    ? "Saved → Emergency Fund"
                    : tx.description;
                descText.color = isFundTransfer
                    ? new Color(0.4f, 0.7f, 1f) // blue to match amount
                    : new Color(0.9f, 0.9f, 0.95f);
                descText.fontSize = 22;
            }

            if (amountText != null)
            {
                float abs = Mathf.Abs(tx.amountPounds);
                bool isFundTransfer = tx.category == "Emergency" && tx.amountPounds < 0;

                if (isFundTransfer)
                {
                    amountText.text = string.Format("\u00a3{0:0.00}", abs);
                    amountText.color = new Color(0.4f, 0.7f, 1f); // blue
                }
                else if (tx.amountPounds < 0)
                {
                    amountText.text = string.Format("-\u00a3{0:0.00}", abs);
                    amountText.color = new Color(1f, 0.45f, 0.4f);
                }
                else
                {
                    amountText.text = string.Format("+\u00a3{0:0.00}", abs);
                    amountText.color = new Color(0.4f, 0.9f, 0.5f);
                }
                amountText.fontSize = 22;
                amountText.fontStyle = FontStyles.Bold;
            }

            if (timeText != null)
            {
                timeText.text = tx.timestamp.ToString("HH:mm");
                timeText.color = new Color(0.6f, 0.6f, 0.65f);
                timeText.fontSize = 18;
            }
        }
    }

    private TextMeshProUGUI FindChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            return null;
        return child.GetComponent<TextMeshProUGUI>();
    }
}
