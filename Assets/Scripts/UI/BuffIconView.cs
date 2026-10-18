using UnityEngine;

public class BuffIconView : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Image _icon;
    [SerializeField] TMPro.TMP_Text _stacks;

    public void Init(Sprite icon)
    {
        _icon.sprite = icon;
    }

    // A single stack doesn't need a counter, unless asked (e.g. the hits a unit still blocks)
    public void SetStacks(int stacks, bool showSingle = false)
    {
        _stacks.gameObject.SetActive(stacks > 1 || (showSingle && stacks == 1));
        _stacks.text = stacks.ToString();
    }

    public string stacksText => _stacks.gameObject.activeSelf ? _stacks.text : "";
}
