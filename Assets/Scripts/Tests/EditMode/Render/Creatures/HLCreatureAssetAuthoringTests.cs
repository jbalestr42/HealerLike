using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLCreatureAssetAuthoringTests
    {
        static readonly string root = "Assets/Render/Creatures/";

        [TestCase("HLBladeRosette")]
        [TestCase("HLHealer")]
        [TestCase("HLSpiralFern")]
        [TestCase("HLHangingArch")]
        [TestCase("HLSphereStack")]
        public void ShippedRecipeValidatesAndBuildsWithoutImportedGeometry(string name)
        {
            string path = root + "Data/" + name + ".asset";
            HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out string error), error);
            Assert.That(recipe.roots.count, Is.InRange(6, 8));
            if (name == "HLHealer")
            {
                HLPart bulb = System.Array.Find(recipe.parts, part => part.id == "HLBulb");
                HLPart crown = System.Array.Find(recipe.parts, part => part.id == "HLCrown");
                Assert.AreEqual(HLPrimitive.Cone, bulb.primitive);
                Assert.AreEqual(HLPrimitive.Torus, crown.primitive);
            }

            GameObject parent = new GameObject("HLRecipeFixture");
            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(root + "Data/HLPlaceholder.mat");
                using (HLCreatureRig rig = HLCreatureRig.Build(recipe, parent.transform, material))
                {
                    rig.Tick(1f, 0.016f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
                    if (name == "HLHealer")
                    {
                        Transform crown = rig.root.Find("HLSway/HLStem/HLCrown");
                        Quaternion before = crown.localRotation;
                        rig.Tick(11f, 0.016f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
                        Assert.That(Quaternion.Angle(before, crown.localRotation), Is.EqualTo(180).Within(0.01f));
                    }

                    foreach (MeshFilter filter in parent.GetComponentsInChildren<MeshFilter>())
                    {
                        Assert.IsFalse(AssetDatabase.Contains(filter.sharedMesh));
                    }

                    Assert.IsEmpty(parent.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
                HLPrimitiveMeshes.ReleaseAll();
            }
        }

        [TestCase("HLNormal", "Assets/Models/Jomon.prefab")]
        [TestCase("HLTest", "Assets/Models/Jomon.prefab")]
        [TestCase("HLSwarm", "Assets/Models/Jomon.prefab")]
        [TestCase("HLFastShoot", "Assets/Models/OwlZun.prefab")]
        [TestCase("HLTripleShoot", "Assets/Models/OwlZun.prefab")]
        [TestCase("HLMultiShot", "Assets/Models/LakshmiTower.prefab")]
        [TestCase("HLRandomShoot", "Assets/Models/LakshmiTower.prefab")]
        [TestCase("HLChainLightning", "Assets/Models/LightningTower.prefab")]
        [TestCase("HLChanneling", "Assets/Models/SlowTowerModel.prefab")]
        [TestCase("HLSoldier", "Assets/Models/Kawaii Slime/Prefabs/Slime_01_Viking.prefab")]
        [TestCase("HLHitArmorBuffer", "Assets/Models/Kawaii Slime/Prefabs/Slime_03 Leaf.prefab")]
        public void ModelVariantPreservesOriginalSocketsAndGameplayColliders(string name, string originalPath)
        {
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/" + name + ".prefab");
            Assert.NotNull(model);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(model));
            Assert.NotNull(model.GetComponent<EntityModel>());
            Assert.NotNull(model.GetComponent<HLCreatureBuilder>());
            Assert.IsEmpty(model.GetComponentsInChildren<Renderer>(true));
            Assert.IsEmpty(model.GetComponentsInChildren<MeshFilter>(true));
            Assert.IsEmpty(model.GetComponentsInChildren<Animator>(true));
            Assert.AreEqual(original.transform.localScale, model.transform.localScale);
            CompareSockets(original.GetComponentsInChildren<SkillSource>(true),
                model.GetComponentsInChildren<SkillSource>(true), original.transform, model.transform);
            CompareSockets(original.GetComponentsInChildren<SkillTargetPointTag>(true),
                model.GetComponentsInChildren<SkillTargetPointTag>(true), original.transform, model.transform);
            Assert.AreEqual(original.GetComponentsInChildren<Collider>(true).Length,
                model.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void ProjectileVariantsRetainComponentsAndFallbackRendererStates()
        {
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string variantPath = root + "Prefabs/HLProjectile" + Path.GetFileName(path);
                GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                Assert.NotNull(variant, path);
                Assert.NotNull(variant.GetComponent<HLProjectileVisualObserver>());
                Assert.AreEqual(original.GetComponent<Projectile>().GetType(),
                    variant.GetComponent<Projectile>().GetType());
                Assert.AreEqual(original.GetComponentsInChildren<Collider>(true).Length,
                    variant.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(original.GetComponentsInChildren<Renderer>(true).Length,
                    variant.GetComponentsInChildren<Renderer>(true).Length);
                Renderer[] before = original.GetComponentsInChildren<Renderer>(true);
                Renderer[] after = variant.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < before.Length; i++)
                {
                    Assert.AreEqual(before[i].enabled, after[i].enabled, path);
                }

                foreach (AProjectileBehaviour behaviour in original.GetComponents<AProjectileBehaviour>())
                {
                    Assert.NotNull(variant.GetComponent(behaviour.GetType()));
                }
            }
        }

        [TestCase("HLBladeRosette")]
        [TestCase("HLHealer")]
        [TestCase("HLSpiralFern")]
        [TestCase("HLHangingArch")]
        [TestCase("HLSphereStack")]
        public void ShippedArmsReachAcrossCurrentBoardAndClampOutsideIt(string name)
        {
            string path = root + "Data/" + name + ".asset";
            HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            using (HLLianaArm arm = new HLLianaArm(recipe.arms[0], null, null))
            {
                arm.Tick(0f, Vector3.zero, Quaternion.identity);
                Vector3 target = new Vector3(15f, 4f, 15f);
                arm.Begin(1, HLGestureKind.Attack, target);
                arm.Contact(1, target);
                arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
                Assert.IsTrue(arm.lastResult.reached, arm.lastResult.error.ToString());
                Assert.Less(Vector3.Distance(arm.tip, target), 0.001f);
                arm.Contact(1, Vector3.right * 100f);
                arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
                Assert.IsTrue(arm.lastResult.clamped);
                for (int i = 0; i < arm.segmentCount; i++)
                {
                    float link = Vector3.Distance(arm.Joint(i), arm.Joint(i + 1));
                    Assert.That(link, Is.EqualTo(recipe.arms[0].segmentLength).Within(0.00001));
                }
            }
        }

        [Test]
        public void HealerVariantRetainsCharacterAndAuthoredAnchor()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/HLHealerCharacter.prefab");
            Assert.NotNull(prefab.GetComponent<Character>());
            Assert.IsNull(prefab.GetComponent<Entity>());
            Assert.NotNull(prefab.transform.Find("HLHealerAnchor").GetComponent<HLCharacterView>());
        }

        static void CompareSockets<SocketType>(SocketType[] before, SocketType[] after, Transform original,
            Transform model)
            where SocketType : Component
        {
            Assert.AreEqual(before.Length, after.Length);
            for (int i = 0; i < before.Length; i++)
            {
                Assert.AreEqual(before[i].GetType(), after[i].GetType());
                Assert.AreEqual(AnimationUtility.CalculateTransformPath(before[i].transform, original),
                    AnimationUtility.CalculateTransformPath(after[i].transform, model));
                Vector3 beforeLocal = original.InverseTransformPoint(before[i].transform.position);
                Vector3 afterLocal = model.InverseTransformPoint(after[i].transform.position);
                Assert.Less(Vector3.Distance(beforeLocal, afterLocal), 0.00001);
                Assert.Less(Quaternion.Angle(before[i].transform.rotation, after[i].transform.rotation), 0.001f);
                Assert.AreEqual(before[i].gameObject.activeSelf, after[i].gameObject.activeSelf);
            }
        }
    }
}
