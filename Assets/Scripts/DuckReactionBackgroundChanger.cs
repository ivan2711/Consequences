using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically changes backgrounds based on specific duck reaction messages.
/// Attach this to the same GameObject as DuckReaction component.
/// Backgrounds persist until another trigger message appears.
/// </summary>
[RequireComponent(typeof(DuckReaction))]
public class DuckReactionBackgroundChanger : MonoBehaviour
{
    [Header("Background Image")]
    [Tooltip("The Background Image component in your Canvas")]
    public Image backgroundImage;
    
    [Header("Trigger Backgrounds")]
    [Tooltip("Shows when duck says 'Pay day'")]
    public Sprite payDayBackground;
    
    [Tooltip("Shows when duck says 'Decide wisely'")]
    public Sprite decideWiselyBackground;
    
    [Tooltip("Shows when duck says 'Bonus week'")]
    public Sprite bonusWeekBackground;
    
    [Tooltip("Shows when using emergency fund")]
    public Sprite useYourFundBackground;
    
    [Tooltip("Shows when duck says 'Lucky you'")]
    public Sprite luckyYouBackground;
    
    [Tooltip("Shows for decision/choice moments")]
    public Sprite twoThingsBackground;
    
    [Tooltip("Shows at game over/end")]
    public Sprite gameOverBackground;
    
    [Tooltip("Shows when duck says 'PERFECT'")]
    public Sprite perfectBackground;
    
    private DuckReaction duckReaction;
    
    void Start()
    {
        duckReaction = GetComponent<DuckReaction>();
    }
    
    /// <summary>
    /// Call this method after showing a duck reaction to check if background should change.
    /// Pass the message that was shown to the duck.
    /// </summary>
    public void CheckAndChangeBackground(string message)
    {
        if (backgroundImage == null || string.IsNullOrEmpty(message))
            return;

        // Calm mode: keep background stable and predictable
        if (GameSettings.CalmMode)
            return;
        
        // Convert to lowercase for easier matching
        string msg = message.ToLower();
        
        // Check for trigger phrases and change background accordingly
        if (msg.Contains("pay day"))
        {
            ChangeBackground(payDayBackground);
        }
        else if (msg.Contains("decide wisely"))
        {
            ChangeBackground(decideWiselyBackground);
        }
        else if (msg.Contains("bonus week"))
        {
            ChangeBackground(bonusWeekBackground);
        }
        else if (msg.Contains("use your fund") || msg.Contains("emergency"))
        {
            ChangeBackground(useYourFundBackground);
        }
        else if (msg.Contains("lucky you") || msg.Contains("lucky"))
        {
            ChangeBackground(luckyYouBackground);
        }
        else if (msg.Contains("two things") || msg.Contains("decide"))
        {
            ChangeBackground(twoThingsBackground);
        }
        else if (msg.Contains("game over") || msg.Contains("try again"))
        {
            ChangeBackground(gameOverBackground);
        }
        else if (msg.Contains("perfect"))
        {
            ChangeBackground(perfectBackground);
        }
        // If no match, background stays as is (persist previous background)
    }
    
    /// <summary>
    /// Directly set a specific background (for manual control)
    /// </summary>
    public void SetPayDay() => ChangeBackground(payDayBackground);
    public void SetDecideWisely() => ChangeBackground(decideWiselyBackground);
    public void SetBonusWeek() => ChangeBackground(bonusWeekBackground);
    public void SetUseYourFund() => ChangeBackground(useYourFundBackground);
    public void SetLuckyYou() => ChangeBackground(luckyYouBackground);
    public void SetTwoThings() => ChangeBackground(twoThingsBackground);
    public void SetGameOver() => ChangeBackground(gameOverBackground);
    public void SetPerfect() => ChangeBackground(perfectBackground);
    
    /// <summary>
    /// Change the background sprite
    /// </summary>
    private void ChangeBackground(Sprite newBackground)
    {
        if (newBackground != null && backgroundImage != null)
        {
            backgroundImage.sprite = newBackground;
            Debug.Log($"Background changed to: {newBackground.name}");
        }
    }
}
