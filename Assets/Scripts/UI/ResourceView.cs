using UnityEngine;

public class ResourceView : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Slider _slider;
    [SerializeField] TMPro.TMP_Text _text;

    public void SetResource(float current, float max)
    {
        _slider.value = current / max;
        SetText($"{current:F0} / {max:F0}");
    }

    public void SetText(string text)
    {
        _text.text = text;
    }

    public void Show(bool show)
    {
        _slider.gameObject.SetActive(show);
    }
}
