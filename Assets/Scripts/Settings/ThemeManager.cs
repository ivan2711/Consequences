using UnityEngine;
using System.Linq;


namespace FinancialLiteracy.Settings
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [Header("Theme Configurations")]
        [SerializeField] private ThemeConfig standardTheme;
        [SerializeField] private ThemeConfig calmTheme;
        [SerializeField] private ThemeConfig highContrastTheme;

        private ThemeConfig currentTheme;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged += ApplyTheme;
                ApplyTheme(SettingsManager.Instance.GetSettings());
            }
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged -= ApplyTheme;
            }
        }

        private void ApplyTheme(SettingsData settings)
        {
            switch (settings.themeMode)
            {
                case ThemeMode.Standard:
                    currentTheme = standardTheme;
                    break;
                case ThemeMode.Calm:
                    currentTheme = calmTheme;
                    break;
                case ThemeMode.HighContrast:
                    currentTheme = highContrastTheme;
                    break;
            }

            ApplyCurrentTheme();
        }

private void ApplyCurrentTheme()
        {
            if (currentTheme == null) return;

            // Apply theme to all UI elements that implement IThemedElement
            IThemedElement[] themedElements = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IThemedElement>()
                .ToArray();

            foreach (var element in themedElements)
            {
                element.ApplyTheme(currentTheme);
            }
        }

        public ThemeConfig GetCurrentTheme()
        {
            return currentTheme;
        }
    }
}