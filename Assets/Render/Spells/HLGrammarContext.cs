using System;

namespace HealerLike.Render.Spells
{
    public class HLGrammarContext
    {
        public Func<AttributeType, float?> sourceAttribute;
        public float? sourceHealth;
        public float? sourceMaxHealth;
        public HLTopology topology = HLTopology.Single;
        public Entity.EntityType side;
        public string selectionData = "";
    }
}
