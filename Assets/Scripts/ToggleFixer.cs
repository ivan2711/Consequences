using UnityEngine;
using UnityEngine.UI;

public class ToggleFixer : MonoBehaviour
{
    public void FixAllToggles()
    {
        Toggle[] toggles = FindObjectsOfType<Toggle>();
        
        foreach (Toggle toggle in toggles)
        {
            // Make the toggle background visible
            Image toggleBg = toggle.GetComponent<Image>();
            if (toggleBg == null)
            {
                toggleBg = toggle.gameObject.AddComponent<Image>();
            }
            toggleBg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            // Find Background child
            Transform bgTransform = toggle.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                }
                
                // Find Checkmark
                Transform checkTransform = bgTransform.Find("Checkmark");
                if (checkTransform != null)
                {
                    Image checkImage = checkTransform.GetComponent<Image>();
                    if (checkImage != null)
                    {
                        checkImage.color = new Color(0.2f, 0.7f, 0.2f, 1f);
                    }
                    
                    // Set as graphic
                    toggle.graphic = checkImage;
                }
                
                // Set target graphic
                toggle.targetGraphic = bgImage;
            }
        }
        
        Debug.Log("Fixed " + toggles.Length + " toggles!");
    }
}
