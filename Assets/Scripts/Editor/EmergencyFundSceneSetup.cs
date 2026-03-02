using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class EmergencyFundSceneSetup : EditorWindow
{
    private static Sprite _roundedSprite;
    private static Sprite RoundedSprite
    {
        get
        {
            if (_roundedSprite == null)
                _roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RoundedRect.png");
            return _roundedSprite;
        }
    }

    [MenuItem("Tools/Setup Emergency Fund Scene")]
    static void Setup()
    {
        // ==================== CLEAN UP OLD SCENE ====================
        CleanupOldObjects();

        // Find the ROOT canvas (not an override-sorting child canvas)
        Canvas canvas = null;
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            if (c.isRootCanvas)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        Transform root = canvas.transform;

        // Find existing controller or create one
        EmergencyFundController controller = FindObjectOfType<EmergencyFundController>();
        if (controller == null)
        {
            GameObject controllerGO = new GameObject("EmergencyFundController");
            controller = controllerGO.AddComponent<EmergencyFundController>();
        }

        // Find existing UIFlow or create one
        EmergencyFundUIFlow uiFlow = FindObjectOfType<EmergencyFundUIFlow>();
        if (uiFlow == null)
        {
            uiFlow = controller.gameObject.AddComponent<EmergencyFundUIFlow>();
        }

        // Wire controller → uiFlow
        controller.uiFlow = uiFlow;
        controller.emergencyFundGoal = 160;

        // Wire duck/star if they exist in scene
        DuckReaction duck = FindObjectOfType<DuckReaction>();
        if (duck != null) controller.duckReaction = duck;

        DuckReactionBackgroundChanger bgChanger = FindObjectOfType<DuckReactionBackgroundChanger>();
        if (bgChanger != null) controller.backgroundChanger = bgChanger;

        StarRating stars = FindObjectOfType<StarRating>();
        if (stars != null) controller.starRating = stars;

        // ==================== HUD PANEL ====================
        GameObject hudPanel = FindOrCreate(root, "HUDPanel");
        SetAnchors(hudPanel, new Vector2(0, 0.9f), Vector2.one, Color.clear);
        uiFlow.hudPanel = hudPanel;

        uiFlow.bankBalanceText = FindOrCreateTMP(hudPanel.transform, "BankBalanceText", "Bank: \u00a3500", 28,
            new Vector2(0, 0), new Vector2(0.33f, 1));
        uiFlow.emergencyFundText = FindOrCreateTMP(hudPanel.transform, "EmergencyFundText", "Fund: \u00a30", 28,
            new Vector2(0.33f, 0), new Vector2(0.66f, 1));

        // Progress bar
        GameObject progressBG = FindOrCreate(hudPanel.transform, "ProgressBarBG");
        SetAnchors(progressBG, new Vector2(0.68f, 0.2f), new Vector2(0.98f, 0.8f), new Color(0.2f, 0.2f, 0.2f));

        GameObject progressFill = FindOrCreate(progressBG.transform, "ProgressBarFill");
        SetAnchors(progressFill, Vector2.zero, new Vector2(0f, 1f), new Color(0.2f, 0.8f, 0.3f));
        uiFlow.progressBarFill = progressFill.GetComponent<RectTransform>();

        // ==================== PROGRESS PANEL (above whiteboard) ====================
        GameObject progressPanel = FindOrCreate(root, "ProgressPanel");
        SetAnchors(progressPanel, new Vector2(0.30f, 0.80f), new Vector2(0.70f, 0.90f), new Color(0.95f, 0.95f, 0.95f, 0.9f));
        uiFlow.progressPanel = progressPanel;

        // Progress text: "£0 / £160"
        uiFlow.progressText = FindOrCreateTMP(progressPanel.transform, "ProgressText", "\u00a30 / \u00a3400", 38,
            new Vector2(0.05f, 0.0f), new Vector2(0.95f, 1.0f));
        uiFlow.progressText.alignment = TextAlignmentOptions.Center;
        uiFlow.progressText.color = new Color(0.15f, 0.15f, 0.15f);
        uiFlow.progressText.fontStyle = FontStyles.Bold;

        // Progress bar background (behind text)
        GameObject progBarBG = FindOrCreate(progressPanel.transform, "ProgressBarBG");
        SetAnchors(progBarBG, new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.35f), new Color(0.8f, 0.8f, 0.8f));

        GameObject progBarFill = FindOrCreate(progBarBG.transform, "ProgressBarFill");
        SetAnchors(progBarFill, Vector2.zero, new Vector2(0f, 1f), new Color(0.2f, 0.75f, 0.3f));
        uiFlow.progressPanelBarFill = progBarFill.GetComponent<RectTransform>();

        // ==================== WHITEBOARD AREA ====================
        // All content panels sit inside the whiteboard image on the background.
        // Whiteboard anchors (from screenshot): ~(0.19, 0.28) to (0.68, 0.86)
        // Panels are transparent so the whiteboard shows through.
        Vector2 boardMin = new Vector2(0.25f, 0.18f);
        Vector2 boardMax = new Vector2(0.74f, 0.86f);
        Color dark = new Color(0.15f, 0.15f, 0.15f);       // dark text on white board
        Color gray = new Color(0.45f, 0.45f, 0.45f);       // secondary text

        // ==================== TUTORIAL PANEL ====================
        GameObject tutorialPanel = FindOrCreate(root, "TutorialPanel");
        SetAnchors(tutorialPanel, boardMin, boardMax, Color.clear);
        uiFlow.tutorialPanel = tutorialPanel;

        uiFlow.tutorialTitleText = FindOrCreateTMP(tutorialPanel.transform, "TitleText", "Emergency Fund", 44,
            new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.95f));
        uiFlow.tutorialTitleText.fontStyle = FontStyles.Bold;
        uiFlow.tutorialTitleText.alignment = TextAlignmentOptions.Center;
        uiFlow.tutorialTitleText.color = dark;

        uiFlow.tutorialBodyText = FindOrCreateTMP(tutorialPanel.transform, "BodyText",
            "Save a little each week so surprises don't turn into debt.\n\nGoal: \u00a3400", 26,
            new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.68f));
        uiFlow.tutorialBodyText.alignment = TextAlignmentOptions.Center;
        uiFlow.tutorialBodyText.color = dark;

        uiFlow.tutorialStartButton = FindOrCreateButton(tutorialPanel.transform, "StartButton", "Start",
            new Vector2(0.10f, -0.40f), new Vector2(0.90f, -0.05f), new Color(0.2f, 0.65f, 0.3f));

        // ==================== EVENT PANEL (full screen for button reach) ====================
        GameObject eventPanel = FindOrCreate(root, "EventPanel");
        SetAnchors(eventPanel, Vector2.zero, Vector2.one, Color.clear);
        uiFlow.eventPanel = eventPanel;

        // Text stays in whiteboard area (screen coords 0.27–0.72 X, same Y as before)
        uiFlow.weekText = FindOrCreateTMP(eventPanel.transform, "WeekText", "Week 1 of 6", 28,
            new Vector2(0.27f, 0.72f), new Vector2(0.72f, 0.78f));
        uiFlow.weekText.fontStyle = FontStyles.Bold;
        uiFlow.weekText.alignment = TextAlignmentOptions.Center;
        uiFlow.weekText.color = dark;

        uiFlow.availableText = FindOrCreateTMP(eventPanel.transform, "AvailableText", "", 22,
            new Vector2(0.27f, 0.67f), new Vector2(0.50f, 0.72f));
        uiFlow.availableText.color = dark;

        uiFlow.fundText = FindOrCreateTMP(eventPanel.transform, "FundText", "Fund: \u00a30", 36,
            new Vector2(0.27f, 0.67f), new Vector2(0.72f, 0.72f));
        uiFlow.fundText.alignment = TextAlignmentOptions.Center;
        uiFlow.fundText.color = dark;

        uiFlow.goalText = FindOrCreateTMP(eventPanel.transform, "GoalText", "", 20,
            new Vector2(0.27f, 0.63f), new Vector2(0.72f, 0.67f));
        uiFlow.goalText.alignment = TextAlignmentOptions.Center;
        uiFlow.goalText.color = gray;

        uiFlow.eventTitleText = FindOrCreateTMP(eventPanel.transform, "EventTitleText", "How much will you save?", 48,
            new Vector2(0.27f, 0.57f), new Vector2(0.72f, 0.69f));
        uiFlow.eventTitleText.fontStyle = FontStyles.Bold;
        uiFlow.eventTitleText.alignment = TextAlignmentOptions.Center;
        uiFlow.eventTitleText.color = dark;

        uiFlow.eventBodyText = FindOrCreateTMP(eventPanel.transform, "EventBodyText", "Choose your saving amount.", 38,
            new Vector2(0.28f, 0.42f), new Vector2(0.71f, 0.56f));
        uiFlow.eventBodyText.alignment = TextAlignmentOptions.Center;
        uiFlow.eventBodyText.color = dark;

        // Choice buttons — large touch zones at screen bottom
        uiFlow.choiceAButton = FindOrCreateButton(eventPanel.transform, "ChoiceAButton", "Strong \u00a340",
            new Vector2(0.02f, 0.02f), new Vector2(0.30f, 0.15f), new Color(0.2f, 0.65f, 0.3f));
        uiFlow.choiceALabel = uiFlow.choiceAButton.GetComponentInChildren<TextMeshProUGUI>();

        uiFlow.choiceBButton = FindOrCreateButton(eventPanel.transform, "ChoiceBButton", "Balanced \u00a330",
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.15f), new Color(0.4f, 0.58f, 0.48f));
        uiFlow.choiceBLabel = uiFlow.choiceBButton.GetComponentInChildren<TextMeshProUGUI>();

        uiFlow.choiceCButton = FindOrCreateButton(eventPanel.transform, "ChoiceCButton", "Small \u00a320",
            new Vector2(0.70f, 0.02f), new Vector2(0.98f, 0.15f), new Color(0.45f, 0.5f, 0.7f));
        uiFlow.choiceCLabel = uiFlow.choiceCButton.GetComponentInChildren<TextMeshProUGUI>();

        // ==================== FEEDBACK PANEL ====================
        GameObject feedbackPanel = FindOrCreate(root, "FeedbackPanel");
        SetAnchors(feedbackPanel, boardMin, boardMax, Color.clear);
        uiFlow.feedbackPanel = feedbackPanel;

        uiFlow.feedbackTitleText = FindOrCreateTMP(feedbackPanel.transform, "FeedbackTitleText", "Week Complete", 36,
            new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.92f));
        uiFlow.feedbackTitleText.fontStyle = FontStyles.Bold;
        uiFlow.feedbackTitleText.alignment = TextAlignmentOptions.Center;
        uiFlow.feedbackTitleText.color = dark;

        uiFlow.feedbackBodyText = FindOrCreateTMP(feedbackPanel.transform, "FeedbackBodyText", "You saved this week.", 24,
            new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.62f));
        uiFlow.feedbackBodyText.alignment = TextAlignmentOptions.Center;
        uiFlow.feedbackBodyText.color = dark;

        uiFlow.continueButton = FindOrCreateButton(feedbackPanel.transform, "ContinueButton", "Continue",
            new Vector2(0.10f, -0.40f), new Vector2(0.90f, -0.05f), new Color(0.3f, 0.5f, 0.75f));

        // ==================== FINAL PANEL ====================
        GameObject finalPanel = FindOrCreate(root, "FinalConsequencesPanel");
        SetAnchors(finalPanel, boardMin, boardMax, Color.clear);
        uiFlow.finalPanel = finalPanel;

        uiFlow.finalTitleText = FindOrCreateTMP(finalPanel.transform, "TitleText", "Season Complete!", 36,
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
        uiFlow.finalTitleText.fontStyle = FontStyles.Bold;
        uiFlow.finalTitleText.alignment = TextAlignmentOptions.Center;
        uiFlow.finalTitleText.color = dark;

        uiFlow.finalSummaryText = FindOrCreateTMP(finalPanel.transform, "SummaryText", "Summary here", 22,
            new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.75f));
        uiFlow.finalSummaryText.alignment = TextAlignmentOptions.Center;
        uiFlow.finalSummaryText.color = dark;

        uiFlow.finishButton = FindOrCreateButton(finalPanel.transform, "FinishButton", "Play Again",
            new Vector2(0.10f, -0.40f), new Vector2(0.90f, -0.05f), new Color(0.75f, 0.55f, 0.2f));


        // ==================== DEACTIVATE PANELS ====================
        tutorialPanel.SetActive(false);
        eventPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        finalPanel.SetActive(false);

        // Mark dirty
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(uiFlow);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Emergency Fund scene setup complete! Save the scene (Ctrl+S).");
    }

    // ==================== CLEANUP OLD SCENE ====================

    static void CleanupOldObjects()
    {
        // Remove old EmergencyFundTutorial components
        var oldTutorials = FindObjectsOfType<EmergencyFundTutorial>();
        foreach (var t in oldTutorials)
        {
            // Disable the tutorial container if it has one
            if (t.tutorialContainer != null)
                t.tutorialContainer.SetActive(false);
            Debug.Log("Disabled old EmergencyFundTutorial on: " + t.gameObject.name);
            DestroyImmediate(t);
        }

        // Remove old EmergencyFundConsequencePanel components (in FinancialLiteracy.UI namespace)
        var oldConsequences = FindObjectsOfType<FinancialLiteracy.UI.EmergencyFundConsequencePanel>();
        foreach (var c in oldConsequences)
        {
            if (c.panelContainer != null)
                c.panelContainer.SetActive(false);
            Debug.Log("Disabled old EmergencyFundConsequencePanel on: " + c.gameObject.name);
            DestroyImmediate(c);
        }

        // Find and disable common old panel names in any Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        string[] oldPanelNames = new string[] {
            "TutorialContainer", "TutorialPanel1", "TutorialPanel2", "TutorialPanel3",
            "ConsequenceContainer", "BridgePanel",
            "Panel1", "Panel2", "Panel3",
            "ChoicesPanel"
        };

        foreach (var canvas in canvases)
        {
            foreach (string name in oldPanelNames)
            {
                Transform found = canvas.transform.Find(name);
                if (found != null)
                {
                    found.gameObject.SetActive(false);
                    Debug.Log("Disabled old panel: " + name);
                }
            }
        }

        // Clear old serialized references on EmergencyFundController
        var controller = FindObjectOfType<EmergencyFundController>();
        if (controller != null)
        {
            // Use SerializedObject to clear any old fields that no longer exist
            var so = new SerializedObject(controller);
            // Clear fields that used to exist but are now removed
            string[] oldFields = new string[] {
                "progressBarFill", "progressText", "eventText", "roundCounterText",
                "choicesPanel", "consequencePanel"
            };
            foreach (string field in oldFields)
            {
                var prop = so.FindProperty(field);
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    prop.objectReferenceValue = null;
                    Debug.Log("Cleared old field: " + field);
                }
            }
            so.ApplyModifiedProperties();
        }

        Debug.Log("Old scene cleanup complete.");
    }

    // ==================== HELPERS ====================

    static GameObject FindOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = (bgColor.a > 0.5f);

        // Apply rounded corners (skip transparent containers and progress bars)
        string nameLower = go.name.ToLower();
        bool isBar = nameLower.Contains("bar") || nameLower.Contains("fill") || nameLower.Contains("progress");
        if (bgColor.a > 0.01f && !isBar && RoundedSprite != null)
        {
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
        }
    }

    static TextMeshProUGUI FindOrCreateTMP(Transform parent, string name, string defaultText, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        return tmp;
    }

    static Button FindOrCreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        // Rounded corners for buttons
        if (RoundedSprite != null)
        {
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
        }

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();

        // Label text
        Transform labelT = go.transform.Find("Label");
        GameObject labelGO;
        if (labelT != null)
            labelGO = labelT.gameObject;
        else
        {
            labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
        }

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        if (labelRect == null) labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.raycastTarget = false;

        return btn;
    }
}
