namespace HealerLike.Render.Grammar
{
    // Which buff factory builds the effect: what still separates handlers that share operation, aspect,
    // tempo and trigger. Plain when no factory matches, and on every handler without one of these buffs
    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectKind
    {
        Plain,
        Projectile,
        Volume,
        Rate,
        Conditional,
        Positional,
        Flat
    }
}
