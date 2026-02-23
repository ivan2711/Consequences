using UnityEngine;
using UnityEngine.UI;

namespace FinancialLiteracy.Settings
{
    [RequireComponent(typeof(Button))]
    public class ThemedButton : MonoBehaviour, IThemedElement
    {
        [SerializeField] private bool isPrimaryButton = false;
        [SerializeField] private bool isSuccessButton = false;
        [SerializeField] private bool isDangerButton = false;

        private Button button;
        private Image buttonImage;
        private TMPro.TextMeshProUGUI buttonText;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
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
            if (theme == null) return;

            Color targetColor = theme.buttonColor;

            if (isPrimaryButton)
            {
                targetColor = theme.primaryColor;
            }
            else if (isSuccessButton)
            {
                targetColor = theme.successColor;
            }
            else if (isDangerButton)
            {
                targetColor = theme.dangerColor;
            }

            if (buttonImage != null)
            {
                buttonImage.color = targetColor;
            }

            if (buttonText != null)
            {
                buttonText.color = theme.buttonTextColor;
                // Note: Custom font handling requires TMP_FontAsset, not Unity Font
                // This would need to be set up differently in the ThemeConfig
            }

            // Apply button padding
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                LayoutElement layoutElement = GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.minHeight = theme.baseFontSize + theme.buttonPadding;
            }
        }
    }
}