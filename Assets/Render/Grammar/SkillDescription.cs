using System.Collections.Generic;

namespace HealerLike.Render.Grammar
{
    // Gameplay facts needed to derive a skill's silhouette and cadence in one reading.
    public class SkillDescription
    {
        public HeadKind head = HeadKind.Bud;
        public EffectFamily accent = EffectFamily.Damage;
        public int hits = 1;
        public float cadence = LookDerivation.DefaultAttackRate;
        public List<SkillWalker.Shot> shots = new List<SkillWalker.Shot>();
    }
}
