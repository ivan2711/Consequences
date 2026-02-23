using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingMoneyText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float floatHeight = 100f;
    public float floatDuration = 1.5f;
    
    private Canvas canvas;
    
    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
    }
    
    public void ShowMoneyChange(int amount, Vector3 worldPosition)
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null) return;
        
        // Create floating text
        GameObject textObj = new GameObject("FloatingMoney");
        textObj.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 100);
        rect.position = worldPosition;
        
        // Add TextMeshProUGUI
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        
        // Format text based on amount
        if (amount > 0)
        {
            text.text = "+£" + amount;
            text.color = new Color(0.2f, 0.9f, 0.3f); // Green
        }
        else if (amount < 0)
        {
            text.text = "-£" + Mathf.Abs(amount);
            text.color = new Color(0.9f, 0.2f, 0.2f); // Red
        }
        else
        {
            text.text = "£0";
            text.color = new Color(0.5f, 0.5f, 0.5f); // Gray
        }
        
        // Text settings
        text.fontSize = 48;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        
        // Add outline
        text.outlineWidth = 0.3f;
        text.outlineColor = new Color(0, 0, 0, 0.8f);
        
        // Animate it
        StartCoroutine(AnimateFloatingText(textObj));
    }
    
    IEnumerator AnimateFloatingText(GameObject textObj)
    {
        RectTransform rect = textObj.GetComponent<RectTransform>();
        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        
        Vector3 startPos = rect.position;
        Vector3 endPos = startPos + Vector3.up * floatHeight;
        
        float elapsed = 0f;
        
        while (elapsed < floatDuration)
        {
            float t = elapsed / floatDuration;
            
            // Ease out movement
            float easeT = 1f - (1f - t) * (1f - t);
            rect.position = Vector3.Lerp(startPos, endPos, easeT);
            
            // Scale animation
            if (t < 0.2f)
            {
                float scaleT = t / 0.2f;
                float scale = Mathf.Lerp(1.5f, 1f, scaleT);
                rect.localScale = Vector3.one * scale;
            }
            
            // Fade out
            if (elapsed > floatDuration - 0.5f)
            {
                float fadeT = (floatDuration - elapsed) / 0.5f;
                canvasGroup.alpha = fadeT;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(textObj);
    }
}
