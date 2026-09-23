using UnityEngine;

public class BuffIconView : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Image _icon;
    [SerializeField] TMPro.TMP_Text _stacks;

    public void Init(Sprite icon)
    {
        _icon.sprite = icon;
    }

    public void SetStacks(int stacks)
    {
        // A single stack doesn't need a counter
        _stacks.gameObject.SetActive(stacks > 1);
        _stacks.text = stacks.ToString();
    }
}
