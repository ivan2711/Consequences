using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads all EmergencyFundEvent JSON files from Resources/Events,
/// groups them by type, and serves them to the game controller.
/// Singleton — lives across scenes.
/// </summary>
public class EventLoader : MonoBehaviour
{
    private static EventLoader _instance;
    public static EventLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventLoader>();
                if (_instance == null)
                {
                    var go = new GameObject("EventLoader");
                    _instance = go.AddComponent<EventLoader>();
                }
            }
            return _instance;
        }
    }

    // All events grouped by type
    private Dictionary<string, List<EmergencyFundEvent>> _pools;

    // Per-type shuffled queue index (so we don't repeat until exhausted)
    private Dictionary<string, int> _poolIndex;

    // Total count for diagnostics
    public int TotalEventsLoaded { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllEvents();
    }

    /// <summary>
    /// Loads every .json TextAsset from Resources/Events and parses them.
    /// </summary>
    private void LoadAllEvents()
    {
        _pools = new Dictionary<string, List<EmergencyFundEvent>>();
        _poolIndex = new Dictionary<string, int>();

        TextAsset[] assets = Resources.LoadAll<TextAsset>("Events");
        int loaded = 0;
        int failed = 0;

        foreach (TextAsset asset in assets)
        {
            try
            {
                EmergencyFundEvent evt = JsonUtility.FromJson<EmergencyFundEvent>(asset.text);

                if (string.IsNullOrEmpty(evt.id) || string.IsNullOrEmpty(evt.type))
                {
                    Debug.LogWarning($"[EventLoader] Skipping invalid event in {asset.name}");
                    failed++;
                    continue;
                }

                if (!_pools.ContainsKey(evt.type))
                {
                    _pools[evt.type] = new List<EmergencyFundEvent>();
                    _poolIndex[evt.type] = 0;
                }

                _pools[evt.type].Add(evt);
                loaded++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EventLoader] Failed to parse {asset.name}: {e.Message}");
                failed++;
            }
        }

        // Shuffle each pool
        foreach (var key in _pools.Keys.ToList())
        {
            _pools[key] = _pools[key].OrderBy(x => Random.value).ToList();
        }

        TotalEventsLoaded = loaded;
        Debug.Log($"[EventLoader] Loaded {loaded} events ({failed} failed) across {_pools.Count} types: " +
                  string.Join(", ", _pools.Select(p => $"{p.Key}={p.Value.Count}")));
    }

    /// <summary>
    /// Get the next event of the given type. Cycles through the shuffled pool.
    /// Optionally exclude a specific event id (for no-repeat rule).
    /// Returns null if no events of that type exist.
    /// </summary>
    public EmergencyFundEvent GetEvent(string type, string excludeId = null)
    {
        if (!_pools.ContainsKey(type) || _pools[type].Count == 0)
        {
            Debug.LogWarning($"[EventLoader] No events of type '{type}'");
            return null;
        }

        List<EmergencyFundEvent> pool = _pools[type];
        int startIdx = _poolIndex.ContainsKey(type) ? _poolIndex[type] : 0;

        // Try each event in the pool from current index
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = (startIdx + i) % pool.Count;
            EmergencyFundEvent candidate = pool[idx];

            if (excludeId != null && candidate.id == excludeId)
                continue;

            // Advance index past this one
            _poolIndex[type] = (idx + 1) % pool.Count;

            // If we wrapped around, reshuffle for next cycle
            if (_poolIndex[type] == 0)
            {
                _pools[type] = pool.OrderBy(x => Random.value).ToList();
                Debug.Log($"[EventLoader] Reshuffled {type} pool");
            }

            return candidate;
        }

        // Fallback: return any event if all are excluded
        _poolIndex[type] = (startIdx + 1) % pool.Count;
        return pool[startIdx];
    }

    /// <summary>
    /// Get a random easy event (bonus or normal) — used by the "Try easier" button.
    /// </summary>
    public EmergencyFundEvent GetEasyEvent(string excludeId = null)
    {
        // Prefer bonus, fall back to normal
        EmergencyFundEvent evt = GetEvent("bonus", excludeId);
        if (evt == null)
            evt = GetEvent("normal", excludeId);
        return evt;
    }

    /// <summary>
    /// Get event by difficulty level — used by adaptive system.
    /// </summary>
    public EmergencyFundEvent GetEventByDifficulty(string difficulty, string excludeId = null)
    {
        // Collect all events matching difficulty
        List<EmergencyFundEvent> matches = new List<EmergencyFundEvent>();
        foreach (var pool in _pools.Values)
        {
            matches.AddRange(pool.Where(e => e.difficulty == difficulty && e.id != excludeId));
        }

        if (matches.Count == 0) return null;
        return matches[Random.Range(0, matches.Count)];
    }

    /// <summary>
    /// Get available type names.
    /// </summary>
    public string[] GetAvailableTypes()
    {
        return _pools.Keys.ToArray();
    }

    /// <summary>
    /// Get count for a type.
    /// </summary>
    public int GetCountForType(string type)
    {
        return _pools.ContainsKey(type) ? _pools[type].Count : 0;
    }
}
