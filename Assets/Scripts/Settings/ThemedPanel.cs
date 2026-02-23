using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinancialLiteracy.Settings
{
    [RequireComponent(typeof(Image))]
    public class ThemedPanel : MonoBehaviour, IThemedElement
    {
        [SerializeField] private bool applyBackgroundColor = true;
        [SerializeField] private bool applyPanelColor = false;

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void Start()
        {
            if (ThemeManager.Instance != null)
            {
                ApplyTheme(ThemeManager.Instance.GetCurrentTheme());
            }
        }

        public void ApplyTheme(ThemeConfig theme)
        {
            if (theme == null || image == null) return;

            if (applyBackgroundColor)
            {
                image.color = theme.backgroundColor;
            }
            else if (applyPanelColor)
            {
                image.color = theme.panelColor;
            }
        }
    }
}