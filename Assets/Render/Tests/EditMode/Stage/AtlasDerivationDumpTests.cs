using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Stage
{
    public class AtlasDerivationDumpTests
    {
        [Test]
        public void Collect_RealAssets_CoversEveryAssetAndBothSides()
        {
            // Passive-only units use Bud without emitting an error while every source is enumerated.
            AtlasDerivationDump.Document dump = AtlasDerivationDump.Collect();
            string[] entities = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Data" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string side in new[] { "Player", "Computer" })
            {
                CollectionAssert.AreEquivalent(entities, dump.entities.Where(row => row.side == side).Select(row => row.path));
            }
            Assert.AreEqual(2 * AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" }).Length, dump.handlers.Count);
            Assert.AreEqual(AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Projectiles" }).Length, dump.projectiles.Count);
            Assert.AreEqual(AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data" }).Length, dump.characters.Count);
            Assert.IsTrue(dump.summary.All(row => row.distinctCount == row.values.Length && row.isConstant == (row.distinctCount == 1)));
            Assert.IsTrue(dump.entities.All(row => !string.IsNullOrEmpty(row.view)));
            Assert.AreEqual(40, dump.commit.Length);
            Assert.AreEqual(2, dump.schemaVersion);
            Assert.AreEqual(2 * AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" }).Length,
                dump.spells.Count);
            Assert.AreEqual(AssetDatabase.FindAssets("t:BaseCharacterSkillData", new[] { "Assets/Data/CharacterSkills" }).Length,
                dump.healerSkills.Count);
            Assert.AreEqual(dump.projectiles.Count, dump.projectiles.Count(row => !string.IsNullOrEmpty(row.effectDelivery)));
            Assert.IsNotNull(JsonUtility.FromJson<AtlasDerivationDump.Document>(JsonUtility.ToJson(dump)));
        }

        [Test]
        public void SameSideSpellRows_MatchPinnedGrammar()
        {
            AtlasDerivationDump.Document dump = AtlasDerivationDump.Collect();
            foreach (SpellChannelAssetPinningTests.HandlerRow expected in SpellChannelAssetPinningTests.HandlerRows)
            {
                string path = "Assets/Data/" + expected.path + ".asset";
                AtlasDerivationDump.SpellRow row = dump.spells.First(item => item.path == path && item.side == "Same");
                Assert.AreEqual(expected.operation.ToString(), row.channels.operation, path);
                Assert.AreEqual(expected.aspect.ToString(), row.channels.aspect, path);
                Assert.AreEqual(expected.tempo.ToString(), row.channels.tempo, path);
                Assert.AreEqual(expected.magnitude.ToString(), row.channels.magnitude, path);
                Assert.AreEqual(expected.trigger.ToString(), row.channels.trigger, path);
            }
        }
    }
}
