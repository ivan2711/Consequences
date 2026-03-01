using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarRating : MonoBehaviour
{
    [Header("Star Images")]
    public Image[] stars; // Assign 3 star images in inspector
    
    [Header("Colors")]
    public Color filledColor = Color.yellow;
    public Color emptyColor = Color.gray;
    
    [Header("Animation")]
    public float animationDelay = 0.2f;
    public float scalePunch = 1.3f;
    
    private int currentRating = 0;
    
    public void SetRating(int rating)
    {
        currentRating = Mathf.Clamp(rating, 0, 3);
        StartCoroutine(AnimateStars());
    }
    
    private IEnumerator AnimateStars()
    {
        bool calm = GameSettings.CalmMode;
        float delay = calm ? 0.4f : animationDelay;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].color = i < currentRating ? filledColor : emptyColor;

                if (i < currentRating)
                {
                    if (calm)
                    {
                        // Gentle reveal: no punch, just a brief pause
                        yield return new WaitForSeconds(delay);
                    }
                    else
                    {
                        yield return StartCoroutine(PunchScale(stars[i].transform));
                        yield return new WaitForSeconds(delay);
                    }
                }
            }
        }
    }
    
    private IEnumerator PunchScale(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * scalePunch;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        // Scale up
        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        
        // Scale back
        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    public int GetRating()
    {
        return currentRating;
    }
}
