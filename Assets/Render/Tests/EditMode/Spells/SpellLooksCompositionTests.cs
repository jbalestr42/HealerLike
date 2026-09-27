using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellLooksCompositionTests
    {
        [Test]
        public void MissingSecondCellRejectsEntireHandlerInWorldAndIconPaths()
        {
            EffectVocabulary vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            SpellLooks looks = ScriptableObject.CreateInstance<SpellLooks>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            FlatModifierFactory boon = ScriptableObject.CreateInstance<FlatModifierFactory>();
            FlatModifierFactory bane = ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                EffectVocabulary shipped = RenderTestAssets.LoadEffectVocabulary();
                vocabulary.palette = shipped.palette;
                vocabulary.entries = new Dictionary<EffectKey, ElementEntry>(shipped.entries);
                vocabulary.legacyTable = new Dictionary<EffectCell, EffectCellEntry>(shipped.legacyTable);
                vocabulary.legacyTable.Remove(new EffectCell(EffectOperation.Bane, EffectAspect.Offence));
                boon.data = new FlatModifierData { type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Add, value = 5f };
                bane.data = new FlatModifierData { type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Add, value = -5f };
                handler.data = new BuffHandlerData { buffFactoryList = new List<ABuffFactory> { boon, bane } };
                TestHelpers.WithLoggingDisabled(() =>
                {
                    Assert.IsEmpty(looks.Compose(vocabulary, handler, null, null));
                    Assert.IsNull(SpellIconComposer.Compose(handler, vocabulary, looks));
                });
                vocabulary.legacyTable = new Dictionary<EffectCell, EffectCellEntry>(shipped.legacyTable);
                List<EffectRecipe> world = looks.Compose(vocabulary, handler, null, null);
                SpellIconRecipe icon = SpellIconComposer.Compose(handler, vocabulary, looks);
                Assert.AreEqual(2, world.Count);
                Assert.AreEqual(world.Count, icon.layers.Count);
                for (int i = 0; i < world.Count; i++)
                {
                    Assert.AreEqual(world[i].channels, icon.layers[i].channels);
                    Assert.AreEqual(world[i].socket, icon.layers[i].socket);
                }
            }
            finally
            {
                Object.DestroyImmediate(vocabulary);
                Object.DestroyImmediate(looks);
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(boon);
                Object.DestroyImmediate(bane);
            }
        }
    }
}
