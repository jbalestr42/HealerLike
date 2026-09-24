namespace HealerLike.Render.Grammar
{
    // What an effect does to its holder, the accent and the shape of its look come from it
    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectFamily
    {
        Damage,
        Heal,
        Rot,
        Renew,
        Boon,
        Bane
    }

    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectTempo
    {
        Once,
        PerPeriod,
        ForDuration
    }

    public enum AttributeGroup
    {
        Offence,
        Defence,
        Prevention
    }

    // Everything the look of an effect reads from its handler, decided once when it lands
    public struct EffectChannels
    {
        public EffectFamily family;
        public AttributeGroup group;
        public EffectTempo tempo;
        // Seconds between two ticks, 0 when the handler does not tick
        public float periodSeconds;
    }
}
