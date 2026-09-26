using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class CreatureRosterCompositionTests : GrowthStoneFixture
{
    [TestCase(Entity.EntityType.Player)]
    [TestCase(Entity.EntityType.Computer)]
    public void FullRealRoster_ComposesAllSourcesWithUnchangedDerivationAndSeparateProfiles(Entity.EntityType side)
    {
        string[] guids = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" });
        Assert.AreEqual(12, guids.Length);
        foreach (string guid in guids)
        {
            EntityData source = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
            string before = EditorJsonUtility.ToJson(source);
            UnitChannels channels = LookDerivation.Channels(source, side);
            CreatureRecipe recipe = LookComposer.Compose(channels, _vocabulary);
            try
            {
                Assert.NotNull(recipe, source.name);
                Assert.IsTrue(recipe.parts.All(p => p.shape.isProcedural), source.name);
                Assert.AreEqual(side == Entity.EntityType.Player ? _vocabulary.rootCount : 0, recipe.roots.count);
                Assert.AreEqual(
                    side == Entity.EntityType.Computer ? 2 : 0,
                    recipe.parts.Count(p => p.role == PartRole.Limb),
                    source.name
                );
                Assert.AreEqual(channels, LookDerivation.Channels(source, side));
                Assert.AreEqual(before, EditorJsonUtility.ToJson(source));
            }
            finally
            {
                if (recipe)
                {
                    Object.DestroyImmediate(recipe);
                }
            }
        }
    }
}
}
