using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class TramplePresentationTests
    {
        GameObject _owner;
        GameObject _anchor;
        CharacterView _view;
        TrampleZone _zone;
        CreatureRecipe _recipe;
        Ground _ground;
        readonly BodyCapsule[] _capsules = new BodyCapsule[TrampleZone.MaxCapsules];

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Ground host");
            _anchor = new GameObject("Separate body anchor");
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = _owner.AddComponent<Character>());
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            _view = _owner.AddComponent<CharacterView>();
            TestHelpers.SetPrivateField(_view, "_character", character);
            TestHelpers.SetPrivateField(_view, "_recipe", _recipe);
            TestHelpers.SetPrivateField(_view, "_meshes", RenderTestAssets.LoadMeshes());
            TestHelpers.SetPrivateField(_view, "_material", RenderTestAssets.LoadLookMaterial());
            TestHelpers.SetPrivateField(_view, "_visualAnchor", _anchor.transform);
            TestHelpers.InvokePrivate(_view, "BuildAndRegister");
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            _ground = new Ground();
            _zone = _owner.AddComponent<TrampleZone>();
            _zone.InitFootprint(_ground);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.InvokePrivate(_view, "OnDestroy");
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_anchor);
            Object.DestroyImmediate(_recipe);
            _ground.Dispose();
        }

        [Test]
        public void AppendCapsules_AnchorMovesBeforeViewTick_UsesDetachedWorldPose()
        {
            int count = _zone.AppendCapsules(_capsules, 0);
            Assert.Greater(count, 0);
            Vector3 first = _capsules[0].start;
            Vector3 step = new Vector3(4f, 0f, -3f);
            _anchor.transform.position += step;

            Assert.AreEqual(count, _zone.AppendCapsules(_capsules, 0));
            Assert.That(Vector3.Distance(first + step, _capsules[0].start), Is.LessThan(0.00001f));
            Assert.IsEmpty(_owner.GetComponentsInChildren<MeshFilter>(), "The gameplay hierarchy stays isolated.");
        }

        [Test]
        public void Refresh_AnchorDisabledOrDestroyed_DoesNotBecomeAnObstacleOrLand()
        {
            _anchor.SetActive(false);
            _zone.Refresh();
            Assert.AreEqual(0, _zone.AppendCapsules(_capsules, 0));
            Assert.AreEqual(0, _ground.oneShotCount);
            Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);

            _anchor.SetActive(true);
            Assert.Greater(_zone.AppendCapsules(_capsules, 0), 0);
            _zone.Refresh();
            Assert.AreEqual(1, _zone.landings);
            _ground.Clear();
            Object.DestroyImmediate(_anchor);
            _zone.Refresh();
            Assert.AreEqual(0, _zone.AppendCapsules(_capsules, 0));
            Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            _zone.Refresh();
            Assert.IsNull(_view.rig);
            Assert.AreEqual(0, _ground.bodyCount);
            Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
            Assert.AreEqual(1, _zone.landings);
        }

        [Test]
        public void RejectedRecipeEdit_LandingAndBodyMeshesKeepAcceptedSnapshot()
        {
            List<MeshFilter> before = new List<MeshFilter>();
            _view.rig.CollectBodyMeshes(before);
            int roots = _view.rig.roots.count;
            _recipe.roots.count = -1;
            _recipe.parts = System.Array.Empty<CreaturePart>();
            Assert.IsFalse(_view.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));

            List<MeshFilter> after = new List<MeshFilter>();
            _view.rig.CollectBodyMeshes(after);
            CollectionAssert.AreEqual(before, after);
            TrampleZone.Landing(_ground, _view.rig, Vector3.zero, 1f, new List<Vector3>());
            Assert.AreEqual(roots, _ground.Playing(_ground.vocabulary.footRing));
            Assert.AreEqual(1, _ground.Playing(_ground.vocabulary.bodyRing));
        }

        [Test]
        public void Recompose_SmallerBody_CollectsOnlyAcceptedActivePartsAndRoots()
        {
            _recipe.roots.count = 0;
            _recipe.arms = System.Array.Empty<ArmDefinition>();
            _recipe.parts = new[] { _recipe.parts[0] };
            Assert.IsTrue(_view.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            List<MeshFilter> body = new List<MeshFilter>();
            _view.rig.CollectBodyMeshes(body);
            Assert.AreEqual(1, body.Count);
            Assert.AreEqual(_view.rig.partTransforms[0], body[0].transform);
            _zone.Refresh();
            Assert.AreEqual(1, _zone.AppendCapsules(_capsules, 0));
        }
    }
}
