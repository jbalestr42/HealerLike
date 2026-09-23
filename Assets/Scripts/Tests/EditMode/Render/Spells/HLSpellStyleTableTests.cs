using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellStyleTableTests
    {
        [Test]
        public void DuplicateKeysFailClosedAndVariantSeparatesThem()
        {
            HLSpellStyleTable table = ScriptableObject.CreateInstance<HLSpellStyleTable>();
            GameObject go = new GameObject("HLTest");
            try
            {
                HLSpellSignature key = new HLSpellSignature
                {
                    sign = HLSign.Positive,
                    hasAttribute = true,
                    attribute = AttributeType.HealPower,
                    topology = HLTopology.Single
                };
                table.entries.Add(new HLSpellStyleTable.HLEntry { signature = key, prefab = go });
                table.entries.Add(new HLSpellStyleTable.HLEntry { signature = key, prefab = go });
                Assert.AreEqual(1, table.FindCollisions().Count);
                Assert.IsFalse(table.TryGet(key, out _));
                key.variant = 1;
                table.entries[1] = new HLSpellStyleTable.HLEntry { signature = key, prefab = go };
                Assert.AreEqual(0, table.FindCollisions().Count);
                Assert.IsTrue(table.TryGet(key, out _));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void MissingSignaturesFailClosed()
        {
            HLSpellStyleTable table = ScriptableObject.CreateInstance<HLSpellStyleTable>();
            GameObject a = new GameObject("HLBuff");
            GameObject b = new GameObject("HLShield");
            try
            {
                table.buff = a;
                table.shield = b;
                Assert.IsNull(
                    table.StatusPrefab(
                        new HLSpellSignature { operation = HLOperation.Attribute, attribute = AttributeType.HealthMax }
                    )
                );
                Assert.IsNull(table.StatusPrefab(new HLSpellSignature { operation = HLOperation.Prevention }));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(table);
            }
        }
    }
}
