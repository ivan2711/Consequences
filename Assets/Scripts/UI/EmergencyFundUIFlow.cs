using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EmergencyFundUIFlow : MonoBehaviour
{
    public enum State { Tutorial, SavingTier, Event, Feedback, Final }

    [Header("Panels")]
    public GameObject tutorialPanel;
    public GameObject eventPanel;
    public GameObject feedbackPanel;
    public GameObject finalPanel;
    public GameObject hudPanel;

    [Header("Tutorial Panel")]
    public TextMeshProUGUI tutorialTitleText;
    public TextMeshProUGUI tutorialBodyText;
    public Button tutorialStartButton;

    [Header("Event Panel")]
    public TextMeshProUGUI weekText;
    public TextMeshProUGUI availableText;
    public TextMeshProUGUI fundText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventBodyText;
    public Button choiceAButton;
    public TextMeshProUGUI choiceALabel;
    public Button choiceBButton;
    public TextMeshProUGUI choiceBLabel;
    public Button choiceCButton;
    public TextMeshProUGUI choiceCLabel;

    [Header("Feedback Panel")]
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackBodyText;
    public Button continueButton;

    [Header("Final Panel")]
    public TextMeshProUGUI finalTitleText;
    public TextMeshProUGUI finalSummaryText;
    public Button finishButton;

    [Header("HUD Panel")]
    public TextMeshProUGUI bankBalanceText;
    public TextMeshProUGUI emergencyFundText;
    public Image progressBarFill;

    // Callbacks set by Controller
    [NonSerialized] public Action OnTutorialDone;
    [NonSerialized] public Action<int> OnTierChosen;
    [NonSerialized] public Action OnChoiceA;
    [NonSerialized] public Action OnChoiceB;
    [NonSerialized] public Action OnContinue;
    [NonSerialized] public Action OnFinish;

    private State currentState;
    private const string TUTORIAL_KEY = "EmergencyTutorialSeen";

    void Start()
    {
        HideAll();

        if (tutorialStartButton != null)
            tutorialStartButton.onClick.AddListener(HandleTutorialStart);

        if (continueButton != null)
            continueButton.onClick.AddListener(HandleContinue);

        if (finishButton != null)
            finishButton.onClick.AddListener(HandleFinish);

        if (choiceAButton != null)
            choiceAButton.onClick.AddListener(HandleChoiceA);

        if (choiceBButton != null)
            choiceBButton.onClick.AddListener(HandleChoiceB);

        if (choiceCButton != null)
            choiceCButton.onClick.AddListener(HandleChoiceC);
    }

    // ==================== PUBLIC API ====================

    public void ShowTutorial()
    {
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            OnTutorialDone?.Invoke();
            return;
        }

        currentState = State.Tutorial;
        HideAllPanels();
        SetActive(tutorialPanel, true);

        SetText(tutorialTitleText, "Emergency Fund", 52);
        SetText(tutorialBodyText, "Save a little each week so surprises don't turn into debt.\n\nGoal: \u00a3400", 30);
    }

    public void ShowSavingTier(int week, int available, int fund, int goal)
    {
        currentState = State.SavingTier;
        HideAllPanels();
        SetActive(eventPanel, true);

        SetText(weekText, "Week " + week + " of 6", 34);
        SetText(availableText, "Available: \u00a3" + available, 30);
        SetText(fundText, "Emergency fund: \u00a3" + fund, 30);
        SetText(goalText, "Goal: \u00a3" + goal, 28);
        SetText(eventTitleText, "How much will you save?", 42);
        SetText(eventBodyText, "Choose your saving amount for this week.", 28);

        ShowButton(choiceAButton, choiceALabel, "Strong \u00a340", true);
        ShowButton(choiceBButton, choiceBLabel, "Balanced \u00a330", true);
        ShowButton(choiceCButton, choiceCLabel, "Small \u00a320", true);
    }

    public void ShowEvent(int week, string title, string body, string choiceA, string choiceB)
    {
        currentState = State.Event;
        HideAllPanels();
        SetActive(eventPanel, true);

        SetText(weekText, "Week " + week + " of 6", 34);
        SetText(eventTitleText, title, 48);
        SetText(eventBodyText, body, 30);

        ShowButton(choiceAButton, choiceALabel, choiceA, true);
        ShowButton(choiceBButton, choiceBLabel, choiceB, choiceB != null);
        ShowButton(choiceCButton, choiceCLabel, null, false);
    }

    public void ShowFeedback(string title, string body)
    {
        currentState = State.Feedback;
        HideAllPanels();
        SetActive(feedbackPanel, true);

        if (feedbackPanel != null)
            feedbackPanel.transform.SetAsLastSibling();

        SetText(feedbackTitleText, title, 48);
        SetText(feedbackBodyText, body, 30);
    }

    public void ShowFinal(string line1, string line2, string line3)
    {
        currentState = State.Final;
        HideAllPanels();
        SetActive(finalPanel, true);

        if (finalPanel != null)
            finalPanel.transform.SetAsLastSibling();

        SetText(finalTitleText, "Season Complete!", 52);
        SetText(finalSummaryText, line1 + "\n\n" + line2 + "\n\n" + line3, 30);
    }

    public void UpdateHUD(float bankBalance, int fundBalance, int goal)
    {
        SetActive(hudPanel, true);

        if (bankBalanceText != null)
            bankBalanceText.text = "Bank: \u00a3" + bankBalance.ToString("F0");

        if (emergencyFundText != null)
            emergencyFundText.text = "Fund: \u00a3" + fundBalance;

        if (progressBarFill != null)
            progressBarFill.fillAmount = goal > 0 ? Mathf.Clamp01((float)fundBalance / goal) : 0f;
    }

    public void HideAll()
    {
        HideAllPanels();
    }

    public State GetCurrentState()
    {
        return currentState;
    }

    // ==================== BUTTON HANDLERS ====================

    void HandleTutorialStart()
    {
        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        PlayerPrefs.Save();
        SetActive(tutorialPanel, false);
        OnTutorialDone?.Invoke();
    }

    void HandleChoiceA()
    {
        if (currentState == State.SavingTier)
            OnTierChosen?.Invoke(40); // Strong
        else
            OnChoiceA?.Invoke();
    }

    void HandleChoiceB()
    {
        if (currentState == State.SavingTier)
            OnTierChosen?.Invoke(30); // Balanced
        else
            OnChoiceB?.Invoke();
    }

    void HandleChoiceC()
    {
        if (currentState == State.SavingTier)
            OnTierChosen?.Invoke(20); // Small
    }

    void HandleContinue()
    {
        OnContinue?.Invoke();
    }

    void HandleFinish()
    {
        OnFinish?.Invoke();
    }

    // ==================== HELPERS ====================

    void HideAllPanels()
    {
        SetActive(tutorialPanel, false);
        SetActive(eventPanel, false);
        SetActive(feedbackPanel, false);
        SetActive(finalPanel, false);
    }

    void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    void SetText(TextMeshProUGUI text, string value, int fontSize)
    {
        if (text == null) return;
        text.text = value;
        text.fontSize = fontSize;
    }

    void ShowButton(Button btn, TextMeshProUGUI label, string text, bool visible)
    {
        if (btn != null)
        {
            btn.gameObject.SetActive(visible);
            btn.interactable = visible;
        }
        if (label != null && text != null)
        {
            label.text = text;
            label.fontSize = 32;
        }
    }

    // For testing
    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_KEY);
        PlayerPrefs.Save();
        Debug.Log("EmergencyFundUIFlow: Tutorial reset");
    }
}
