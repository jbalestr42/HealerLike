// A skill going through named phases (e.g. strikes, then a slam), the one under way shown above the unit
public interface IPhasedSkill
{
    // The phase being played, or the next one while the cycle is between phases, with the part of it played (0 when
    // it is still to come): false when the skill has no phase
    bool TryGetPhase(out PhaseSkillStepData phase, out float progress);
}
