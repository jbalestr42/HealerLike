using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace HealerLike.Render.Stones
{
    public class HLStonePrefabBuilderTests
    {
        static readonly string root = "Assets/Render/Stones/Prefabs/";

        [TestCase("HLStoneSoldierModel")]
        [TestCase("HLStoneCairnModel")]
        public void ModelVariantsKeepSourceTargetAndHudContracts(string name)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(root + name + ".prefab");
            Assert.IsNotNull(model);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(model));
            Assert.IsNotNull(model.GetComponent<EntityModel>());
            Assert.IsNotNull(model.GetComponent<HLStoneEnemyVisual>());
            Assert.AreEqual(1, model.GetComponentsInChildren<SkillSource>(true).Length);
            Assert.AreEqual(1, model.GetComponentsInChildren<SkillTargetPointTag>(true).Length);
            Assert.IsNotNull(model.GetComponentInChildren<EntityHUD>(true));
            Assert.AreEqual(0, model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            Assert.IsNotNull(model.transform.Find("BodyPivot").GetComponent<LookAtTarget>());
        }

        [Test]
        public void BlockPreservesColliderAndGridPreservesListOrderAndRanges()
        {
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");
            GameObject block = AssetDatabase.LoadAssetAtPath<GameObject>(root + "HLStoneBlock.prefab");
            Assert.AreEqual(original.layer, block.layer);
            BoxCollider a = original.GetComponentInChildren<BoxCollider>();
            BoxCollider b = block.GetComponentInChildren<BoxCollider>();
            Assert.AreEqual(a.size, b.size);
            Assert.AreEqual(a.center, b.center);
            Assert.AreEqual(a.transform.localPosition, b.transform.localPosition);
            Assert.AreEqual(a.isTrigger, b.isTrigger);
            Assert.AreEqual(a.gameObject.layer, b.gameObject.layer);

            GameObject grid = AssetDatabase.LoadAssetAtPath<GameObject>(root + "HLStoneGrid.prefab");
            HLStoneBlockGridSystem[] systems = grid.GetComponents<HLStoneBlockGridSystem>();
            Assert.AreEqual(2, systems.Length);
            Assert.AreEqual(7, systems[0].min);
            Assert.AreEqual(17, systems[0].max);
            Assert.AreEqual(0, systems[1].min);
            Assert.AreEqual(3, systems[1].max);
            Assert.IsTrue(systems.All(system => !system.isWalkable));

            SerializedObject generator = new SerializedObject(grid.GetComponent<GridGenerator>());
            SerializedProperty list = generator.FindProperty("_gridGenerators");
            Assert.AreEqual(4, list.arraySize);
            Assert.AreSame(systems[0], list.GetArrayElementAtIndex(1).objectReferenceValue);
            Assert.AreSame(systems[1], list.GetArrayElementAtIndex(2).objectReferenceValue);
            Assert.IsInstanceOf<HLStoneGenerationFence>(list.GetArrayElementAtIndex(3).objectReferenceValue);

            GridGeneratorSystem first = (GridGeneratorSystem)list.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.AreEqual(2, first.min);
            Assert.AreEqual(5, first.max);
            Assert.IsTrue(first.isWalkable);
            GameObject prop = new SerializedObject(first).FindProperty("_prefab").objectReferenceValue as GameObject;
            Assert.IsNotNull(prop);
            Assert.AreEqual(0, prop.GetComponentsInChildren<Collider>().Length);

            Assert.AreEqual(5, new SerializedObject(systems[0]).FindProperty("_minSize").intValue);
            Assert.AreEqual(15, new SerializedObject(systems[0]).FindProperty("_maxSize").intValue);
            Assert.AreEqual(4, new SerializedObject(systems[1]).FindProperty("_minSize").intValue);
            Assert.AreEqual(15, new SerializedObject(systems[1]).FindProperty("_maxSize").intValue);
        }

        [Test]
        public void ProjectileVariantsHaveBridgeWithoutAlteringOriginal()
        {
            foreach (string path in System.IO.Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (original.GetComponent<Projectile>() == null)
                {
                    continue;
                }

                string variantPath = root + "HLStone" + original.name + ".prefab";
                GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                Assert.IsNotNull(variant);
                Assert.IsNotNull(variant.GetComponent<HLStoneProjectileImpactBridge>());
                Assert.IsNull(original.GetComponent<HLStoneProjectileImpactBridge>());
            }
        }
    }
}
