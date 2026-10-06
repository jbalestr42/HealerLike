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

    public enum EffectOperation { Damage, Heal, Boon, Bane, Ward, Mana, ManaDrain }
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
        // The caster's material: what the core element is built from, never its colour
        public LookSide material;
        // The buff factory the handler is built from: what separates handlers that agree on every other channel
        public EffectKind kind;
    }

    public struct EffectContext
    {
        public EffectOrigin origin;
        public EffectTrigger[] triggers;
        public int targetCount;
        public UnityEngine.GameObject projectilePrefab;
        public LookSide material;
        public float maximumHealth;
        public System.Collections.Generic.IReadOnlyDictionary<AttributeType, float> attributeBaselines;
        // The owning class's base stats when a player skill or item is sized as its class casts it, else null.
        // Caster-scaled values read these instead of 1, and a damage-reduction fraction is its own share
        public System.Collections.Generic.IReadOnlyDictionary<AttributeType, float> casterBaselines;
        // The handlers the target's growing items stack on it, else null
        public System.Collections.Generic.HashSet<ABuffHandlerFactory> growthHandlers;

        public static EffectContext Default
        {
            get { return new EffectContext { origin = EffectOrigin.Creature, targetCount = 1 }; }
        }
    }
}
