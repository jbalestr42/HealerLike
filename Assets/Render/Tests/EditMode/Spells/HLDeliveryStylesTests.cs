using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLDeliveryStylesTests
    {
        [TestCase(true, false, false, false, false, HLDeliveryStyle.Arc)]
        [TestCase(false, true, false, false, false, HLDeliveryStyle.Rigid)]
        [TestCase(true, false, true, false, false, HLDeliveryStyle.Swarm)]
        [TestCase(true, false, false, true, false, HLDeliveryStyle.Bounce)]
        [TestCase(false, false, false, false, true, HLDeliveryStyle.ChainSync)]
        [TestCase(false, false, false, false, false, HLDeliveryStyle.Direct)]
        public void FactsSelectDelivery(
            bool curve,
            bool laser,
            bool swarm,
            bool bounce,
            bool chain,
            HLDeliveryStyle expected
        )
        {
            Assert.AreEqual(expected, HLDeliveryStyles.Classify(curve, laser, swarm, bounce, chain));
        }

        [Test]
        public void NamesResolveOriginalStageAndCloneWithoutGameplay()
        {
            HLDeliveryStyles table = ScriptableObject.CreateInstance<HLDeliveryStyles>();
            GameObject go = new GameObject("HLCurveBullet(Clone)");
            try
            {
                table.entries.Add(
                    new HLDeliveryStyles.HLEntry { prefabName = "CurveBullet", style = HLDeliveryStyle.Arc }
                );
                Assert.AreEqual(HLDeliveryStyle.Arc, table.For(go));
                Assert.AreEqual(HLDeliveryStyle.Arc, table.ForName("CurveBullet"));
                Assert.AreEqual(HLDeliveryStyle.Direct, table.For(null));
                Assert.AreEqual(HLDeliveryStyle.Direct, table.ForName("unknown"));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void ShippedTableHasNineUniqueVariants()
        {
            HLDeliveryStyles table = AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(
                "Assets/Render/Spells/Data/HLDeliveryStyles.asset"
            );
            Assert.AreEqual(9, table.entries.Count);
            CollectionAssert.AllItemsAreUnique(table.entries.ConvertAll(x => x.prefabName));
            Assert.AreEqual(HLDeliveryStyle.ChainSync, table.ForName("ChannelingLightning"));
            Assert.AreEqual(HLDeliveryStyle.Rigid, table.ForName("StraightLaserBullet"));
        }
    }
}
