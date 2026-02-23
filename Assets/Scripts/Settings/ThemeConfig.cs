using UnityEngine;
using System.Linq;

namespace FinancialLiteracy.Settings
{
    [CreateAssetMenu(fileName = "ThemeConfig", menuName = "Financial Literacy/Theme Configuration")]
    public class ThemeConfig : ScriptableObject
    {
        [Header("Colors")]
        public Color primaryColor = new Color(0.2f, 0.6f, 0.9f);
        public Color secondaryColor = new Color(0.3f, 0.7f, 0.5f);
        public Color backgroundColor = Color.white;
        public Color textColor = Color.black;
        public Color buttonColor = new Color(0.8f, 0.8f, 0.8f);
        public Color buttonTextColor = Color.black;
        public Color successColor = new Color(0.3f, 0.8f, 0.3f);
        public Color warningColor = new Color(0.9f, 0.7f, 0.2f);
        public Color dangerColor = new Color(0.9f, 0.3f, 0.3f);
        public Color panelColor = new Color(0.95f, 0.95f, 0.95f);

        [Header("Visual Effects")]
        public bool enableParticles = true;
        public bool enableScreenShake = true;
        [Range(0f, 1f)] public float transitionSpeed = 1f;

        [Header("Typography")]
        public Font customFont;
        public int baseFontSize = 18;
        public float lineSpacing = 1.2f;

        [Header("Spacing & Layout")]
        public float buttonPadding = 20f;
        public float panelPadding = 30f;
        public float elementSpacing = 15f;
    }

    public interface IThemedElement
    {
        void ApplyTheme(ThemeConfig theme);
    }
}