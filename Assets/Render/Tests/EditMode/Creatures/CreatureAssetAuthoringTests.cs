using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class CreatureAssetAuthoringTests
{
    static readonly string root = "Assets/Render/Creatures/";

    GameObject _parent;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("RecipeFixture");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_parent);
    }

    [TestCase("BladeRosette")]
    [TestCase("Healer")]
    [TestCase("SpiralFern")]
    [TestCase("HangingArch")]
    [TestCase("SphereStack")]
    public void Recipe_ShippedAsset_ValidatesAndBuildsFromBakedMeshes(string name)
    {
        string path = root + "Data/" + name + ".asset";
        CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
        Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
        Assert.That(recipe.roots.count, Is.InRange(6, 8));
        if (name == "Healer")
        {
            Part bulb = System.Array.Find(recipe.parts, part => part.id == "Bulb");
            Part crown = System.Array.Find(recipe.parts, part => part.id == "Crown");
            Assert.AreEqual(Primitive.Cone, bulb.primitive);
            Assert.AreEqual(Primitive.Torus, crown.primitive);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        using (CreatureRig rig = CreatureRigTests.CreateRig(recipe, _parent.transform, material))
        {
            rig.Tick(1f, 0.016f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            if (name == "Healer")
            {
                Transform crown = rig.root.Find("Sway/Stem/Crown");
                Quaternion before = crown.localRotation;
                rig.Tick(11f, 0.016f, new FootFrame(Vector3.zero, Vector3.up, 1f));
                Assert.That(Quaternion.Angle(before, crown.localRotation), Is.EqualTo(180).Within(0.01f));
            }

            // Body parts share the baked meshes, only the arm chains are generated per rig
            foreach (MeshFilter filter in _parent.GetComponentsInChildren<MeshFilter>())
            {
                bool isChain = filter.name == "LianaArm";
                Assert.AreEqual(!isChain, AssetDatabase.Contains(filter.sharedMesh), filter.name);
            }

            Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
        }
    }

    [TestCase("Normal", "SpiralFern")]
    [TestCase("Test", "SphereStack")]
    [TestCase("Swarm", "HangingArch")]
    [TestCase("FastShoot", "SpiralFern")]
    [TestCase("TripleShoot", "HangingArch")]
    [TestCase("MultiShot", "HangingArch")]
    [TestCase("RandomShoot", "SpiralFern")]
    [TestCase("ChainLightning", "SphereStack")]
    [TestCase("Channeling", "SphereStack")]
    [TestCase("Soldier", "SphereStack")]
    [TestCase("HitArmorBuffer", "BladeRosette")]
    public void View_ShippedPrefab_HasNoBaseModelAndCarriesItsLook(string name, string recipeName)
    {
        GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/" + name + ".prefab");

        Assert.NotNull(view);
        Assert.AreEqual(PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(view));
        Assert.IsNull(view.GetComponent<EntityModel>());
        Assert.IsEmpty(view.GetComponentsInChildren<Renderer>(true));
        Assert.IsEmpty(view.GetComponentsInChildren<Collider>(true));
        CreatureBuilder builder = view.GetComponent<CreatureBuilder>();
        Assert.NotNull(builder);
        Assert.AreEqual(recipeName, builder.recipe.name);
        SerializedObject data = new SerializedObject(builder);
        Assert.AreEqual("Assets/Render/Look/Look_Default.mat",
            AssetDatabase.GetAssetPath(data.FindProperty("_material").objectReferenceValue));
        Assert.AreSame(PrimitiveMeshesTests.Meshes(), data.FindProperty("_meshes").objectReferenceValue);
    }

    [TestCase("BladeRosette")]
    [TestCase("Healer")]
    [TestCase("SpiralFern")]
    [TestCase("HangingArch")]
    [TestCase("SphereStack")]
    public void Arm_ShippedRecipe_ReachesAcrossBoardAndClampsOutsideIt(string name)
    {
        string path = root + "Data/" + name + ".asset";
        CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
        using (LianaArm arm = LianaArmTests.CreateArm(recipe.arms[0]))
        {
            arm.Tick(0f, Vector3.zero, Quaternion.identity);
            Vector3 target = new Vector3(15f, 4f, 15f);
            arm.Begin(1, GestureKind.Attack, target);
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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/HealerCharacter.prefab");

        Assert.AreEqual(PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(prefab));
        Assert.IsNull(prefab.GetComponent<Character>());
        CharacterView view = prefab.GetComponent<CharacterView>();
        Assert.NotNull(view);
        SerializedObject data = new SerializedObject(view);
        Assert.AreSame(prefab.transform, data.FindProperty("_visualAnchor").objectReferenceValue);
        Assert.AreEqual("Healer", data.FindProperty("_recipe").objectReferenceValue.name);
        Assert.AreSame(PrimitiveMeshesTests.Meshes(), data.FindProperty("_meshes").objectReferenceValue);
    }
}

}
