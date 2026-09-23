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
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
                using (HLCreatureRig rig = HLCreatureRigTests.CreateRig(recipe, parent.transform, material))
                {
                    rig.Tick(1f, 0.016f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
                    if (name == "HLHealer")
                    {
                        Transform crown = rig.root.Find("HLSway/HLStem/HLCrown");
                        Quaternion before = crown.localRotation;
                        rig.Tick(11f, 0.016f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
                        Assert.That(Quaternion.Angle(before, crown.localRotation), Is.EqualTo(180).Within(0.01f));
                    }

                    // Body parts share the baked meshes, only the arm chains are generated per rig
                    foreach (MeshFilter filter in parent.GetComponentsInChildren<MeshFilter>())
                    {
                        bool isChain = filter.name == "HLLianaArm";
                        Assert.AreEqual(!isChain, AssetDatabase.Contains(filter.sharedMesh), filter.name);
                    }

                    Assert.IsEmpty(parent.GetComponentsInChildren<Collider>());
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [TestCase("HLNormal", "HLSpiralFern")]
        [TestCase("HLTest", "HLSphereStack")]
        [TestCase("HLSwarm", "HLHangingArch")]
        [TestCase("HLFastShoot", "HLSpiralFern")]
        [TestCase("HLTripleShoot", "HLHangingArch")]
        [TestCase("HLMultiShot", "HLHangingArch")]
        [TestCase("HLRandomShoot", "HLSpiralFern")]
        [TestCase("HLChainLightning", "HLSphereStack")]
        [TestCase("HLChanneling", "HLSphereStack")]
        [TestCase("HLSoldier", "HLSphereStack")]
        [TestCase("HLHitArmorBuffer", "HLBladeRosette")]
        public void View_ShippedPrefab_HasNoBaseModelAndCarriesItsLook(string name, string recipeName)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/" + name + ".prefab");

            Assert.NotNull(view);
            Assert.AreEqual(PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(view));
            Assert.IsNull(view.GetComponent<EntityModel>());
            Assert.IsEmpty(view.GetComponentsInChildren<Renderer>(true));
            Assert.IsEmpty(view.GetComponentsInChildren<Collider>(true));
            HLCreatureBuilder builder = view.GetComponent<HLCreatureBuilder>();
            Assert.NotNull(builder);
            Assert.AreEqual(recipeName, builder.recipe.name);
            SerializedObject data = new SerializedObject(builder);
            Assert.AreEqual("Assets/Render/Look/HLLook_Default.mat",
                AssetDatabase.GetAssetPath(data.FindProperty("_material").objectReferenceValue));
            Assert.AreSame(HLPrimitiveMeshesTests.Meshes(), data.FindProperty("_meshes").objectReferenceValue);
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
            using (HLLianaArm arm = HLLianaArmTests.CreateArm(recipe.arms[0]))
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
        public void CharacterView_ShippedPrefab_HasNoBaseCharacterAndAnchorsOnItself()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/HLHealerCharacter.prefab");

            Assert.AreEqual(PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(prefab));
            Assert.IsNull(prefab.GetComponent<Character>());
            HLCharacterView view = prefab.GetComponent<HLCharacterView>();
            Assert.NotNull(view);
            SerializedObject data = new SerializedObject(view);
            Assert.AreSame(prefab.transform, data.FindProperty("_visualAnchor").objectReferenceValue);
            Assert.AreEqual("HLHealer", data.FindProperty("_recipe").objectReferenceValue.name);
            Assert.AreSame(HLPrimitiveMeshesTests.Meshes(), data.FindProperty("_meshes").objectReferenceValue);
        }
    }
}
