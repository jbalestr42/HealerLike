using UnityEngine;
using UnityEngine.UI;

// Cost comes from the authored resource value. Cooldown is the existing UI's numeric fill,
// the same remaining fraction written by UseCharacterSkillButton.SetCooldown. No text parsing.
public struct ToolkitSpellState
{
    public float cost;
    public float remaining;
    public float duration;
    public bool insufficientMana;
    public bool canUse;

    public static ToolkitSpellState Read(CharacterSkillSlot slot, Character character)
    {
        ToolkitSpellState state = new ToolkitSpellState();
        if (slot == null || slot.data == null || character == null)
        {
            return state;
        }

        UseCharacterSkillButton button = slot.skillButton;
        foreach (ACharacterSkillValidatorFactory factory in slot.data.validators)
        {
            if (button != null && button.hasCost && factory is ResourceValidatorFactory resource && resource.data?.consumer?.data?.value != null)
            {
                state.cost += resource.data.consumer.data.value.GetValue(character.gameObject);
            }

            if (button != null && button.hasCooldown && factory is DurationValidatorFactory duration && duration.data != null)
            {
                state.duration = Mathf.Max(state.duration, duration.data.duration);
            }
        }
        if (button != null && button.hasCooldown)
        {
            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image.type == Image.Type.Filled && image.fillMethod == Image.FillMethod.Radial360)
                {
                    state.remaining = Mathf.Max(state.remaining, image.fillAmount);
                }
            }
        }
        state.insufficientMana = character.mana != null && character.mana.Value < state.cost;
        state.canUse = button != null && button.button != null && button.button.interactable;
        return state;
    }
}
