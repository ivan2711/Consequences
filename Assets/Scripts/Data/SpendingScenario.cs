using System;

[Serializable]
public class SpendingScenario
{
    public string id;
    public string seasonName;
    public SpendingRound[] rounds;
}

[Serializable]
public class SpendingRound
{
    public float budget;
    public string roundName;
    public string recipeName;            // e.g. "Pasta Bake"
    public string duckLine;
    public string duckEmotion;
    public SpendingItem[] essentials;    // 4 ingredients for the recipe
    public SpendingTreat[] treats;
    public SpendingTreat[] extraTreats;  // round 3 bonus items
}

[Serializable]
public class SpendingItem
{
    public string name;
    public float price;
}

[Serializable]
public class SpendingTreat
{
    public string name;
    public float price;
}
