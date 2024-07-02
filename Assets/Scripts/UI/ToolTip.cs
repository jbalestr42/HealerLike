using UnityEngine;

public class ToolTip : MonoBehaviour
{
    [SerializeField] GameObject _toolTip;
    [SerializeField] TMPro.TMP_Text _text;

    public void Show(bool show)
    {
        _toolTip.SetActive(show);
    }

    public void SetText(string text)
    {
        _text.text = text;
    }
}
