using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrefabBuilderTests
    {
        [Test] public void EveryInventorySignatureHasExactlyOnePersistentVisual()
        {
            var type=System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type builder=null;foreach(var assembly in type) builder=builder??assembly.GetType("HealerLike.Render.Spells.HLSpellPrefabBuilder");
            Assert.IsNotNull(builder);
            Assert.AreEqual("HealerLike.Render.Spells.Editor", builder.Assembly.GetName().Name);
            var rows=(System.Collections.IDictionary)builder.GetMethod("Inventory").Invoke(null,null);
            var table=AssetDatabase.LoadAssetAtPath<HLSpellStyleTable>("Assets/Render/Spells/Data/HLSpellStyles.asset");
            Assert.Greater(rows.Count,20);Assert.IsEmpty(table.FindCollisions());
            foreach(var entry in table.entries) {
                Assert.IsNotNull(entry.prefab);Assert.IsEmpty(entry.prefab.GetComponentsInChildren<Collider>());
                foreach(var f in entry.prefab.GetComponentsInChildren<MeshFilter>()) Assert.IsTrue(EditorUtility.IsPersistent(f.sharedMesh),entry.signature.ToString());
            }
            foreach(System.Collections.DictionaryEntry row in rows) {
                var value=row.Value; var signature=(HLSpellSignature)value.GetType().GetField("Item1").GetValue(value);
                Assert.IsTrue(table.TryGet(signature,out _),signature.ToString());
            }
        }
        [TestCase("HLStatus_Buff",HLSpellEffectKind.Buff)] [TestCase("HLStatus_Shield",HLSpellEffectKind.Shield)]
        [TestCase("HLFx_HealSpheres",HLSpellEffectKind.Heal)] [TestCase("HLFx_Impact",HLSpellEffectKind.Impact)] [TestCase("HLFx_ChainBeam",HLSpellEffectKind.Chain)]
        public void ShippedPrefabsHavePersistentMeshesMaterialsAndNoGameplay(string name,HLSpellEffectKind kind)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/"+name+".prefab");Assert.IsNotNull(prefab);
            Assert.AreEqual(kind,prefab.GetComponent<HLSpellEffect>().kind);Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>());
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())Assert.IsTrue(EditorUtility.IsPersistent(filter.sharedMesh));
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())Assert.IsNotNull(renderer.sharedMaterial);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Projectile>());Assert.IsEmpty(prefab.GetComponentsInChildren<BuffManager>());
        }
        [Test] public void ShippedSinkIsFullyWired()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/HLSpellVisualSink.prefab");var sink=prefab.GetComponent<HLSpellVisualSink>();Assert.IsNotNull(sink.styles);Assert.IsNotNull(sink.material);Assert.IsNotNull(sink.styles.chain);Assert.IsNotNull(sink.styles.buff);Assert.IsNotNull(sink.styles.shield);Assert.IsNotNull(sink.styles.heal);Assert.IsNotNull(sink.styles.impact);Assert.IsEmpty(sink.styles.FindCollisions());
        }
    }
}
