using System;
using System.Collections.Generic;
using UnityEngine;

public class BankAccountService : MonoBehaviour
{
    private static BankAccountService _instance;

    public static BankAccountService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BankAccountService>();
                if (_instance == null)
                {
                    var go = new GameObject("Services");
                    _instance = go.AddComponent<BankAccountService>();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class Transaction
    {
        public string description;
        public float amountPounds;
        public DateTime timestamp;
        public string category;

        public Transaction(string description, float amountPounds, DateTime timestamp, string category)
        {
            this.description = description;
            this.amountPounds = amountPounds;
            this.timestamp = timestamp;
            this.category = category;
        }
    }

    private float balancePounds = 500f;
    private List<Transaction> transactions = new List<Transaction>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (PlayerPrefs.HasKey("BankBalance"))
        {
            balancePounds = PlayerPrefs.GetFloat("BankBalance");
        }
        else
        {
            balancePounds = 500f;
        }
    }

    public float GetBalance()
    {
        return balancePounds;
    }

    public bool Spend(float amountPounds, string description, string category)
    {
        if (amountPounds <= 0f || amountPounds > balancePounds)
        {
            return false;
        }

        balancePounds -= amountPounds;

        transactions.Add(new Transaction(description, -amountPounds, DateTime.Now, category));

        PlayerPrefs.SetFloat("BankBalance", balancePounds);
        PlayerPrefs.Save();

        return true;
    }

    public void Earn(float amountPounds, string description)
    {
        if (amountPounds <= 0f)
        {
            return;
        }

        balancePounds += amountPounds;

        transactions.Add(new Transaction(description, amountPounds, DateTime.Now, "income"));

        PlayerPrefs.SetFloat("BankBalance", balancePounds);
        PlayerPrefs.Save();
    }

    public List<Transaction> GetRecentTransactions(int count)
    {
        if (count <= 0)
        {
            return new List<Transaction>();
        }

        int start = Mathf.Max(0, transactions.Count - count);
        return transactions.GetRange(start, transactions.Count - start);
    }
}
