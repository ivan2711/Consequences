using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarRating : MonoBehaviour
{
    [Header("Star Images (optional — leave empty to use text)")]
    public Image[] stars;

    [Header("Colors")]
    public Color filledColor = Color.yellow;
    public Color emptyColor  = new Color(0.4f, 0.4f, 0.4f);

    [Header("Animation")]
    public float animationDelay = 0.2f;
    public float scalePunch = 1.3f;

    private TMP_Text _label;
    private bool _useText;
    private int currentRating = 0;

    private void Awake()
    {
        if (stars == null || stars.Length == 0)
            stars = GetComponentsInChildren<Image>(true);

        // Use text if no star images have sprites assigned
        bool hasSprites = stars.Length > 0 && stars[0] != null && stars[0].sprite != null;
        _useText = !hasSprites;

        if (_useText)
        {
            // Hide blank image boxes
            foreach (var img in stars)
                if (img != null) img.gameObject.SetActive(false);

            // Find or create a TMP_Text child
            _label = GetComponentInChildren<TMP_Text>(true);
            if (_label == null)
            {
                var go = new GameObject("RatingLabel");
                go.transform.SetParent(transform, false);
                _label = go.AddComponent<TextMeshProUGUI>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            _label.fontSize = 64;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = filledColor;
        }
    }

    public void SetRating(int rating)
    {
        currentRating = Mathf.Clamp(rating, 0, 3);
        if (_useText)
            ApplyTextRating();
        else
            StartCoroutine(AnimateStars());
    }

    private void ApplyTextRating()
    {
        if (_label == null) return;
        // Use filled/empty star characters — LiberationSans includes U+2605/U+2606
        string filled = "<color=#FFD700>\u2605</color>";
        string empty  = "<color=#555555>\u2606</color>";
        string s = "";
        for (int i = 0; i < 3; i++)
            s += (i < currentRating ? filled : empty) + (i < 2 ? "  " : "");
        _label.text = s;
    }

    private IEnumerator AnimateStars()
    {
        bool calm = GameSettings.CalmMode;
        float delay = calm ? 0.4f : animationDelay;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            stars[i].color = i < currentRating ? filledColor : emptyColor;
            if (i < currentRating)
            {
                if (!calm)
                    yield return StartCoroutine(PunchScale(stars[i].transform));
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private IEnumerator PunchScale(Transform target)
    {
        Vector3 orig = target.localScale;
        Vector3 big  = orig * scalePunch;
        float elapsed = 0f, dur = 0.2f;
        while (elapsed < dur) { target.localScale = Vector3.Lerp(orig, big, elapsed / dur); elapsed += Time.deltaTime; yield return null; }
        elapsed = 0f;
        while (elapsed < dur) { target.localScale = Vector3.Lerp(big, orig, elapsed / dur); elapsed += Time.deltaTime; yield return null; }
        target.localScale = orig;
    }

    public int GetRating() => currentRating;
}
