using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageIconsTests
    {
        class CreatureCapture : ICreaturePortraitCapture
        {
            public bool disposed;
            public Texture2D Capture(EntityData data, Entity.EntityType side) { return new Texture2D(4, 4); }
            public void Dispose() { disposed = true; }
        }
        class SpellCapture : ISpellIconCapture
        {
            public bool disposed;
            public Texture2D Capture(SpellIconRecipe recipe) { return new Texture2D(4, 4); }
            public void Dispose() { disposed = true; }
        }

        [Test]
        public void CombinedProvider_CoalescesInvalidationAndDisposesBothOwners()
        {
            CreatureCapture creatureCapture = new CreatureCapture();
            SpellCapture spellCapture = new SpellCapture();
            CreaturePortraits portraits = new CreaturePortraits(creatureCapture);
            SpellIcons spells = new SpellIcons(RenderTestAssets.LoadEffectVocabulary(), null, spellCapture);
            StageIcons icons = new StageIcons(portraits, spells);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                Texture2D portrait = icons.GetCreatureIcon(data, Entity.EntityType.Player);
                int changes = 0;
                icons.Changed += () => changes++;
                icons.Invalidate();
                Assert.AreEqual(1, changes, "The view refreshes only after both caches are clear.");
                Assert.IsTrue(portrait == null);
                Assert.IsNull(icons.GetDataIcon(data), "Creature data keeps its portrait/fallback route.");
                icons.Dispose();
                Assert.IsTrue(creatureCapture.disposed && spellCapture.disposed);
                Assert.IsNull(icons.GetCreatureIcon(data, Entity.EntityType.Player));
                icons.Dispose();
                Assert.AreEqual(1, changes, "A detached owner must not refresh a released view.");
            }
            finally
            {
                icons.Dispose();
                Object.DestroyImmediate(data);
            }
        }
    }
}
