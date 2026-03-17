using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class EndGameButtons
{
    public static GameObject Create(Transform parent, System.Action onPlayAgain, float yOffset = -150f)
    {
        var container = new GameObject("EndGameButtons");
        container.transform.SetParent(parent, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.offsetMin = new Vector2(0, yOffset);
        containerRect.offsetMax = new Vector2(0, yOffset);

        // Find RoundedRect sprite from an existing UI Image in the scene
        Sprite roundedRect = null;
        foreach (var img in parent.GetComponentsInParent<Image>(true))
        {
            if (img.sprite != null && img.sprite.name == "RoundedRect")
            {
                roundedRect = img.sprite;
                break;
            }
        }
        if (roundedRect == null)
        {
            foreach (var img in Object.FindObjectsOfType<Image>())
            {
                if (img.sprite != null && img.sprite.name == "RoundedRect")
                {
                    roundedRect = img.sprite;
                    break;
                }
            }
        }

        // Home button - bottom left
        CreateFixedButton(container.transform, "HomeBtn", "Home",
            new Color(0.3f, 0.5f, 0.8f), roundedRect,
            new Vector2(0.20f, 0.02f), new Vector2(420, 250),
            () => SceneManager.LoadScene("Home"));

        // Play Again button - bottom right
        CreateFixedButton(container.transform, "PlayAgainBtn", "Play Again",
            new Color(0.3f, 0.7f, 0.35f), roundedRect,
            new Vector2(0.80f, 0.02f), new Vector2(420, 250),
            () => onPlayAgain?.Invoke());

        return container;
    }

    private static void CreateFixedButton(Transform parent, string name, string label,
        Color color, Sprite sprite,
        Vector2 anchorPos, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(anchorPos.x, anchorPos.y);
        btnRect.anchorMax = new Vector2(anchorPos.x, anchorPos.y);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = size;
        btnRect.anchoredPosition = Vector2.zero;

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = color;
        if (sprite != null)
        {
            btnImg.sprite = sprite;
            btnImg.type = Image.Type.Sliced;
        }

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
