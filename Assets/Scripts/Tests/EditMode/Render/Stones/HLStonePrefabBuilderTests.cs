using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace HealerLike.Render.Stones
{
    public class HLStonePrefabBuilderTests
    {
        const string Root="Assets/Render/Stones/Prefabs/";
        [TestCase("HLStoneSoldierModel")] [TestCase("HLStoneCairnModel")]
        public void ModelVariantsKeepSourceTargetAndHudContracts(string name)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".prefab");Assert.IsNotNull(go);
            Assert.AreEqual(PrefabAssetType.Variant,PrefabUtility.GetPrefabAssetType(go));
            Assert.IsNotNull(go.GetComponent<EntityModel>());Assert.IsNotNull(go.GetComponent<HLStoneEnemyVisual>());
            Assert.AreEqual(1,go.GetComponentsInChildren<SkillSource>(true).Length);Assert.AreEqual(1,go.GetComponentsInChildren<SkillTargetPointTag>(true).Length);
            Assert.IsNotNull(go.GetComponentInChildren<EntityHUD>(true));Assert.AreEqual(0,go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            Assert.IsNotNull(go.transform.Find("BodyPivot").GetComponent<LookAtTarget>());
        }
        [Test] public void BlockPreservesColliderAndGridPreservesListOrderAndRanges()
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");var block=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HLStoneBlock.prefab");
            Assert.AreEqual(original.layer,block.layer);var a=original.GetComponentInChildren<BoxCollider>();var b=block.GetComponentInChildren<BoxCollider>();
            Assert.AreEqual(a.size,b.size);Assert.AreEqual(a.center,b.center);Assert.AreEqual(a.transform.localPosition,b.transform.localPosition);Assert.AreEqual(a.isTrigger,b.isTrigger);Assert.AreEqual(a.gameObject.layer,b.gameObject.layer);
            var grid=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HLStoneGrid.prefab");var systems=grid.GetComponents<HLStoneBlockGridSystem>();Assert.AreEqual(2,systems.Length);
            Assert.AreEqual(20,systems[0].min);Assert.AreEqual(50,systems[0].max);Assert.AreEqual(0,systems[1].min);Assert.AreEqual(10,systems[1].max);Assert.IsTrue(systems.All(s=>!s.isWalkable));
            var list=new SerializedObject(grid.GetComponent<GridGenerator>()).FindProperty("_gridGenerators");Assert.AreEqual(4,list.arraySize);
            Assert.AreSame(systems[0],list.GetArrayElementAtIndex(1).objectReferenceValue);Assert.AreSame(systems[1],list.GetArrayElementAtIndex(2).objectReferenceValue);
            Assert.IsInstanceOf<HLStoneGenerationFence>(list.GetArrayElementAtIndex(3).objectReferenceValue);
            var first=(GridGeneratorSystem)list.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.AreEqual(2,first.min); Assert.AreEqual(5,first.max); Assert.IsTrue(first.isWalkable);
            var prop=new SerializedObject(first).FindProperty("_prefab").objectReferenceValue as GameObject;
            Assert.IsNotNull(prop); Assert.AreEqual(0,prop.GetComponentsInChildren<Collider>().Length);
            Assert.AreEqual(5,new SerializedObject(systems[0]).FindProperty("_minSize").intValue);
            Assert.AreEqual(15,new SerializedObject(systems[0]).FindProperty("_maxSize").intValue);
            Assert.AreEqual(4,new SerializedObject(systems[1]).FindProperty("_minSize").intValue);
            Assert.AreEqual(15,new SerializedObject(systems[1]).FindProperty("_maxSize").intValue);
        }
        [Test] public void ProjectileVariantsHaveBridgeWithoutAlteringOriginal()
        {
            foreach(var path in System.IO.Directory.GetFiles("Assets/Prefabs/Projectiles","*.prefab"))
            {
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(original.GetComponent<Projectile>()==null)continue;
                var variant=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HLStone"+original.name+".prefab");Assert.IsNotNull(variant);Assert.IsNotNull(variant.GetComponent<HLStoneProjectileImpactBridge>());
                Assert.IsNull(original.GetComponent<HLStoneProjectileImpactBridge>());
            }
        }
    }
}
