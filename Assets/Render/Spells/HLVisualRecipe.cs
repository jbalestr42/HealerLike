using System;
using System.Collections.Generic;

namespace HealerLike.Render.Spells
{
    public enum HLExpression
    {
        Unknown,
        Flat,
        SourceAttribute,
        SourceCurrentHealth,
        SourceMissingHealth,
        SourceMaxHealth,
        RecipientHealth,
        Round,
        Decay
    }

    public enum HLStackLaw
    {
        None,
        FlatPowers,
        Linear,
        Refresh,
        Logarithmic,
        Delegated
    }

    public class HLVisualRecipe
    {
        HLSpellSignature _signature;
        public HLSpellSignature signature { get { return _signature; } }

        HLExpression _expression;
        public HLExpression expression { get { return _expression; } }

        AttributeType _readAttribute;
        public AttributeType readAttribute { get { return _readAttribute; } }

        float _scalar;
        public float scalar { get { return _scalar; } }

        float _resourceMultiplier;
        public float resourceMultiplier { get { return _resourceMultiplier; } }

        float? _previewAmount;
        public float? previewAmount { get { return _previewAmount; } }

        bool _ignoreReduction;
        public bool ignoreReduction { get { return _ignoreReduction; } }

        bool _ignorePrevention;
        public bool ignorePrevention { get { return _ignorePrevention; } }

        AttributeModifierType _modifier;
        public AttributeModifierType modifier { get { return _modifier; } }

        HLStackLaw _stackLaw;
        public HLStackLaw stackLaw { get { return _stackLaw; } }

        float _durationSeconds;
        public float durationSeconds { get { return _durationSeconds; } }

        float _periodSeconds;
        public float periodSeconds { get { return _periodSeconds; } }

        HLClockKind _clock;
        public HLClockKind clock { get { return _clock; } }

        Entity.EntityType _side;
        public Entity.EntityType side { get { return _side; } }

        string _diagnostic;
        public string diagnostic { get { return _diagnostic; } }

        string _deliveryData;
        public string deliveryData { get { return _deliveryData; } }

        IReadOnlyList<HLVisualRecipe> _children;
        public IReadOnlyList<HLVisualRecipe> children { get { return _children; } }

        public bool isValid
        {
            get
            {
                if (_diagnostic != null)
                {
                    return false;
                }
                foreach (HLVisualRecipe child in _children)
                {
                    if (!child.isValid)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public HLVisualRecipe(
            HLSpellSignature signature,
            HLExpression expression = HLExpression.Unknown,
            AttributeType readAttribute = default,
            float scalar = 0f,
            float multiplier = 1f,
            float? preview = null,
            bool ignoreReduction = false,
            bool ignorePrevention = false,
            AttributeModifierType modifier = default,
            HLStackLaw stackLaw = HLStackLaw.None,
            float duration = 0f,
            float period = 0f,
            HLClockKind clock = HLClockKind.Simulation,
            Entity.EntityType side = Entity.EntityType.None,
            string diagnostic = null,
            string deliveryData = null,
            IEnumerable<HLVisualRecipe> children = null
        )
        {
            _signature = signature;
            _expression = expression;
            _readAttribute = readAttribute;
            _scalar = scalar;
            _resourceMultiplier = multiplier;
            _previewAmount = preview;
            _ignoreReduction = ignoreReduction;
            _ignorePrevention = ignorePrevention;
            _modifier = modifier;
            _stackLaw = stackLaw;
            _durationSeconds = duration;
            _periodSeconds = period;
            _clock = clock;
            _side = side;
            _diagnostic = diagnostic;
            _deliveryData = deliveryData ?? "";
            _children = new List<HLVisualRecipe>(children ?? Array.Empty<HLVisualRecipe>()).AsReadOnly();
        }
    }
}
