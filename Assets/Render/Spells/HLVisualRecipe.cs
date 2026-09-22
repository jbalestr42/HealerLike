using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public enum HLSign : byte { Unknown, Negative, Zero, Positive, Conditional, Mixed }
    public enum HLTopology : byte { Unknown, Single, Self, Group, Area, Sequential }
    public enum HLDurationShape : byte { Instant, Timed, Infinite, Transit, Decay }
    public enum HLTempo : byte { Immediate, Collision, HandlerTick, Cooldown, Sequence, Death, RoundEnd, Continuous, Synchronous }
    public enum HLOperation : byte { Unknown, Resource, Attribute, Prevention, TargetCount, InstallSkill, InstallBehaviour, Delivery, Wait }
    public enum HLExpression : byte { Unknown, Flat, SourceAttribute, SourceCurrentHealth, SourceMissingHealth, SourceMaxHealth, RecipientHealth, Round, Decay }
    public enum HLStackLaw : byte { None, FlatPowers, Linear, Refresh, Logarithmic, Delegated }

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
        public override string ToString() => $"{operation}/{sign}/{(hasAttribute ? attribute.ToString() : "none")}/{topology}/{duration}/{tempo}/v{variant}";
        public bool Equals(HLSpellSignature other) => sign == other.sign && attribute == other.attribute &&
            hasAttribute == other.hasAttribute && operation == other.operation && topology == other.topology &&
            duration == other.duration && tempo == other.tempo && variant == other.variant;
        public override bool Equals(object obj) => obj is HLSpellSignature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(sign, attribute, hasAttribute, operation, topology, duration, tempo, variant);
    }

    /// <summary>Immutable intent snapshot. Children are an ordered multiset, never deduplicated.</summary>
    public sealed class HLVisualRecipe
    {
        public HLSpellSignature Signature { get; }
        public HLExpression Expression { get; }
        public AttributeType ReadAttribute { get; }
        public float Scalar { get; }
        public float ResourceMultiplier { get; }
        public float? PreviewAmount { get; }
        public bool IgnoreReduction { get; }
        public bool IgnorePrevention { get; }
        public AttributeModifierType Modifier { get; }
        public HLStackLaw StackLaw { get; }
        public float DurationSeconds { get; }
        public float PeriodSeconds { get; }
        public HLClockKind Clock { get; }
        public Entity.EntityType Side { get; }
        public string Diagnostic { get; }
        public string DeliveryData { get; }
        public IReadOnlyList<HLVisualRecipe> Children { get; }
        public bool IsValid { get { if (Diagnostic != null) return false; foreach (var c in Children) if (!c.IsValid) return false; return true; } }
        public HLVisualRecipe(HLSpellSignature signature, HLExpression expression = HLExpression.Unknown,
            AttributeType readAttribute = default, float scalar = 0, float multiplier = 1, float? preview = null,
            bool ignoreReduction = false, bool ignorePrevention = false, AttributeModifierType modifier = default,
            HLStackLaw stackLaw = HLStackLaw.None, float duration = 0, float period = 0,
            HLClockKind clock = HLClockKind.Simulation, Entity.EntityType side = Entity.EntityType.None,
            string diagnostic = null, string deliveryData = null, IEnumerable<HLVisualRecipe> children = null)
        {
            Signature = signature; Expression = expression; ReadAttribute = readAttribute; Scalar = scalar;
            ResourceMultiplier = multiplier; PreviewAmount = preview; IgnoreReduction = ignoreReduction;
            IgnorePrevention = ignorePrevention; Modifier = modifier; StackLaw = stackLaw;
            DurationSeconds = duration; PeriodSeconds = period; Clock = clock; Side = side;
            Diagnostic = diagnostic; DeliveryData = deliveryData ?? "";
            Children = new List<HLVisualRecipe>(children ?? Array.Empty<HLVisualRecipe>()).AsReadOnly();
        }
    }

    /// <summary>Caller-supplied values only; null means unavailable, never a guessed zero.</summary>
    public sealed class HLGrammarContext
    {
        public Func<AttributeType, float?> SourceAttribute;
        public float? SourceHealth, SourceMaxHealth;
        public HLTopology Topology = HLTopology.Single;
        public Entity.EntityType Side;
        public string SelectionData = "";
    }
}
