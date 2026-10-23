// What a spell card shows of its skill: its cost, its cooldown and whether it can be cast now
public struct ToolkitSpellState
{
    public float cost;
    // The share of the cooldown left, 0 when ready
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

        CharacterSkillState skill = slot.state;
        state.cost = skill.hasCost ? skill.cost : 0f;
        state.duration = skill.hasCooldown ? skill.cooldownDuration : 0f;
        state.remaining = skill.cooldownProgress;
        state.insufficientMana = character.mana != null && character.mana.Value < state.cost;
        state.canUse = slot.CanUseSkill();
        return state;
    }
}
