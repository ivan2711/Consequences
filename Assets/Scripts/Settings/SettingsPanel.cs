using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinancialLiteracy.Settings
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle calmModeToggle;
        [SerializeField] private Slider animationSpeedSlider;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Toggle particleEffectsToggle;
        
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteAudioToggle;
        
        [SerializeField] private Toggle unlimitedTimeToggle;
        [SerializeField] private Slider timeMultiplierSlider;
        [SerializeField] private Toggle showHintsToggle;
        
        [SerializeField] private Toggle highContrastToggle;
        [SerializeField] private TMP_Dropdown textSizeDropdown;
        [SerializeField] private Toggle dyslexiaFontToggle;

        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Header("Theme Dropdown")]
        [SerializeField] private TMP_Dropdown themeDropdown;

        private SettingsData tempSettings;

        private void Start()
        {
            LoadCurrentSettings();
            SetupListeners();
        }

        private void LoadCurrentSettings()
        {
            if (SettingsManager.Instance == null) return;

            tempSettings = SettingsManager.Instance.GetSettings();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (calmModeToggle != null) calmModeToggle.isOn = tempSettings.calmModeEnabled;
            if (animationSpeedSlider != null) animationSpeedSlider.value = tempSettings.animationSpeed;
            if (reducedMotionToggle != null) reducedMotionToggle.isOn = tempSettings.reducedMotion;
            if (particleEffectsToggle != null) particleEffectsToggle.isOn = !tempSettings.removeParticleEffects;
            
            if (masterVolumeSlider != null) masterVolumeSlider.value = tempSettings.masterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = tempSettings.musicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = tempSettings.sfxVolume;
            if (muteAudioToggle != null) muteAudioToggle.isOn = tempSettings.muteAudio;
            
            if (unlimitedTimeToggle != null) unlimitedTimeToggle.isOn = tempSettings.unlimitedTime;
            if (timeMultiplierSlider != null) timeMultiplierSlider.value = tempSettings.timeMultiplier;
            if (showHintsToggle != null) showHintsToggle.isOn = tempSettings.showHints;
            
            if (highContrastToggle != null) highContrastToggle.isOn = tempSettings.highContrast;
            if (textSizeDropdown != null) textSizeDropdown.value = (int)tempSettings.textSize;
            if (dyslexiaFontToggle != null) dyslexiaFontToggle.isOn = tempSettings.dyslexiaFriendlyFont;
            if (themeDropdown != null) themeDropdown.value = (int)tempSettings.themeMode;
        }

        private void SetupListeners()
        {
            if (calmModeToggle != null) calmModeToggle.onValueChanged.AddListener(OnCalmModeChanged);
            if (animationSpeedSlider != null) animationSpeedSlider.onValueChanged.AddListener(v => tempSettings.animationSpeed = v);
            if (reducedMotionToggle != null) reducedMotionToggle.onValueChanged.AddListener(v => tempSettings.reducedMotion = v);
            if (particleEffectsToggle != null) particleEffectsToggle.onValueChanged.AddListener(v => tempSettings.removeParticleEffects = !v);
            
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(v => tempSettings.masterVolume = v);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(v => tempSettings.musicVolume = v);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(v => tempSettings.sfxVolume = v);
            if (muteAudioToggle != null) muteAudioToggle.onValueChanged.AddListener(v => tempSettings.muteAudio = v);
            
            if (unlimitedTimeToggle != null) unlimitedTimeToggle.onValueChanged.AddListener(v => tempSettings.unlimitedTime = v);
            if (timeMultiplierSlider != null) timeMultiplierSlider.onValueChanged.AddListener(v => tempSettings.timeMultiplier = v);
            if (showHintsToggle != null) showHintsToggle.onValueChanged.AddListener(v => tempSettings.showHints = v);
            
            if (highContrastToggle != null) highContrastToggle.onValueChanged.AddListener(v => tempSettings.highContrast = v);
            if (textSizeDropdown != null) textSizeDropdown.onValueChanged.AddListener(v => tempSettings.textSize = (TextSize)v);
            if (dyslexiaFontToggle != null) dyslexiaFontToggle.onValueChanged.AddListener(v => tempSettings.dyslexiaFriendlyFont = v);
            if (themeDropdown != null) themeDropdown.onValueChanged.AddListener(v => tempSettings.themeMode = (ThemeMode)v);

            if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        }

        private void OnCalmModeChanged(bool enabled)
        {
            if (enabled)
            {
                tempSettings.ApplyCalmModePreset();
            }
            else
            {
                tempSettings.ApplyStandardPreset();
            }
            UpdateUI();
        }

        private void ApplySettings()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.UpdateSettings(tempSettings);
            }
            ClosePanel();
        }

        private void ResetToDefault()
        {
            tempSettings = SettingsData.GetDefault();
            UpdateUI();
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        public void OpenPanel()
        {
            gameObject.SetActive(true);
            LoadCurrentSettings();
        }
    }
}