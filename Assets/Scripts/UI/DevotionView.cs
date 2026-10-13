using UnityEngine;
using UnityEngine.UI;

// The Devotion of the character in the game HUD: an icon, lit while it holds some, and its count over its max.
// Hidden when it holds none
public class DevotionView : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] TMPro.TMP_Text _text;
    [SerializeField] Color _litColor = Color.white;
    [SerializeField] Color _unlitColor = new Color(1f, 1f, 1f, 0.3f);

    public bool isLit => _icon.color == _litColor;
    public string text => _text.text;

    public void SetDevotion(int value, int max)
    {
        gameObject.SetActive(max > 0);
        _icon.color = value > 0 ? _litColor : _unlitColor;
        _text.text = $"{value}/{max}";
    }
}
