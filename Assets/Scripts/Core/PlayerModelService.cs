using UnityEngine;

public class PlayerModelService : MonoBehaviour
{
    private static PlayerModelService _instance;

    public static PlayerModelService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerModelService>();
                if (_instance == null)
                {
                    var go = new GameObject("PlayerModelService");
                    _instance = go.AddComponent<PlayerModelService>();
                }
            }
            return _instance;
        }
    }

    public enum EngagementState
    {
        OK,
        Frustrated,
        Bored
    }

    public int overspendCount;
    public float treatRatioAvg;
    public int inactivityCount;
    public int failedRoundsStreak;
    public int successStreak;

    private int _treatRoundCount;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void RecordSpendingRound(float totalSpent, float budget, int treatsChosen, int totalItems)
    {
        bool overBudget = totalSpent > budget;

        if (overBudget)
        {
            overspendCount++;
            failedRoundsStreak++;
            successStreak = 0;
        }
        else
        {
            failedRoundsStreak = 0;
            successStreak++;
        }

        float treatRatio = (totalItems > 0) ? (float)treatsChosen / totalItems : 0f;
        _treatRoundCount++;
        treatRatioAvg += (treatRatio - treatRatioAvg) / _treatRoundCount;

        inactivityCount = 0;

        Save();
        Debug.Log($"[PlayerModel] Spending | state={GetEngagementState()} overspends={overspendCount} treatAvg={treatRatioAvg:F2} streak={failedRoundsStreak}F/{successStreak}S");
    }

    public void RecordEmergencyFundRound(int amountSaved, int goalAmount)
    {
        bool reachedGoal = amountSaved >= goalAmount;

        if (reachedGoal)
        {
            failedRoundsStreak = 0;
            successStreak++;
        }
        else if (amountSaved < goalAmount / 2)
        {
            failedRoundsStreak++;
            successStreak = 0;
        }

        inactivityCount = 0;

        Save();
        Debug.Log($"[PlayerModel] EmergencyFund | state={GetEngagementState()} saved={amountSaved}/{goalAmount} streak={failedRoundsStreak}F/{successStreak}S");
    }

    public void RecordInactivity()
    {
        inactivityCount++;
        Save();
        Debug.Log($"[PlayerModel] Inactivity | state={GetEngagementState()} idleCount={inactivityCount} overspends={overspendCount}");
    }

    public EngagementState GetEngagementState()
    {
        if (failedRoundsStreak >= 3 || overspendCount >= 5)
        {
            return EngagementState.Frustrated;
        }

        if (inactivityCount >= 3)
        {
            return EngagementState.Bored;
        }

        return EngagementState.OK;
    }

    public void ResetAll()
    {
        overspendCount = 0;
        treatRatioAvg = 0f;
        inactivityCount = 0;
        failedRoundsStreak = 0;
        successStreak = 0;
        _treatRoundCount = 0;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt("PM_OverspendCount", overspendCount);
        PlayerPrefs.SetFloat("PM_TreatRatioAvg", treatRatioAvg);
        PlayerPrefs.SetInt("PM_InactivityCount", inactivityCount);
        PlayerPrefs.SetInt("PM_FailedStreak", failedRoundsStreak);
        PlayerPrefs.SetInt("PM_SuccessStreak", successStreak);
        PlayerPrefs.SetInt("PM_TreatRoundCount", _treatRoundCount);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        overspendCount = PlayerPrefs.GetInt("PM_OverspendCount", 0);
        treatRatioAvg = PlayerPrefs.GetFloat("PM_TreatRatioAvg", 0f);
        inactivityCount = PlayerPrefs.GetInt("PM_InactivityCount", 0);
        failedRoundsStreak = PlayerPrefs.GetInt("PM_FailedStreak", 0);
        successStreak = PlayerPrefs.GetInt("PM_SuccessStreak", 0);
        _treatRoundCount = PlayerPrefs.GetInt("PM_TreatRoundCount", 0);
    }
}
