using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads all SpendingScenario JSON files from Resources/Scenarios,
/// shuffles them, and serves them to the Spending Game without repeats.
/// Singleton — lives across scenes.
/// </summary>
public class SpendingScenarioLoader : MonoBehaviour
{
    private static SpendingScenarioLoader _instance;
    public static SpendingScenarioLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SpendingScenarioLoader>();
                if (_instance == null)
                {
                    var go = new GameObject("SpendingScenarioLoader");
                    _instance = go.AddComponent<SpendingScenarioLoader>();
                }
            }
            return _instance;
        }
    }

    private List<SpendingScenario> _scenarios;
    private int _currentIndex;

    public int TotalScenariosLoaded { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllScenarios();
    }

    /// <summary>
    /// Loads every .json TextAsset from Resources/Scenarios and parses them.
    /// </summary>
    private void LoadAllScenarios()
    {
        _scenarios = new List<SpendingScenario>();
        _currentIndex = 0;

        TextAsset[] assets = Resources.LoadAll<TextAsset>("Scenarios");
        int loaded = 0;
        int failed = 0;

        foreach (TextAsset asset in assets)
        {
            try
            {
                SpendingScenario scenario = JsonUtility.FromJson<SpendingScenario>(asset.text);

                if (string.IsNullOrEmpty(scenario.id) || scenario.rounds == null || scenario.rounds.Length == 0)
                {
                    Debug.LogWarning($"[SpendingScenarioLoader] Skipping invalid scenario in {asset.name}");
                    failed++;
                    continue;
                }

                _scenarios.Add(scenario);
                loaded++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SpendingScenarioLoader] Failed to parse {asset.name}: {e.Message}");
                failed++;
            }
        }

        // Shuffle
        _scenarios = _scenarios.OrderBy(x => Random.value).ToList();

        TotalScenariosLoaded = loaded;
        Debug.Log($"[SpendingScenarioLoader] Loaded {loaded} scenarios ({failed} failed)");
    }

    /// <summary>
    /// Returns the next scenario, cycling through without repeats.
    /// Reshuffles when all scenarios have been used.
    /// </summary>
    public SpendingScenario GetNextScenario()
    {
        if (_scenarios == null || _scenarios.Count == 0)
        {
            Debug.LogWarning("[SpendingScenarioLoader] No scenarios loaded");
            return null;
        }

        SpendingScenario scenario = _scenarios[_currentIndex];
        _currentIndex++;

        // Wrap around and reshuffle
        if (_currentIndex >= _scenarios.Count)
        {
            _currentIndex = 0;
            _scenarios = _scenarios.OrderBy(x => Random.value).ToList();
            Debug.Log("[SpendingScenarioLoader] Reshuffled scenario pool");
        }

        return scenario;
    }
}
