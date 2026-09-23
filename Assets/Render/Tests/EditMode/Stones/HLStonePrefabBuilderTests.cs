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
            Assert.IsNotNull(model.transform.Find("BodyPivot/HLStonePresentation"));
            StoneGroundDisc shadow = model.transform.Find("GroundShadow").GetComponent<StoneGroundDisc>();
            Assert.IsTrue(shadow.isShadow);
            Assert.IsFalse(shadow.gameObject.activeSelf);

            SerializedObject visual = new SerializedObject(model.GetComponent<HLStoneEnemyVisual>());
            Assert.AreSame(shadow, visual.FindProperty("_groundShadow").objectReferenceValue);
            Assert.IsNotNull(visual.FindProperty("_presentation").objectReferenceValue);
            Assert.IsNotNull(visual.FindProperty("_effects").objectReferenceValue);
        }

        [Test]
        public void BlockPreservesColliderAndCarriesItsDiscsAndFacet()
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

            HLStoneTerrainClump clump = block.GetComponentInChildren<HLStoneTerrainClump>(true);
            SerializedObject clumpSO = new SerializedObject(clump);
            StoneGroundDisc disc = (StoneGroundDisc)clumpSO.FindProperty("_groundDisc").objectReferenceValue;
            StoneGroundDisc shadow = (StoneGroundDisc)clumpSO.FindProperty("_groundShadow").objectReferenceValue;
            Assert.IsFalse(disc.isShadow);
            Assert.IsTrue(shadow.isShadow);
            Assert.AreEqual("GroundDisc", disc.GetComponent<MeshRenderer>().sharedMaterial.name);
            Assert.AreEqual("GroundShadow", shadow.GetComponent<MeshRenderer>().sharedMaterial.name);
            Assert.AreEqual("Disc", disc.GetComponent<MeshFilter>().sharedMesh.name);
            Assert.IsNotNull(clumpSO.FindProperty("_ochreFace").objectReferenceValue);
        }

        [Test]
        public void EffectsPrefabWiresFragmentMeshesAndMaterials()
        {
            GameObject effects = AssetDatabase.LoadAssetAtPath<GameObject>(root + "StoneEffects.prefab");
            SerializedObject effectsSO = new SerializedObject(effects.GetComponent<HLStoneEffects>());
            foreach (string field in new string[] { "_stoneMaterial", "_coralMaterial", "_dustMaterial", "_meshes" })
            {
                Assert.IsNotNull(effectsSO.FindProperty(field).objectReferenceValue, field);
            }

            GameObject fragment = (GameObject)effectsSO.FindProperty("_fragmentPrefab").objectReferenceValue;
            Assert.IsNotNull(fragment.GetComponent<MeshFilter>());
            Assert.IsNotNull(fragment.GetComponent<MeshRenderer>());
            Assert.AreEqual("HealerLike/Stones/Dust", ((Material)effectsSO.FindProperty("_dustMaterial").objectReferenceValue).shader.name);
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
