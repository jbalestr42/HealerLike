using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class ProceduralRigTests
    {
        GameObject _parent;
        CreatureRecipe _recipe;
        CreatureRecipe _replacement;
        CreatureRig _rig;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("ProceduralRigTest");
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.parts[0].shape = ShapeProfile.Bulb();
            _recipe.roots.segmentShape = ShapeProfile.Segment();
            _recipe.roots.jointShape = ShapeProfile.Bulb(0.7f);
            _material = new Material(RenderTestAssets.LoadLookMaterial());
            _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_replacement);
            Object.DestroyImmediate(_material);
        }

        Mesh BodyMesh() => _rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;

        [Test]
        public void Recompose_ChangedProfile_ReplacesGeometryAndRetainsAnchors()
        {
            Transform body = _rig.partTransforms[0];
            Transform root = _rig.root;
            Mesh previous = BodyMesh();
            _replacement = Object.Instantiate(_recipe);
            _replacement.parts[0].shape = ShapeProfile.Bulb(2f);
            Vector3[] before = previous.vertices;
            Assert.IsTrue(_rig.Recompose(_replacement, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.IsFalse(previous);
            Assert.AreSame(root, _rig.root);
            Assert.AreSame(body, _rig.partTransforms[0]);
            CollectionAssert.AreNotEqual(before, BodyMesh().vertices);
        }

        [Test]
        public void Recompose_InvalidProfile_KeepsTheLiveRecipeAndMeshes()
        {
            _replacement = Object.Instantiate(_recipe);
            ShapeProfile invalid = ShapeProfile.Leaf();
            invalid.bend = float.NaN;
            _replacement.parts[0].shape = invalid;
            Mesh previous = BodyMesh();
            int revision = _rig.revision;
            Assert.IsFalse(_rig.Recompose(_replacement, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.AreSame(_recipe, _rig.recipe);
            Assert.AreSame(previous, BodyMesh());
            Assert.IsTrue(previous);
            Assert.AreEqual(revision, _rig.revision);
        }

        [Test]
        public void Recompose_MatchingParts_UseOneGeneratedMeshAndKeepBakedFallbacks()
        {
            _replacement = Object.Instantiate(_recipe);
            CreaturePart second = _recipe.parts[0];
            second.id = "Second";
            second.parent = 0;
            second.localPosition = Vector3.up;
            CreaturePart baked = second;
            baked.id = "Baked";
            baked.shape = default;
            _replacement.parts = new[] { _recipe.parts[0], second, baked };
            Assert.IsTrue(_rig.Recompose(_replacement, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.AreSame(BodyMesh(), _rig.partTransforms[1].GetComponent<MeshFilter>().sharedMesh);
            Mesh legacy = _rig.partTransforms[2].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(RenderTestAssets.LoadMeshes().sphere, legacy);
            _rig.Dispose();
            Assert.IsTrue(legacy);
        }

        [Test]
        public void Dispose_ReleasesBodyAndRootMeshesWithoutTouchingBakedAssets()
        {
            Mesh body = BodyMesh();
            Mesh segment = _rig.root.Find("Root").GetComponent<MeshFilter>().sharedMesh;
            Mesh joint = _rig.root.Find("RootJoint").GetComponent<MeshFilter>().sharedMesh;
            Mesh baked = RenderTestAssets.LoadMeshes().sphere;
            Assert.IsTrue(body);
            Assert.IsTrue(segment);
            Assert.IsTrue(joint);
            _rig.Dispose();
            _rig.Dispose();
            Assert.IsFalse(body);
            Assert.IsFalse(segment);
            Assert.IsFalse(joint);
            Assert.IsTrue(baked);
        }

        [Test]
        public void Tick_ProceduralAssembly_DoesNotGenerateOrReplaceMeshes()
        {
            Mesh body = BodyMesh();
            FootFrame ground = new FootFrame(Vector3.zero, Vector3.up, 1f);
            for (int i = 0; i < 20; i++)
            {
                _rig.Tick(i * 0.016f, 0.016f, ground);
            }
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                _rig.Tick(i * 0.016f, 0.016f, ground);
            }
            Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
            Assert.AreSame(body, BodyMesh());
        }

        [Test]
        public void Recompose_InvalidRootProfile_LeavesCurrentRootsAlive()
        {
            _replacement = Object.Instantiate(_recipe);
            ShapeProfile invalid = ShapeProfile.Segment();
            invalid.radialSegments = 0;
            _replacement.roots.segmentShape = invalid;
            Mesh segment = _rig.root.Find("Root").GetComponent<MeshFilter>().sharedMesh;
            Assert.IsFalse(_rig.Recompose(_replacement, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.IsTrue(segment);
            Assert.AreSame(_recipe, _rig.recipe);
        }

        [TestCase(0.022f)]
        [TestCase(0.8f)]
        public void Tick_BentRootProfiles_KeepActualMeshEndpointsJoinedAndFeetPlanted(float thickness)
        {
            _replacement = Object.Instantiate(_recipe);
            _replacement.idle = default;
            ShapeProfile bent = ShapeProfile.Segment(0.7f, 0.3f, 0.9f);
            _replacement.roots.segmentShape = bent;
            _replacement.roots.thickness = thickness;
            Assert.IsTrue(_rig.Recompose(_replacement, _material, _material, RenderTestAssets.LoadMeshes()));
            _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            Vector3 lower = ProceduralShapeMeshes.Anchor(bent, ShapeAnchor.Bottom);
            Vector3 upper = ProceduralShapeMeshes.Anchor(bent, ShapeAnchor.Top);
            Vector3 previous = new Vector3(0.08f, _replacement.roots.hipHeight, 0f);
            int seen = 0;
            foreach (Transform child in _rig.root)
            {
                if (child.name != "Root")
                {
                    continue;
                }
                Assert.That(Vector3.Distance(previous, child.TransformPoint(lower)), Is.LessThan(0.0001f));
                previous = child.TransformPoint(upper);
                if (++seen == _replacement.roots.segments)
                {
                    break;
                }
            }
            Assert.AreEqual(_replacement.roots.segments, seen);
            Assert.That(Vector3.Distance(new Vector3(_replacement.roots.footRadius, 0f, 0f), previous),
                Is.LessThan(0.0001f));
        }
    }
}
