using System.Collections.Generic;

namespace HealerLike.Render.Grammar
{
    // Player skills and items are sized as if cast by their own class at its base stats: the class's base
    // attributes become both the stat reference of its modifiers and the base of its caster-scaled values.
    // A skill or item no class lists keeps the plain context, sized as before.
    public static class PlayerClassContext
    {
        // The first class listing the handler through one of its skills or items, in the order given (asset path
        // order in the atlas). No skill or item is shared at ade6ad91; a shared one takes its first owner rather
        // than its largest reading, so its look never depends on which other classes happen to exist
        public static CharacterData Owner(ABuffHandlerFactory handler, IEnumerable<CharacterData> characters)
        {
            if (handler == null || characters == null) return null;
            foreach (CharacterData character in characters)
            {
                if (Owns(character, handler)) return character;
            }
            return null;
        }

        // The first class listing the skill, same order rule as the handler lookup
        public static CharacterData Owner(CharacterSkillData skill, IEnumerable<CharacterData> characters)
        {
            if (skill == null || characters == null) return null;
            foreach (CharacterData character in characters)
            {
                if (character == null || character.skills == null) continue;
                foreach (ACharacterSkillFactory factory in character.skills)
                {
                    if (factory is IGameDataSource source && ReferenceEquals(source.sourceData, skill)) return character;
                }
            }
            return null;
        }

        public static bool Owns(CharacterData character, ABuffHandlerFactory handler)
        {
            if (character == null || handler == null) return false;
            if (character.skills != null)
            {
                foreach (ACharacterSkillFactory factory in character.skills)
                {
                    if (factory is IGameDataSource source && source.sourceData is BuffCharacterSkillData buff
                        && Contains(buff.buffHandlerFactory, handler)) return true;
                }
            }
            if (character.items != null)
            {
                foreach (AItemFactory factory in character.items)
                {
                    if (factory is ItemFactory item && item.data != null
                        && (Contains(item.data.buffs, handler) || Contains(item.data.onHitEffects, handler))) return true;
                }
            }
            return false;
        }

        // The context with the class's base stats as both the modifier reference and the caster base.
        // Without a class, or a class without stats, the context is returned unchanged
        public static EffectContext With(EffectContext context, CharacterData owner)
        {
            if (owner == null || owner.attributes == null) return context;
            Dictionary<AttributeType, float> baselines = new Dictionary<AttributeType, float>(owner.attributes);
            context.attributeBaselines = baselines;
            context.casterBaselines = baselines;
            return context;
        }

        public static EffectContext For(ABuffHandlerFactory handler, EffectContext context,
            IEnumerable<CharacterData> characters)
        {
            return With(context, Owner(handler, characters));
        }

        static bool Contains(List<ABuffHandlerFactory> handlers, ABuffHandlerFactory handler)
        {
            return handlers != null && handlers.Contains(handler);
        }
    }
}
