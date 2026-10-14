public interface ICooldownSkill
{
    float cooldownProgress { get; }
    float cooldownDuration { get; }
    // Brings the next use closer by this time (e.g. the Purifier getting blessed), never before now
    void ReduceCooldown(float seconds);
}
