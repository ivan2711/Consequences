using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FinancialLiteracy.Settings
{
    public class ThemeCreator
    {
#if UNITY_EDITOR
        [MenuItem("Financial Literacy/Create Default Themes")]
        public static void CreateDefaultThemes()
        {
            CreateStandardTheme();
            CreateCalmTheme();
            CreateHighContrastTheme();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default themes created successfully!");
        }

        private static void CreateStandardTheme()
        {
            ThemeConfig theme = ScriptableObject.CreateInstance<ThemeConfig>();
            
            // Vibrant, engaging colors for standard mode
            theme.primaryColor = new Color(0.2f, 0.5f, 0.95f); // Bright blue
            theme.secondaryColor = new Color(0.4f, 0.8f, 0.5f); // Fresh green
            theme.backgroundColor = new Color(0.98f, 0.98f, 1f); // Very light blue-white
            theme.textColor = new Color(0.1f, 0.1f, 0.15f); // Dark blue-black
            theme.buttonColor = new Color(0.85f, 0.85f, 0.9f); // Light grey-blue
            theme.buttonTextColor = new Color(0.1f, 0.1f, 0.15f);
            theme.successColor = new Color(0.2f, 0.8f, 0.3f); // Bright green
            theme.warningColor = new Color(0.95f, 0.7f, 0.2f); // Bright orange
            theme.dangerColor = new Color(0.95f, 0.3f, 0.3f); // Bright red
            theme.panelColor = new Color(1f, 1f, 1f); // Pure white
            
            theme.enableParticles = true;
            theme.enableScreenShake = true;
            theme.transitionSpeed = 1f;
            
            theme.baseFontSize = 18;
            theme.lineSpacing = 1.2f;
            theme.buttonPadding = 20f;
            theme.panelPadding = 30f;
            theme.elementSpacing = 15f;

            AssetDatabase.CreateAsset(theme, "Assets/Settings/Themes/StandardTheme.asset");
        }

        private static void CreateCalmTheme()
        {
            ThemeConfig theme = ScriptableObject.CreateInstance<ThemeConfig>();
            
            // Soft, muted, calming colors
            theme.primaryColor = new Color(0.6f, 0.75f, 0.85f); // Soft blue
            theme.secondaryColor = new Color(0.7f, 0.85f, 0.75f); // Soft sage green
            theme.backgroundColor = new Color(0.95f, 0.96f, 0.97f); // Very light grey
            theme.textColor = new Color(0.25f, 0.25f, 0.3f); // Dark grey (less harsh than black)
            theme.buttonColor = new Color(0.88f, 0.9f, 0.92f); // Light grey
            theme.buttonTextColor = new Color(0.3f, 0.3f, 0.35f);
            theme.successColor = new Color(0.6f, 0.8f, 0.65f); // Muted green
            theme.warningColor = new Color(0.85f, 0.75f, 0.6f); // Muted tan
            theme.dangerColor = new Color(0.85f, 0.65f, 0.65f); // Muted coral
            theme.panelColor = new Color(0.97f, 0.97f, 0.98f); // Almost white with slight grey
            
            theme.enableParticles = false;
            theme.enableScreenShake = false;
            theme.transitionSpeed = 0.5f;
            
            theme.baseFontSize = 20; // Slightly larger for readability
            theme.lineSpacing = 1.5f; // More spacing
            theme.buttonPadding = 30f; // More padding
            theme.panelPadding = 40f;
            theme.elementSpacing = 25f; // More whitespace

            AssetDatabase.CreateAsset(theme, "Assets/Settings/Themes/CalmTheme.asset");
        }

        private static void CreateHighContrastTheme()
        {
            ThemeConfig theme = ScriptableObject.CreateInstance<ThemeConfig>();
            
            // High contrast for accessibility
            theme.primaryColor = new Color(0f, 0.3f, 0.8f); // Deep blue
            theme.secondaryColor = new Color(0f, 0.6f, 0f); // Deep green
            theme.backgroundColor = Color.white;
            theme.textColor = Color.black;
            theme.buttonColor = new Color(0.9f, 0.9f, 0.9f);
            theme.buttonTextColor = Color.black;
            theme.successColor = new Color(0f, 0.7f, 0f); // Strong green
            theme.warningColor = new Color(1f, 0.6f, 0f); // Strong orange
            theme.dangerColor = new Color(0.9f, 0f, 0f); // Strong red
            theme.panelColor = new Color(0.95f, 0.95f, 0.95f);
            
            theme.enableParticles = false;
            theme.enableScreenShake = false;
            theme.transitionSpeed = 0.7f;
            
            theme.baseFontSize = 20;
            theme.lineSpacing = 1.4f;
            theme.buttonPadding = 25f;
            theme.panelPadding = 35f;
            theme.elementSpacing = 20f;

            AssetDatabase.CreateAsset(theme, "Assets/Settings/Themes/HighContrastTheme.asset");
        }
#endif
    }
}