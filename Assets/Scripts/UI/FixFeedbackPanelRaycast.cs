using UnityEngine;
using UnityEngine.UI;

public class FixFeedbackPanelRaycast : MonoBehaviour
{
    void Start()
    {
        // Ensure this panel blocks raycasts when active
        Image img = GetComponent<Image>();
        if (img != null)
            img.raycastTarget = true;

        // Add a GraphicRaycaster if on a Canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }
}
