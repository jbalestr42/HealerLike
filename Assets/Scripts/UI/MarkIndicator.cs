using UnityEngine;

// The arrow above the marked enemy, bobbing up and down to catch the eye
public class MarkIndicator : MonoBehaviour
{
    // In the units of its canvas: world units on the HUD of a unit
    [SerializeField] float _amplitude = 0.08f;
    [SerializeField] float _period = 1f;

    RectTransform _rectTransform;
    Vector2 _restPosition;

    void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _restPosition = _rectTransform.anchoredPosition;
    }

    void Update()
    {
        _rectTransform.anchoredPosition = _restPosition + Vector2.up * GetOffset(Time.time, _amplitude, _period);
    }

    // Height above its rest position: from 0 up to the amplitude and back, once per period
    public static float GetOffset(float time, float amplitude, float period)
    {
        if (period <= 0f)
        {
            return 0f;
        }
        return amplitude * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / period));
    }
}
