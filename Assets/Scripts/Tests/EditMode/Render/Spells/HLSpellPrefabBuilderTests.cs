using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrefabBuilderTests
    {
        [Test] public void EveryShippedPrefabUsesReviewedBeautyGeometry()
        {
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Render/Spells/Prefabs"});
            Assert.Greater(paths.Length,90);
            foreach(var guid in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var fx in prefab.GetComponentsInChildren<HLSpellEffect>())
                {
                    if(fx.hasAuthoredSignature) Assert.AreEqual(HLSpellPrimitives.Kind(fx.authoredSignature),fx.kind,prefab.name);
                    switch(fx.kind)
                    {
                        case HLSpellEffectKind.Heal: Assert.AreEqual(7,fx.stalks.Length,prefab.name);break;
                        case HLSpellEffectKind.Impact: Assert.AreEqual(5,fx.parts.Length,prefab.name);Assert.AreEqual("HLStar",fx.parts[0].GetComponent<MeshFilter>().sharedMesh.name);break;
                        case HLSpellEffectKind.Buff: Assert.AreEqual(3,fx.parts.Length,prefab.name);break;
                        case HLSpellEffectKind.Shield: Assert.AreEqual(6,fx.parts.Length,prefab.name);Assert.Greater(fx.parts[0].localScale.z,.4f);break;
                        case HLSpellEffectKind.Litter: Assert.AreEqual(5,fx.parts.Length,prefab.name);break;
                    }
                }
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>()) Assert.IsTrue(EditorUtility.IsPersistent(filter.sharedMesh),prefab.name);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(),prefab.name);
            }
        }
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
