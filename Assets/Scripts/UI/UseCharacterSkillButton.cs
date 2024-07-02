using UnityEngine;

public class UseCharacterSkillButton : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button _button;
    public UnityEngine.UI.Button button => _button;
    [SerializeField] UnityEngine.UI.Text _nameText;
    [SerializeField] UnityEngine.UI.Text _cooldownText;
    [SerializeField] UnityEngine.UI.Text _costText;
    [SerializeField] UnityEngine.UI.Image _cooldownImage;
    public CharacterSkillSlotData data { get; set; }
    public bool hasCooldown { get; set; } = false;
    public bool hasCost { get; set; } = false;

    public void SetName(string name)
    {
        _nameText.text = name;
    }

    public void SetCooldown(float cooldown, float duration)
    {
        float progress = cooldown / duration;
        _cooldownImage.fillAmount = hasCooldown ? progress : 0f;
        _cooldownText.text = hasCooldown && progress != 0f ? $"{cooldown:F0}s" : "";
    }

    public void SetCost(float cost)
    {
        _costText.text = hasCost ? cost.ToString("F0") : "";
    }

    public void Enable(bool enabled)
    {
        _button.interactable = enabled;
    }

    public void OnPointerEnter()
    {
        ShowToolTip(true);
    }

    public void OnPointerExit()
    {
        ShowToolTip(false);
    }

    void ShowToolTip(bool show)
    {
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.Show(show);
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.SetText(data.description);
    }
}
