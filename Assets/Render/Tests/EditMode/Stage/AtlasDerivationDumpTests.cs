using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stage
{
    public class AtlasDerivationDumpTests
    {
        [Test]
        public void Collect_RealAssets_CoversEveryAssetAndBothSides()
        {
            // BuffEntity is intentionally skill-less; the real derivation diagnoses it on both sides.
            foreach (string guid in AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Data" }))
            {
                EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
                if (Grammar.LookDerivation.Primary(data) == null)
                {
                    LogAssert.Expect(LogType.Error, "[HeadDerivation] No head for a unit without skill");
                    LogAssert.Expect(LogType.Error, "[HeadDerivation] No head for a unit without skill");
                }
            }
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
        }
    }
}
