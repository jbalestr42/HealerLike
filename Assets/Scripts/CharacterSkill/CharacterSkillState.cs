// What a skill shows of its validators: its cost and its cooldown, kept up to date by them
public class CharacterSkillState
{
    // A cost to pay to use it (not a gain, e.g. the Devotion given by a heal)
    public bool hasCost = false;
    public float cost = 0f;

    public bool hasCooldown = false;
    // Seconds left before it can be used again, 0 when ready
    public float cooldown = 0f;
    // Seconds of the last cooldown started
    public float cooldownDuration = 0f;

    // The share of the cooldown left, 0 when ready
    public float cooldownProgress => hasCooldown && cooldownDuration > 0f ? cooldown / cooldownDuration : 0f;
}
