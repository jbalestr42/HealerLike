using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreaturePreviewTests
    {
        CreatureLooks _looks;
        GameObject _parent;
        CreaturePreview _preview;

        [SetUp]
        public void SetUp()
        {
            _looks = AssetDatabase.LoadAssetAtPath<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset");
            Assert.That(_looks, Is.Not.Null);
            _parent = new GameObject("Preview fixture");
            _preview = new CreaturePreview();
        }

        [TearDown]
        public void TearDown()
        {
            _preview.Dispose();
            Object.DestroyImmediate(_parent);
        }

        [TestCase(Entity.EntityType.Player)]
        [TestCase(Entity.EntityType.Computer)]
        public void Preview_UsesLiveSideRecipeAndMaterialsWithoutGameplayAndReleasesOwnedRecipe(Entity.EntityType side)
        {
            EntityData data = RenderTestAssets.LoadEntity("NormalEntity");
            Assert.That(_preview.Init(_looks, data, side, RenderTestAssets.LoadMeshes(), _parent.transform, 1f), Is.True);
            CreatureRecipe derived = _preview.rig.recipe;
            CreatureRecipe control = _looks.GetRecipe(data, side);
            try
            {
                Assert.That(derived.parts.Length, Is.EqualTo(control.parts.Length));
                Assert.That(derived.parts[0].dimensions, Is.EqualTo(control.parts[0].dimensions));
                _preview.CompleteAppearance();
                _preview.Tick(0f, 0f, new FootFrame(Vector3.right, Vector3.up, 1f), Vector3.left);
                Assert.That(_preview.rig.isAppearing, Is.False);
                Assert.That(_preview.rig.root.position, Is.EqualTo(Vector3.right));
                Assert.That(_parent.GetComponentsInChildren<Entity>(true), Is.Empty);
                Assert.That(_parent.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(_parent.GetComponentsInChildren<CreatureBuilder>(true), Is.Empty);
                CreatureBuilder host = _looks.GetView(data, side).GetComponentInChildren<CreatureBuilder>(true);
                Renderer renderer = _preview.rig.partTransforms[0].GetComponent<Renderer>();
                Assert.That(renderer.sharedMaterial, Is.SameAs(host.bodyMaterial ? host.bodyMaterial : host.material));
                _preview.Dispose();
                _preview.Dispose();
                Assert.That(derived == null, Is.True);
                Assert.That(_parent.transform.childCount, Is.Zero);
                Assert.That(host.material != null, Is.True);
            }
            finally { Object.DestroyImmediate(control); }
        }

        [Test]
        public void Preview_AuthoredOverrideUsesItsRecipeAndDoesNotDestroyIt()
        {
            CreatureLooks looks = Object.Instantiate(_looks);
            EntityData data = RenderTestAssets.LoadEntity("NormalEntity");
            GameObject host = new GameObject("Authored override");
            CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
            Material material = new Material(RenderTestAssets.LoadLookMaterial());
            try
            {
                CreatureBuilder builder = host.AddComponent<CreatureBuilder>();
                TestHelpers.SetPrivateField(builder, "_recipe", recipe);
                TestHelpers.SetPrivateField(builder, "_material", material);
                TestHelpers.SetPrivateField(builder, "_bodyMaterial", material);
                TestHelpers.SetPrivateField(builder, "_meshes", RenderTestAssets.LoadMeshes());
                looks.entities[data] = host;
                Assert.That(_preview.Init(looks, data, Entity.EntityType.Computer, null, _parent.transform, 1f), Is.True);
                Assert.That(_preview.rig.recipe, Is.SameAs(recipe));
                Assert.That(_preview.rig.partTransforms[0].GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
                _preview.Dispose();
                Assert.That(recipe != null, Is.True);
                Assert.That(material != null, Is.True);
                Assert.That(host != null, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(looks);
            }
        }

        [Test]
        public void Preview_InvalidInputLeavesParentEmptyAndCanBeDisposed()
        {
            Assert.That(_preview.Init(null, null, Entity.EntityType.Player, null, _parent.transform, 1f), Is.False);
            Assert.That(_preview.rig, Is.Null);
            Assert.That(_parent.transform.childCount, Is.Zero);
            _preview.Dispose();
            Assert.That(_preview.Init(_looks, RenderTestAssets.LoadEntity("NormalEntity"), Entity.EntityType.Player,
                RenderTestAssets.LoadMeshes(), _parent.transform, 1f), Is.False);
        }
    }
}
