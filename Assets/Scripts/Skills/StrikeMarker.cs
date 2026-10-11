using System.Globalization;
using TMPro;
using UnityEngine;

// Put on a unit marked by a MarkedStrikeSkill until the strike: the effects of the prefab (e.g. an aura)
// and the seconds left before the strike, shown above the unit
public class StrikeMarker : MonoBehaviour
{
    [SerializeField] TMP_Text _countdown;
    // Moved above the unit, at the height of its skill target point plus this offset
    [SerializeField] Transform _countdownRoot;
    [SerializeField] float _countdownHeight = 1.5f;

    public void Attach(GameObject target)
    {
        transform.SetParent(target.transform, false);
        transform.localPosition = Vector3.zero;
        if (_countdownRoot != null)
        {
            _countdownRoot.position = TargetLines.GetPoint(target) + Vector3.up * _countdownHeight;
        }
    }

    public void SetRemaining(float seconds)
    {
        if (_countdown != null)
        {
            _countdown.text = FormatCountdown(seconds);
        }
    }

    // Hidden on the units struck along the main one: a single countdown is enough
    public void ShowCountdown(bool isShown)
    {
        // The text only: it may sit on the marker itself, whose effects stay
        if (_countdown != null)
        {
            _countdown.enabled = isShown;
        }
        if (_countdownRoot != null && _countdownRoot != transform)
        {
            _countdownRoot.gameObject.SetActive(isShown);
        }
    }

    public bool isCountdownShown => _countdown != null && _countdown.enabled;

    // "2.4": tenths of a second, never negative
    public static string FormatCountdown(float seconds)
    {
        return Mathf.Max(0f, seconds).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
