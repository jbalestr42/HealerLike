using System;

namespace HealerLike.Render.Spells
{
    public enum HLSign
    {
        Unknown,
        Negative,
        Zero,
        Positive,
        Conditional,
        Mixed
    }

    public enum HLTopology
    {
        Unknown,
        Single,
        Self,
        Group,
        Area,
        Sequential
    }

    public enum HLDurationShape
    {
        Instant,
        Timed,
        Infinite,
        Transit,
        Decay
    }

    public enum HLTempo
    {
        Immediate,
        Collision,
        HandlerTick,
        Cooldown,
        Sequence,
        Death,
        RoundEnd,
        Continuous,
        Synchronous
    }

    public enum HLOperation
    {
        Unknown,
        Resource,
        Attribute,
        Prevention,
        TargetCount,
        InstallSkill,
        InstallBehaviour,
        Delivery,
        Wait
    }

    [Serializable]
    public struct HLSpellSignature : IEquatable<HLSpellSignature>
    {
        public HLSign sign;
        public AttributeType attribute;
        public bool hasAttribute;
        public HLOperation operation;
        public HLTopology topology;
        public HLDurationShape duration;
        public HLTempo tempo;
        public byte variant;

        public override string ToString()
        {
            string attributeName = hasAttribute ? attribute.ToString() : "none";
            return $"{operation}/{sign}/{attributeName}/{topology}/{duration}/{tempo}/v{variant}";
        }

        public bool Equals(HLSpellSignature other)
        {
            return sign == other.sign
                && attribute == other.attribute
                && hasAttribute == other.hasAttribute
                && operation == other.operation
                && topology == other.topology
                && duration == other.duration
                && tempo == other.tempo
                && variant == other.variant;
        }

        public override bool Equals(object obj)
        {
            return obj is HLSpellSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sign, attribute, hasAttribute, operation, topology, duration, tempo, variant);
        }
    }
}
