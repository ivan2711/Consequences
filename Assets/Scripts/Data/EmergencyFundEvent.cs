using System;

/// <summary>
/// Pure data class matching the EmergencyFundEvent JSON schema.
/// Not a MonoBehaviour — just a serializable container for JsonUtility.
/// </summary>
[Serializable]
public class EmergencyFundEvent
{
    public string id;
    public int version;
    public string type;       // normal, choice, bonus, emergency, crisis, lucky
    public string title;
    public string description;
    public int weeklyIncomePounds;
    public int costPounds;
    public int bonusPounds;
    public EventChoice[] choices;
    public string duckEmotion; // happy, sad, neutral, excited, shocked, worried, thinking, celebrating
    public string duckLine;
    public string difficulty;  // easy, medium, hard
    public string[] tags;
    public int ageRangeMin;
    public int ageRangeMax;
    public string currencyCode;
    public string author;
    public string notes;
}

[Serializable]
public class EventChoice
{
    public string label;
    public int savePounds;
    public string flavourText;
}
