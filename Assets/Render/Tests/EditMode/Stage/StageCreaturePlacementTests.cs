using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageCreaturePlacementTests
    {
        GameObject _host;
        GameObject _model;
        GameObject _camera;
        EntityData _data;
        EntityGridInteraction _interaction;
        StageCreaturePlacement _placement;
        GameObject _legacy;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Placement fixture");
            _model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject hidden = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hidden.transform.SetParent(_model.transform, false);
            hidden.GetComponent<Renderer>().enabled = false;
            _data = Object.Instantiate(RenderTestAssets.LoadEntity("NormalEntity"));
            _data.model = _model;
            _interaction = new EntityGridInteraction(_data);
            Assert.That(EntityPlacementReadout.TryRead(_interaction, out _, out _legacy, out _), Is.True);
            _camera = new GameObject("Placement camera");
            _camera.transform.rotation = Quaternion.Euler(52f, 90f, 0f);
            _placement = _host.AddComponent<StageCreaturePlacement>();
            _placement.Init(AssetDatabase.LoadAssetAtPath<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset"),
                RenderTestAssets.LoadMeshes(), null, _camera.AddComponent<Camera>(), 1f);
        }

        [TearDown]
        public void TearDown()
        {
            _placement.Clear();
            Object.DestroyImmediate(_legacy);
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(_model);
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void Selection_MasksOnlyLegacyRenderersAndMovementKeepsOneCosmeticRig()
        {
            int entitiesBefore = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length;
            int collidersBefore = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
            _placement.Tick(_interaction, 0f, 0f);
            CreatureRig rig = _placement.preview.rig;
            Assert.That(_placement.data, Is.SameAs(_data));
            foreach (Renderer renderer in _legacy.GetComponentsInChildren<Renderer>()) Assert.That(renderer.enabled, Is.False);
            for (int i = 1; i <= 5; i++)
            {
                _legacy.transform.position = new Vector3(i, 0f, 2f);
                _placement.Tick(_interaction, i * 0.1f, 0.1f);
                Assert.That(_placement.preview.rig, Is.SameAs(rig));
                Assert.That(rig.root.position, Is.EqualTo(_legacy.transform.position));
                Assert.That(rig.appearanceElapsed, Is.EqualTo(i * 0.1f).Within(0.0001f));
            }
            Assert.That(Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length, Is.EqualTo(entitiesBefore));
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length, Is.EqualTo(collidersBefore));
            Assert.That(_host.GetComponentsInChildren<Collider>(), Is.Empty);
            Quaternion facing = rig.root.Find("Sway").rotation;
            Assert.That(Vector3.Dot(facing * Vector3.forward, Vector3.left), Is.GreaterThan(0.95f));
            _placement.Tick(null, 1f, 0f);
            Assert.That(_placement.preview, Is.Null);
            Assert.That(_host.transform.childCount, Is.Zero);
            Assert.That(_legacy.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(_legacy.transform.GetChild(0).GetComponent<Renderer>().enabled, Is.False);
        }

        [Test]
        public void Refresh_DoesNotReplayGrowthAndDisableRestoresTheOriginalModel()
        {
            _placement.Tick(_interaction, 0f, 0.3f);
            _placement.Refresh();
            _placement.Tick(_interaction, 0f, 0f);
            Assert.That(_placement.preview.rig.isAppearing, Is.False);
            TestHelpers.InvokePrivate(_placement, "OnDisable");
            Assert.That(_placement.preview, Is.Null);
            Assert.That(_legacy.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(_legacy.transform.GetChild(0).GetComponent<Renderer>().enabled, Is.False);
        }

        [Test]
        public void NewSelection_ReleasesOldRigAndStartsOneNewAppearance()
        {
            EntityGridInteraction next = new EntityGridInteraction(_data, Entity.EntityType.Computer);
            EntityPlacementReadout.TryRead(next, out _, out GameObject nextModel, out _);
            try
            {
                _placement.Tick(_interaction, 0f, 0.5f);
                Transform previousRoot = _placement.preview.rig.root;
                _placement.Tick(next, 0f, 0f);
                Assert.That(previousRoot == null, Is.True);
                Assert.That(_placement.preview.rig.appearanceElapsed, Is.Zero);
                Assert.That(_legacy.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(nextModel.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(_placement.legacyModel, Is.SameAs(nextModel));
                Assert.That(_host.transform.childCount, Is.EqualTo(1));
                _placement.Clear();
                Assert.That(nextModel.GetComponent<Renderer>().enabled, Is.True);
            }
            finally { Object.DestroyImmediate(nextModel); }
        }

        [Test]
        public void ReplacedSources_RefreshTheActivePreviewWithoutReplayingAppearance()
        {
            CreatureLooks nextLooks = ScriptableObject.CreateInstance<CreatureLooks>();
            PrimitiveMeshes nextMeshes = Object.Instantiate(RenderTestAssets.LoadMeshes());
            nextMeshes.sphere = nextMeshes.pyramid;
            CreatureRecipe authored = RenderTestAssets.CreateRecipe();
            GameObject overrideView = new GameObject("Replacement view");
            CreatureBuilder builder = overrideView.AddComponent<CreatureBuilder>();
            TestHelpers.SetPrivateField(builder, "_recipe", authored);
            TestHelpers.SetPrivateField(builder, "_material", RenderTestAssets.LoadLookMaterial());
            nextLooks.entities[_data] = overrideView;
            try
            {
                _placement.Tick(_interaction, 0f, 0.4f);
                Transform previousRoot = _placement.preview.rig.root;
                _placement.Refresh(nextLooks, nextMeshes);
                _placement.Tick(_interaction, 0f, 0f);
                Assert.That(previousRoot == null, Is.True);
                Assert.That(_placement.preview.rig.recipe, Is.SameAs(authored));
                Assert.That(_placement.preview.rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh,
                    Is.SameAs(nextMeshes.sphere));
                Assert.That(_placement.preview.rig.isAppearing, Is.False);
                Assert.That(_placement.data, Is.SameAs(_data));
                Assert.That(_legacy.GetComponent<Renderer>().enabled, Is.False);
                _placement.Clear();
                Assert.That(_legacy.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(authored != null, Is.True);
            }
            finally
            {
                _placement.Clear();
                Object.DestroyImmediate(overrideView);
                Object.DestroyImmediate(authored);
                Object.DestroyImmediate(nextLooks);
                Object.DestroyImmediate(nextMeshes);
            }
        }

        [Test]
        public void MissingLegacyModel_ClearsPreviewWithoutDestroyingSharedAssets()
        {
            _placement.Tick(_interaction, 0f, 0.1f);
            Object.DestroyImmediate(_legacy);
            _placement.Tick(_interaction, 0f, 0.1f);
            Assert.That(_placement.preview, Is.Null);
            Assert.That(_data != null, Is.True);
            Assert.That(_host.transform.childCount, Is.Zero);
        }
    }
}
