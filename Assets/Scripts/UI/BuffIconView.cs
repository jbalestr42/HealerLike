using UnityEngine;

public class BuffIconView : MonoBehaviour
{
    // The last seconds of an effect, its icon blinking
    public const float EndingTime = 1f;
    const float BlinkSpeed = 4f;
    const float BlinkMinAlpha = 0.3f;

    [SerializeField] UnityEngine.UI.Image _icon;
    [SerializeField] TMPro.TMP_Text _stacks;
    // Optional: darkens the icon as the time of the effect runs out (filled radially), empty for an effect without
    // duration
    [SerializeField] UnityEngine.UI.Image _timer;

    public string stacksText => _stacks.gameObject.activeSelf ? _stacks.text : "";
    public float timerFill => _timer != null ? _timer.fillAmount : 0f;
    public float iconAlpha => _icon.color.a;

    public void Init(Sprite icon)
    {
        _icon.sprite = icon;
        if (_timer != null)
        {
            // The shape of the icon darkened
            _timer.sprite = icon;
            _timer.fillAmount = 0f;
        }
    }

    // A single stack doesn't need a counter, unless asked (e.g. the hits a unit still blocks)
    public void SetStacks(int stacks, bool showSingle = false)
    {
        _stacks.gameObject.SetActive(stacks > 1 || (showSingle && stacks == 1));
        _stacks.text = stacks.ToString();
    }

    // The part of the duration gone darkened, the icon blinking in its last second. A duration of 0: an effect that
    // never ends, neither
    public void SetRemaining(float remaining, float duration)
    {
        if (_timer != null)
        {
            _timer.fillAmount = GetElapsedShare(remaining, duration);
        }
        Color color = _icon.color;
        color.a = IsEnding(remaining, duration) ? GetBlinkAlpha(Time.time) : 1f;
        _icon.color = color;
    }

    public static float GetElapsedShare(float remaining, float duration)
    {
        return duration > 0f ? Mathf.Clamp01(1f - remaining / duration) : 0f;
    }

    public static bool IsEnding(float remaining, float duration)
    {
        return duration > 0f && remaining <= EndingTime;
    }

    // From BlinkMinAlpha to opaque and back, BlinkSpeed times a second
    public static float GetBlinkAlpha(float time)
    {
        return Mathf.Lerp(BlinkMinAlpha, 1f, Mathf.PingPong(time * BlinkSpeed * 2f, 1f));
    }
}
