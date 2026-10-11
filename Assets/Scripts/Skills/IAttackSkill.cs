// A skill which may attack the targets of the entity (its TargetProvider), the ones its TargetLines show
public interface IAttackSkill
{
    bool attacksTargets { get; }
}
