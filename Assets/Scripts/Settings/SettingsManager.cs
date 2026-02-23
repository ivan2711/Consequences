using UnityEngine;
using System;

namespace FinancialLiteracy.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private SettingsData currentSettings;

        public event Action<SettingsData> OnSettingsChanged;

        private const string SETTINGS_KEY = "PlayerSettings";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        public void UpdateSettings(SettingsData newSettings)
        {
            currentSettings = newSettings;
            SaveSettings();
            OnSettingsChanged?.Invoke(currentSettings);
        }

        public SettingsData GetSettings()
        {
            return currentSettings;
        }

        public void ToggleCalmMode()
        {
            currentSettings.calmModeEnabled = !currentSettings.calmModeEnabled;
            UpdateSettings(currentSettings);
        }

        private void SaveSettings()
        {
            string json = JsonUtility.ToJson(currentSettings);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey(SETTINGS_KEY))
            {
                string json = PlayerPrefs.GetString(SETTINGS_KEY);
                currentSettings = JsonUtility.FromJson<SettingsData>(json);
            }
            else
            {
                currentSettings = SettingsData.GetDefault();
            }
        }
    }

    [Serializable]
    public class SettingsData
    {
        [Header("Calm Mode")]
        public bool calmModeEnabled = false;

        [Header("Visual Settings")]
        [Range(0f, 1f)] public float animationSpeed = 1f;
        public bool reducedMotion = false;
        public bool removeParticleEffects = false;
        public ThemeMode themeMode = ThemeMode.Standard;

        [Header("Audio Settings")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.7f;
        public bool muteAudio = false;

        [Header("Gameplay Settings")]
        public bool unlimitedTime = false;
        [Range(1f, 3f)] public float timeMultiplier = 1f;
        public bool showHints = true;

        [Header("Accessibility")]
        public bool highContrast = false;
        public TextSize textSize = TextSize.Medium;
        public bool dyslexiaFriendlyFont = false;

        public static SettingsData GetDefault()
        {
            return new SettingsData();
        }

        public void ApplyCalmModePreset()
        {
            calmModeEnabled = true;
            animationSpeed = 0.5f;
            reducedMotion = true;
            removeParticleEffects = true;
            themeMode = ThemeMode.Calm;
            musicVolume = 0.3f;
            sfxVolume = 0.4f;
            unlimitedTime = true;
            timeMultiplier = 2f;
        }

        public void ApplyStandardPreset()
        {
            calmModeEnabled = false;
            animationSpeed = 1f;
            reducedMotion = false;
            removeParticleEffects = false;
            themeMode = ThemeMode.Standard;
            musicVolume = 0.6f;
            sfxVolume = 0.7f;
            unlimitedTime = false;
            timeMultiplier = 1f;
        }
    }

    public enum ThemeMode
    {
        Standard,
        Calm,
        HighContrast
    }

    public enum TextSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }
}