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

    public enum EffectOperation { Damage, Heal, Boon, Bane, Ward, Mana }
    public enum EffectAspect { Offence, Defence, Prevention }
    public enum EffectMagnitude { Light, Solid, Heavy }
    public enum EffectReach { Single, Group, All, Area, Chain }
    public enum EffectDelivery { Instant, Rigid, Arc, Swarm, ChainSync, Zone, Link }
    public enum EffectTrigger { Cast, OnHit, OnDeath, RoundEnd, Equip }
    public enum EffectSide { Ally, Opposing }
    public enum EffectOrigin { Healer, Creature, Item }

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
    [System.Serializable]
    public struct EffectChannels
    {
        public EffectOperation operation;
        public EffectAspect aspect;
        public EffectMagnitude magnitude;
        public EffectReach reach;
        public EffectDelivery delivery;
        public EffectTrigger trigger;
        public EffectSide side;
        public EffectOrigin origin;

        // Kept as compatibility aliases while gameplay callers move to the flat grammar.
        public EffectFamily family;
        public AttributeGroup group;
        public EffectTempo tempo;
        // Seconds between two ticks, 0 when the handler does not tick
        public float periodSeconds;
    }

    public struct EffectContext
    {
        public EffectOrigin origin;
        public EffectTrigger[] triggers;
        public int targetCount;
        public UnityEngine.GameObject projectilePrefab;
        public float maximumHealth;
        public System.Collections.Generic.IReadOnlyDictionary<AttributeType, float> attributeBaselines;

        public static EffectContext Default
        {
            get { return new EffectContext { origin = EffectOrigin.Creature, targetCount = 1 }; }
        }
    }
}
