using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrefabBuilderTests
    {
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
