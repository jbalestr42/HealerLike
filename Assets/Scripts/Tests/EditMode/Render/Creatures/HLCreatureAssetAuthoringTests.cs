using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureAssetAuthoringTests
    {
        const string Root = "Assets/Render/Creatures/";
        [TestCase("HLBladeRosette")] [TestCase("HLHealer")] [TestCase("HLSpiralFern")] [TestCase("HLHangingArch")] [TestCase("HLSphereStack")]
        public void ShippedRecipeValidatesAndBuildsWithoutImportedGeometry(string name)
        {
            var recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(Root + "Data/" + name + ".asset");
            Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out var error), error);
            Assert.That(recipe.roots.count, Is.InRange(6, 8));
            if (name == "HLHealer")
            {
                Assert.AreEqual(HLPrimitive.Cone, System.Array.Find(recipe.parts, p => p.id == "HLBulb").primitive);
                Assert.AreEqual(HLPrimitive.Torus, System.Array.Find(recipe.parts, p => p.id == "HLCrown").primitive);
            }
            var parent = new GameObject("HLRecipeFixture");
            try
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Data/HLPlaceholder.mat");
                using (var rig = HLCreatureRig.Build(recipe, parent.transform, material))
                {
                    rig.Tick(1, .016f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
                    if (name == "HLHealer")
                    {
                        var crown = rig.Root.Find("HLSway/HLStem/HLCrown");
                        var before = crown.localRotation;
                        rig.Tick(11, .016f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
                        Assert.That(Quaternion.Angle(before, crown.localRotation), Is.EqualTo(180).Within(.01f));
                    }
                    foreach (var filter in parent.GetComponentsInChildren<MeshFilter>()) Assert.IsFalse(AssetDatabase.Contains(filter.sharedMesh));
                    Assert.IsEmpty(parent.GetComponentsInChildren<Collider>());
                }
            }
            finally { Object.DestroyImmediate(parent); HLPrimitiveMeshes.ReleaseAll(); }
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
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + name + ".prefab");
            Assert.NotNull(model); Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(model));
            Assert.NotNull(model.GetComponent<EntityModel>()); Assert.NotNull(model.GetComponent<HLCreatureBuilder>());
            Assert.IsEmpty(model.GetComponentsInChildren<Renderer>(true)); Assert.IsEmpty(model.GetComponentsInChildren<MeshFilter>(true)); Assert.IsEmpty(model.GetComponentsInChildren<Animator>(true));
            Assert.AreEqual(original.transform.localScale, model.transform.localScale);
            CompareSockets(original.GetComponentsInChildren<SkillSource>(true), model.GetComponentsInChildren<SkillSource>(true), original.transform, model.transform);
            CompareSockets(original.GetComponentsInChildren<SkillTargetPointTag>(true), model.GetComponentsInChildren<SkillTargetPointTag>(true), original.transform, model.transform);
            Assert.AreEqual(original.GetComponentsInChildren<Collider>(true).Length, model.GetComponentsInChildren<Collider>(true).Length);
        }
        static void CompareSockets<T>(T[] before, T[] after, Transform original, Transform model) where T : Component
        {
            Assert.AreEqual(before.Length, after.Length);
            for (int i = 0; i < before.Length; i++)
            {
                Assert.AreEqual(before[i].GetType(), after[i].GetType());
                Assert.AreEqual(AnimationUtility.CalculateTransformPath(before[i].transform, original), AnimationUtility.CalculateTransformPath(after[i].transform, model));
                Assert.Less(Vector3.Distance(original.InverseTransformPoint(before[i].transform.position), model.InverseTransformPoint(after[i].transform.position)), 1e-5);
                Assert.Less(Quaternion.Angle(before[i].transform.rotation, after[i].transform.rotation), .001f);
                Assert.AreEqual(before[i].gameObject.activeSelf, after[i].gameObject.activeSelf);
            }
        }
        [Test] public void ProjectileVariantsRetainComponentsAndFallbackRendererStates()
        {
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var variant = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/HLProjectile" + Path.GetFileName(path));
                Assert.NotNull(variant, path); Assert.NotNull(variant.GetComponent<HLProjectileVisualObserver>());
                Assert.AreEqual(original.GetComponent<Projectile>().GetType(), variant.GetComponent<Projectile>().GetType());
                Assert.AreEqual(original.GetComponentsInChildren<Collider>(true).Length, variant.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(original.GetComponentsInChildren<Renderer>(true).Length, variant.GetComponentsInChildren<Renderer>(true).Length);
                var before = original.GetComponentsInChildren<Renderer>(true);
                var after = variant.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < before.Length; i++) Assert.AreEqual(before[i].enabled, after[i].enabled, path);
                foreach (var behaviour in original.GetComponents<AProjectileBehaviour>()) Assert.NotNull(variant.GetComponent(behaviour.GetType()));
            }
        }
        [TestCase("HLBladeRosette")] [TestCase("HLHealer")] [TestCase("HLSpiralFern")] [TestCase("HLHangingArch")] [TestCase("HLSphereStack")]
        public void ShippedArmsReachAcrossCurrentBoardAndClampOutsideIt(string name)
        {
            var recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(Root + "Data/" + name + ".asset");
            using (var arm = new HLLianaArm(recipe.arms[0], null, null))
            {
                arm.Tick(0, Vector3.zero, Quaternion.identity);
                var target = new Vector3(15, 4, 15);
                arm.Begin(1, HLGestureKind.Attack, target); arm.Contact(1, target);
                arm.Tick(.016f, Vector3.zero, Quaternion.identity);
                Assert.IsTrue(arm.LastResult.reached, arm.LastResult.error.ToString());
                Assert.Less(Vector3.Distance(arm.Tip, target), .001f);
                arm.Contact(1, Vector3.right * 100);
                arm.Tick(.016f, Vector3.zero, Quaternion.identity); Assert.IsTrue(arm.LastResult.clamped);
                for (int i = 0; i < arm.SegmentCount; i++) Assert.That(Vector3.Distance(arm.Joint(i), arm.Joint(i + 1)), Is.EqualTo(.2f).Within(1e-5));
            }
        }
        [Test] public void HealerVariantRetainsCharacterAndAuthoredAnchor()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/HLHealerCharacter.prefab");
            Assert.NotNull(prefab.GetComponent<Character>()); Assert.IsNull(prefab.GetComponent<Entity>());
            Assert.NotNull(prefab.transform.Find("HLHealerAnchor").GetComponent<HLCharacterView>());
        }
    }
}
