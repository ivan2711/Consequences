using UnityEngine;
using TMPro;

namespace FinancialLiteracy.Settings
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ThemedText : MonoBehaviour, IThemedElement
    {
        [SerializeField] private bool useCustomColor = false;
        [SerializeField] private Color customColor = Color.black;

        private TextMeshProUGUI textComponent;
        private float baseFontSizeMultiplier = 1f;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            baseFontSizeMultiplier = textComponent.fontSize / 18f; // Store relative size
        }

        private void Start()
        {
            if (ThemeManager.Instance != null)
            {
                ApplyTheme(ThemeManager.Instance.GetCurrentTheme());
            }

            // Subscribe to settings changes for text size
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged += OnSettingsChanged;
                OnSettingsChanged(SettingsManager.Instance.GetSettings());
            }
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged -= OnSettingsChanged;
            }
        }

        public void ApplyTheme(ThemeConfig theme)
        {
            if (theme == null || textComponent == null) return;

            if (!useCustomColor)
            {
                textComponent.color = theme.textColor;
            }
            else
            // Custom font handling removed - would need TMP_FontAsset reference in ThemeConfigf (theme.customFont != null)
            {
                textComponent.font = TMP_FontAsset.CreateFontAsset(theme.customFont);
            }

            textComponent.lineSpacing = (theme.lineSpacing - 1f) * 100f; // Convert to percentage
        }

        private void OnSettingsChanged(SettingsData settings)
        {
            if (textComponent == null) return;

            float sizeMultiplier = 1f;
            switch (settings.textSize)
            {
                case TextSize.Small:
                    sizeMultiplier = 0.85f;
                    break;
                case TextSize.Medium:
                    sizeMultiplier = 1f;
                    break;
                case TextSize.Large:
                    sizeMultiplier = 1.25f;
                    break;
                case TextSize.ExtraLarge:
                    sizeMultiplier = 1.5f;
                    break;
            }

            ThemeConfig theme = ThemeManager.Instance?.GetCurrentTheme();
            if (theme != null)
            {
                textComponent.fontSize = theme.baseFontSize * baseFontSizeMultiplier * sizeMultiplier;
            }
        }
    }
}