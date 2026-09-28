namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        // The first buff whose factory has a kind decides the handler's kind
        public static EffectKind Kind(ABuffHandlerFactory handler)
        {
            foreach (ABuffFactory buff in Buffs(handler))
            {
                EffectKind kind = Kind(buff);
                if (kind != EffectKind.Plain) return kind;
            }
            return EffectKind.Plain;
        }

        public static EffectKind Kind(ABuffFactory buff)
        {
            if (buff is ProjectileBehaviourBuffFactory) return EffectKind.Projectile;
            if (buff is MultipleShootBuffFactory) return EffectKind.Volume;
            if (buff is TimeModifierFactory) return EffectKind.Rate;
            if (buff is HPBasedModifierFactory) return EffectKind.Conditional;
            if (buff is BoostEntitiesOnRelativeCellBuffFactory) return EffectKind.Positional;
            if (buff is FlatModifierFactory) return EffectKind.Flat;
            return EffectKind.Plain;
        }
    }
}
