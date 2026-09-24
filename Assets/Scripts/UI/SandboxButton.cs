using UnityEngine;
using UnityEngine.Events;

public class SandboxButton : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button _button;
    [SerializeField] TMPro.TMP_Text _label;

    public void Init(string label, UnityAction onClick)
    {
        SetLabel(label);
        _button.onClick.AddListener(onClick);
    }

    public void SetLabel(string label)
    {
        _label.text = label;
    }

    public void SetInteractable(bool interactable)
    {
        _button.interactable = interactable;
    }
}
