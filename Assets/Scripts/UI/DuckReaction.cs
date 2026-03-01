using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DuckReaction : MonoBehaviour
{
    [Header("Rubber Duck Parts")]
    public Image duckBody;          // Main rounded body
    public Image duckHead;          // Round head
    public Image duckBeak;          // Orange triangular beak
    public Image duckEye;           // Black dot eye
    public Image duckWing;          // Small wing on side
    public Image duckTail;          // Small tail feathers
    public Image duckShine;         // White shine/highlight (optional)
    
    [Header("UI References")]
    public TextMeshProUGUI duckMessage;
    public CanvasGroup canvasGroup;
    
    // Classic rubber duck colors
    private readonly Color rubberDuckYellow = new Color(1f, 0.85f, 0f);
    private readonly Color beakOrange = new Color(1f, 0.6f, 0f);
    private readonly Color eyeBlack = new Color(0.1f, 0.1f, 0.1f);
    private readonly Color shineWhite = new Color(1f, 1f, 1f, 0.6f);
    
    // Store original position to prevent drift
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool hasStoredOriginals = false;
    
    public enum Emotion
    {
        Happy,
        Sad,
        Excited,
        Worried,
        Thinking,
        Celebrating,
        Shocked,
        Neutral
    }
    
private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName == "Home")
            ShowReaction(Emotion.Neutral, "Welcome!");
        else if (sceneName == "GameChoice")
            ShowReaction(Emotion.Excited, "Pick a game!");
        else if (sceneName == "Spending")
            ShowReaction(Emotion.Neutral, "Ready to shop!");
        else if (sceneName == "EmergencyFund")
            ShowReaction(Emotion.Neutral, "Let's save!");
        else
            ShowReaction(Emotion.Neutral, "Let's go!");
    }
    
    private void SetupRubberDuck()
    {
        // Set default rubber duck colors
        if (duckBody != null)
            duckBody.color = rubberDuckYellow;
        
        if (duckHead != null)
            duckHead.color = rubberDuckYellow;
        
        if (duckBeak != null)
            duckBeak.color = beakOrange;
        
        if (duckEye != null)
            duckEye.color = eyeBlack;
        
        if (duckWing != null)
            duckWing.color = rubberDuckYellow;
        
        if (duckTail != null)
            duckTail.color = rubberDuckYellow;
        
        if (duckShine != null)
            duckShine.color = shineWhite;
    }
    
    public void ShowReaction(Emotion emotion, string message = "")
    {
        // Calm mode: redirect negative emotions to Neutral
        if (GameSettings.CalmMode)
        {
            if (emotion == Emotion.Shocked || emotion == Emotion.Sad || emotion == Emotion.Worried)
                emotion = Emotion.Neutral;
        }

        StopAllCoroutines();
        SetEmotionContent(emotion, message);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        StartCoroutine(BounceAnimation(emotion));
    }
    
    private void SetEmotionContent(Emotion emotion, string customMessage)
    {
        string message = customMessage;
        Color bodyTint = rubberDuckYellow;
        
        switch (emotion)
        {
            case Emotion.Happy:
                bodyTint = new Color(1f, 0.9f, 0.2f); // Brighter yellow
                if (string.IsNullOrEmpty(message)) message = "Good choice!";
                break;
                
            case Emotion.Sad:
                bodyTint = new Color(0.8f, 0.7f, 0.1f); // Darker yellow
                if (string.IsNullOrEmpty(message)) message = "Oh no...";
                break;
                
            case Emotion.Excited:
                bodyTint = new Color(1f, 0.95f, 0.3f); // Very bright yellow
                if (string.IsNullOrEmpty(message)) message = "Amazing!";
                break;
                
            case Emotion.Worried:
                bodyTint = new Color(1f, 0.75f, 0.1f); // Orange-ish yellow
                if (string.IsNullOrEmpty(message)) message = "Careful now...";
                break;
                
            case Emotion.Thinking:
                bodyTint = new Color(0.9f, 0.8f, 0.15f); // Slightly darker
                if (string.IsNullOrEmpty(message)) message = "Hmm...";
                break;
                
            case Emotion.Celebrating:
                bodyTint = new Color(1f, 1f, 0.4f); // Golden yellow
                if (string.IsNullOrEmpty(message)) message = "You did it!";
                break;
                
            case Emotion.Shocked:
                bodyTint = new Color(1f, 0.7f, 0f); // Deep orange-yellow
                if (string.IsNullOrEmpty(message)) message = "Whoa!";
                break;
                
            case Emotion.Neutral:
                bodyTint = rubberDuckYellow;
                if (string.IsNullOrEmpty(message)) message = "Let's go!";
                break;
        }
        
        // Apply tint to rubber duck body parts only
        if (duckBody != null)
            duckBody.color = bodyTint;
        
        if (duckHead != null)
            duckHead.color = bodyTint;
        
        if (duckWing != null)
            duckWing.color = bodyTint;
        
        if (duckTail != null)
            duckTail.color = bodyTint;
        
        // Beak stays orange
        if (duckBeak != null)
            duckBeak.color = beakOrange;
        
        // Eye stays black
        if (duckEye != null)
            duckEye.color = eyeBlack;
        
        // Shine stays white
        if (duckShine != null)
            duckShine.color = shineWhite;
        
        // Update message
        if (duckMessage != null)
        {
            duckMessage.text = message;
        }
    }
    
private IEnumerator BounceAnimation(Emotion emotion)
    {
        Transform duckTransform = transform;
        
        // Use stored originals, fallback to current if not stored yet
        Vector3 startScale = hasStoredOriginals ? originalScale : duckTransform.localScale;
        Vector3 startPos = hasStoredOriginals ? originalPosition : duckTransform.localPosition;
        
        float bounceHeight = 10f;
        float bounceScale = 1.15f;
        float duration = 0.2f;

        // Calm mode: minimal animation
        if (GameSettings.CalmMode)
        {
            bounceHeight = 3f;
            bounceScale = 1.0f;
            duration = 0.3f;
        }
        // Special animations for certain emotions
        else if (emotion == Emotion.Celebrating || emotion == Emotion.Excited)
        {
            bounceHeight = 20f;
            bounceScale = 1.25f;
        }
        else if (emotion == Emotion.Shocked)
        {
            bounceHeight = 15f;
            bounceScale = 1.2f;
        }
        else if (emotion == Emotion.Sad)
        {
            bounceHeight = 5f;
            bounceScale = 0.95f;
        }
        
        float elapsed = 0f;
        
        // Bounce up
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easeOut = 1f - (1f - t) * (1f - t);
            
            duckTransform.localScale = Vector3.Lerp(startScale, startScale * bounceScale, easeOut);
            duckTransform.localPosition = startPos + Vector3.up * (bounceHeight * easeOut);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        
        // Bounce down
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easeIn = t * t;
            
            duckTransform.localScale = Vector3.Lerp(startScale * bounceScale, startScale, easeIn);
            duckTransform.localPosition = Vector3.Lerp(startPos + Vector3.up * bounceHeight, startPos, easeIn);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to stored original position
        duckTransform.localScale = startScale;
        duckTransform.localPosition = startPos;
    }
}