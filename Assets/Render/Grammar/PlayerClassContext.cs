using System.Collections.Generic;
using UnityEngine;

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

        // Any icon or tool source, a skill (its factory, live skill or data) or a buff handler, owned by the first class
        // listing it. Anything else (an item's data, a consumer, a creature) belongs to no class
        public static CharacterData OwnerOf(object source, IEnumerable<CharacterData> characters)
        {
            source = SpellIconDerivation.Source(source);
            if (source is Object asset && !asset) return null;
            if (source is ABuffHandlerFactory handler) return Owner(handler, characters);
            if (source is CharacterSkillData skill) return Owner(skill, characters);
            return null;
        }

        // A live caster sizes every skill it casts, as the in-world look does (EffectDerivation.Context). A handler
        // takes the caster's class only when that class lists it, so a creature's handler never reads the healer's
        // stats. Without a caster, or for a handler the caster does not own, the listing class decides
        public static CharacterData CasterOf(object source, CharacterData caster, IEnumerable<CharacterData> characters)
        {
            source = SpellIconDerivation.Source(source);
            if (source is Object asset && !asset) return null;
            if (caster != null && caster.attributes != null)
            {
                if (source is CharacterSkillData) return caster;
                if (source is ABuffHandlerFactory handler && Owns(caster, handler)) return caster;
            }
            return OwnerOf(source, characters);
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

        // A handler's channels as its own class casts it, the plain reading when no class lists it
        public static EffectChannels Channels(ABuffHandlerFactory handler, bool isSameSide,
            IEnumerable<CharacterData> characters)
        {
            return EffectDerivation.Channels(handler, isSameSide, For(handler, EffectContext.Default, characters));
        }

        // Every class in the project, in asset path order, the list AtlasAssetCatalog.Characters sizes the atlas with.
        // Editor only, rescanned when the project changes; a player has no asset scan and a live caster instead
        public static IReadOnlyList<CharacterData> ProjectClasses()
        {
#if UNITY_EDITOR
            if (_projectClasses == null || _projectClasses.Exists(character => !character))
            {
                List<string> paths = new List<string>();
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data" }))
                {
                    paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                }
                paths.Sort(System.StringComparer.Ordinal);
                _projectClasses = new List<CharacterData>();
                foreach (string path in paths)
                {
                    CharacterData character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                    if (character) _projectClasses.Add(character);
                }
            }
            return _projectClasses;
#else
            return System.Array.Empty<CharacterData>();
#endif
        }

#if UNITY_EDITOR
        static List<CharacterData> _projectClasses;

        [UnityEditor.InitializeOnLoadMethod]
        static void WatchProject()
        {
            UnityEditor.EditorApplication.projectChanged += () => _projectClasses = null;
        }
#endif

        static bool Contains(List<ABuffHandlerFactory> handlers, ABuffHandlerFactory handler)
        {
            return handlers != null && handlers.Contains(handler);
        }
    }
}
