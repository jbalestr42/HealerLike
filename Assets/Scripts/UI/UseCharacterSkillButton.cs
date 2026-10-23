using UnityEngine;

public class UseCharacterSkillButton : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button _button;
    public UnityEngine.UI.Button button => _button;
    [SerializeField] TMPro.TMP_Text _nameText;
    [SerializeField] TMPro.TMP_Text _cooldownText;
    [SerializeField] TMPro.TMP_Text _costText;
    [SerializeField] UnityEngine.UI.Image _cooldownImage;

    CharacterSkillSlot _slot;
    Character _character;

    public void Init(CharacterSkillSlot slot, Character character)
    {
        _slot = slot;
        _character = character;
        _nameText.text = slot.data.name;
        _button.onClick.AddListener(slot.UseSkill);
    }

    void Update()
    {
        if (_slot == null)
        {
            return;
        }

        CharacterSkillState state = _slot.state;
        float progress = state.cooldownProgress;
        _cooldownImage.fillAmount = progress;
        _cooldownText.text = progress != 0f ? $"{state.cooldown:F0}s" : "";
        _costText.text = state.hasCost ? state.cost.ToString("F0") : "";
        _button.interactable = _slot.CanUseSkill();
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
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        toolTip.Show(show);
        if (show && _slot != null)
        {
            toolTip.SetText(TextConvertor.Convert(_slot.data.description, _character, _slot.data));
        }
    }
}
