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
    public RectTransform progressBarFill;

    [Header("Progress Panel (above whiteboard)")]
    public GameObject progressPanel;
    public TextMeshProUGUI progressText;
    public RectTransform progressPanelBarFill;

    // Callbacks set by Controller
    [NonSerialized] public Action OnTutorialDone;
    [NonSerialized] public Action<int> OnTierChosen;
    [NonSerialized] public Action OnChoiceA;
    [NonSerialized] public Action OnChoiceB;
    [NonSerialized] public Action OnContinue;
    [NonSerialized] public Action OnFinish;

    private State currentState;
    private const string TUTORIAL_KEY = "EmergencyTutorialSeen";

    void Awake()
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
        Debug.Log("[UIFlow] ShowTutorial called. Key=" + PlayerPrefs.GetInt(TUTORIAL_KEY, 0)
            + " tutorialPanel=" + (tutorialPanel != null ? "OK" : "NULL"));

        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            Debug.Log("[UIFlow] Tutorial already seen, skipping.");
            OnTutorialDone?.Invoke();
            return;
        }

        currentState = State.Tutorial;
        HideAllPanels();
        SetActive(tutorialPanel, true);

        Debug.Log("[UIFlow] Tutorial panel active=" + (tutorialPanel != null && tutorialPanel.activeSelf));

        SetText(tutorialTitleText, "Emergency Fund", 60);
        SetText(tutorialBodyText, "Save a little each week so surprises don't turn into debt.\n\nGoal: \u00a3160", 38);
    }

    public void ShowSavingTier(int week, int available, int fund, int goal)
    {
        currentState = State.SavingTier;
        HideAllPanels();
        SetActive(eventPanel, true);

        SetText(weekText, "Week " + week + " of 6", 40);
        SetText(eventTitleText, "How much will you save?", 52);
        SetText(eventBodyText, "Choose your saving amount for this week.", 36);
        HideText(availableText);
        HideText(goalText);

        ShowButton(choiceAButton, choiceALabel, "Strong \u00a340", true);
        ShowButton(choiceBButton, choiceBLabel, "Balanced \u00a330", true);
        ShowButton(choiceCButton, choiceCLabel, "Small \u00a320", true);

        LayoutButtons(true, true, true);
    }

    public void ShowEvent(int week, string title, string body, string choiceA, string choiceB)
    {
        currentState = State.Event;
        HideAllPanels();
        SetActive(eventPanel, true);

        SetText(weekText, "Week " + week + " of 6", 40);
        SetText(eventTitleText, title, 56);
        SetText(eventBodyText, body, 38);
        HideText(availableText);
        HideText(goalText);

        ShowButton(choiceAButton, choiceALabel, choiceA, true);
        ShowButton(choiceBButton, choiceBLabel, choiceB, choiceB != null);
        ShowButton(choiceCButton, choiceCLabel, null, false);

        LayoutButtons(choiceA != null, choiceB != null, false);
    }

    public void ShowFeedback(string title, string body)
    {
        currentState = State.Feedback;
        HideAllPanels();
        SetActive(feedbackPanel, true);

        if (feedbackPanel != null)
            feedbackPanel.transform.SetAsLastSibling();

        SetText(feedbackTitleText, title, 56);
        SetText(feedbackBodyText, body, 38);
    }

    public void ShowFinal(string line1, string line2, string line3)
    {
        currentState = State.Final;
        HideAllPanels();
        SetActive(finalPanel, true);

        if (finalPanel != null)
            finalPanel.transform.SetAsLastSibling();

        SetText(finalTitleText, "Season Complete!", 60);
        SetText(finalSummaryText, line1 + "\n\n" + line2 + "\n\n" + line3, 38);
    }

    public void UpdateHUD(float bankBalance, int fundBalance, int goal)
    {
        SetActive(hudPanel, false);

        // Update bank balance overlay
        if (bankBalanceText != null)
            bankBalanceText.text = "Balance: \u00a3" + bankBalance.ToString("F0");

        // Update fund text on event panel
        if (fundText != null)
            fundText.text = "Fund: \u00a3" + fundBalance;

        float fill = goal > 0 ? Mathf.Clamp01((float)fundBalance / goal) : 0f;

        // Progress panel above whiteboard
        SetActive(progressPanel, true);

        if (progressText != null)
            progressText.text = "\u00a3" + fundBalance + " / \u00a3" + goal;

        if (progressPanelBarFill != null)
            progressPanelBarFill.anchorMax = new Vector2(fill, 1f);
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
        text.gameObject.SetActive(true);
    }

    void HideText(TextMeshProUGUI text)
    {
        if (text != null) text.gameObject.SetActive(false);
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
            label.fontSize = 40;
        }
    }

    // Position visible buttons in large touch zones: bottom-left, bottom-center, bottom-right
    void LayoutButtons(bool showA, bool showB, bool showC)
    {
        int count = (showA ? 1 : 0) + (showB ? 1 : 0) + (showC ? 1 : 0);
        if (count == 0) return;

        float yMin = 0.02f;
        float yMax = 0.16f;

        if (count == 3)
        {
            SetButtonAnchors(choiceAButton, 0.02f, yMin, 0.30f, yMax);   // bottom-left
            SetButtonAnchors(choiceBButton, 0.35f, yMin, 0.65f, yMax);   // bottom-center
            SetButtonAnchors(choiceCButton, 0.70f, yMin, 0.98f, yMax);   // bottom-right
        }
        else if (count == 2)
        {
            SetButtonAnchors(choiceAButton, 0.02f, yMin, 0.42f, yMax);   // bottom-left
            SetButtonAnchors(choiceBButton, 0.58f, yMin, 0.98f, yMax);   // bottom-right
        }
        else
        {
            SetButtonAnchors(choiceAButton, 0.15f, yMin, 0.85f, yMax);   // bottom-center
        }
    }

    void SetButtonAnchors(Button btn, float xMin, float yMin, float xMax, float yMax)
    {
        if (btn == null) return;
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
