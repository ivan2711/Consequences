using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class BackButton : MonoBehaviour
{
    public string targetScene = "Home";
    public bool showConfirmation = false;

    private GameObject _overlay;

    private void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(GoBack);
        }
    }

    private void LateUpdate()
    {
        transform.SetAsLastSibling();
    }

    public void GoBack()
    {
        if (showConfirmation)
        {
            ShowConfirmDialog();
            return;
        }
        SceneManager.LoadScene(targetScene);
    }

    private void ShowConfirmDialog()
    {
        if (_overlay != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.gameObject.scene == gameObject.scene)
                {
                    canvas = c;
                    break;
                }
            }
        }
        if (canvas == null)
        {
            SceneManager.LoadScene(targetScene);
            return;
        }

        // Dark overlay
        _overlay = new GameObject("BackConfirmOverlay");
        _overlay.transform.SetParent(canvas.transform, false);
        var overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        var overlayImg = _overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);

        // Ensure overlay renders above everything
        var overlayCanvas = _overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 20;
        _overlay.AddComponent<GraphicRaycaster>();

        // Dialog panel
        var panel = new GameObject("DialogPanel");
        panel.transform.SetParent(_overlay.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 250);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(1f, 1f, 1f, 0.95f);

        // Message text
        var textObj = new GameObject("Message");
        textObj.transform.SetParent(panel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.45f);
        textRect.anchorMax = new Vector2(1, 0.95f);
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, 0);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Leave game?\nYour progress will be lost.";
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        // Leave button
        CreateDialogButton(panel.transform, "LeaveBtn", "Leave", new Vector2(-90, -80), new Color(0.85f, 0.25f, 0.25f), () =>
        {
            Destroy(_overlay);
            _overlay = null;
            SceneManager.LoadScene(targetScene);
        });

        // Stay button
        CreateDialogButton(panel.transform, "StayBtn", "Stay", new Vector2(90, -80), new Color(0.3f, 0.7f, 0.35f), () =>
        {
            Destroy(_overlay);
            _overlay = null;
        });
    }

    private void CreateDialogButton(Transform parent, string name, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(150, 55);
        btnRect.anchoredPosition = position;
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = color;
        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var btnText = new GameObject("Text");
        btnText.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnText.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        var btnTmp = btnText.AddComponent<TextMeshProUGUI>();
        btnTmp.text = label;
        btnTmp.fontSize = 26;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
    }
}
