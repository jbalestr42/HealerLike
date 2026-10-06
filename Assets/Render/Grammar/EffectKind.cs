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
        Flat,
        // Fires in answer to an event: an attack, a heal received, a kill, an overheal, a battle start
        Reactive,
        // A strike repeated
        Echo,
        // The kind half of a link: the delivery half is EffectDelivery.Link
        Link,
        // A unit arrives or comes back
        Summon,
        // Grows each battle or kill; read from the holder's growing item, never from the buff alone
        Growth
    }
}
