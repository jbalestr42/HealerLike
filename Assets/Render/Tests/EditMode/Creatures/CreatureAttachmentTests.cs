using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Creatures
{
    public class CreatureAttachmentTests : CreatureMeshOwnershipFixture
    {
        CreatureBuilder BuildHost()
        {
            _pool.Dispose();
            _rig.Dispose();
            Entity entity = _owner.GetComponent<Entity>();
            if (!entity)
            {
                entity = RenderTestAssets.CreateStoneEntity(_owner, null);
            }
            CreatureBuilder host = _owner.AddComponent<CreatureBuilder>();
            RenderTestAssets.SetRecipe(host, _recipe, _material, _meshes);
            host.Init(entity);
            return host;
        }

        [Test]
        public void SourceMovedAfterInit_AnchorsAndDiscoverySynchronizeWithoutWaitingForAFrame()
        {
            _owner.transform.SetPositionAndRotation(Vector3.one, Quaternion.Euler(0f, 30f, 0f));
            _owner.transform.localScale = Vector3.one * 2f;
            CreatureBuilder host = BuildHost();
            Assert.IsTrue(host.TryGetAnchors(out EffectAnchors before));
            Vector3 displacement = new Vector3(17f, 3f, -11f);
            _owner.transform.position += displacement;
            Assert.IsTrue(host.TryGetAnchors(out EffectAnchors after));
            Assert.That(Vector3.Distance(before.bodyCentre + displacement, after.bodyCentre), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(Vector3.one, host.rig.root.lossyScale), Is.LessThan(0.0001f));
            Assert.IsFalse(host.rig.root.IsChildOf(_owner.transform));
            Assert.IsEmpty(_owner.GetComponentsInChildren<Renderer>(true));
            Assert.IsNotEmpty(CreatureRenderers.Find(_owner.transform));
            Assert.AreSame(host, _owner.GetComponentInChildren<CreatureBuilder>());
            _owner.transform.localScale = Vector3.one * 3f;
            _owner.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
            TestHelpers.InvokePrivate(host, "LateUpdate");
            Assert.That(Vector3.Distance(Vector3.one, host.rig.root.lossyScale), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(Quaternion.identity, host.rig.root.rotation), Is.LessThan(0.0001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisableAndReenable_HidesAllGeometryImmediatelyAndReusesTheLiveRig(bool wholeObject)
        {
            StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            CreatureBuilder host = BuildHost();
            CreatureRig rig = host.rig;
            shadow.Show(true);
            Assert.IsTrue(shadow.gameObject.activeInHierarchy);
            if (wholeObject)
            {
                _owner.SetActive(false);
            }
            else
            {
                host.enabled = false;
            }
            TestHelpers.InvokePrivate(host, "OnDisable");
            Assert.IsFalse(host.presentation.gameObject.activeInHierarchy);
            Assert.IsFalse(shadow.gameObject.activeInHierarchy);
            Assert.IsFalse(rig.root.gameObject.activeInHierarchy);
            if (wholeObject)
            {
                _owner.SetActive(true);
            }
            else
            {
                host.enabled = true;
            }
            TestHelpers.InvokePrivate(host, "OnEnable");
            Assert.AreSame(rig, host.rig);
            Assert.IsTrue(rig.root.gameObject.activeInHierarchy);
            Assert.IsTrue(shadow.gameObject.activeInHierarchy);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Destroy_ReleasesOwnedShadowAndGeometryAndAllowsRepeatedDisposal(bool wholeObject)
        {
            StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            CreatureBuilder host = BuildHost();
            StoneBody body = _owner.AddComponent<StoneBody>();
            TestHelpers.SetPrivateField(body, "_groundShadow", shadow);
            body.Init(null, 1, null);
            Transform presentation = host.presentation;
            TestHelpers.InvokePrivate(host, "OnDestroy");
            TestHelpers.InvokePrivate(host, "OnDestroy");
            Assert.IsFalse(presentation);
            Assert.IsFalse(shadow);
            Assert.DoesNotThrow(() => body.RefreshRig());
            Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(body, "LateUpdate"));
            if (wholeObject)
            {
                Object.DestroyImmediate(_owner);
                _owner = null;
            }
            else
            {
                Object.DestroyImmediate(host);
                CreatureBuilder next = BuildHost();
                Assert.IsTrue(next.rig.root);
            }
            Assert.IsTrue(_source);
        }

        [Test]
        public void ModelRemoved_SourceEntityAndColliderRemainButItsPresentationIsReleased()
        {
            Entity entity = RenderTestAssets.CreateStoneEntity(_owner, null);
            BoxCollider collider = _owner.AddComponent<BoxCollider>();
            GameObject model = new GameObject("Model");
            model.transform.SetParent(_owner.transform, false);
            CreatureBuilder host = model.AddComponent<CreatureBuilder>();
            RenderTestAssets.SetRecipe(host, _recipe, _material, _meshes);
            host.Init(entity);
            Transform presentation = host.presentation;
            TestHelpers.InvokePrivate(host, "OnDisable");
            Assert.IsFalse(presentation.gameObject.activeInHierarchy);
            TestHelpers.InvokePrivate(host, "OnDestroy");
            Object.DestroyImmediate(model);
            Assert.IsFalse(presentation);
            Assert.IsTrue(entity);
            Assert.IsTrue(collider.enabled);
        }

        [Test]
        public void StoneBodyComponentRemoved_ReleasesItsShadowWithoutDestroyingTheLiveRig()
        {
            StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            CreatureBuilder host = BuildHost();
            StoneBody body = _owner.AddComponent<StoneBody>();
            TestHelpers.SetPrivateField(body, "_groundShadow", shadow);
            body.Init(null, 1, null);
            TestHelpers.InvokePrivate(body, "OnDestroy");
            Object.DestroyImmediate(body);
            Assert.IsFalse(shadow);
            Assert.IsTrue(host.rig.root);
            Assert.IsTrue(_source);
        }

        [Test]
        public void SceneUnload_AlsoOwnsDetachedGeometryInTheSourceScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.MoveGameObjectToScene(_owner, scene);
                using (CreatureAttachment presentation = new CreatureAttachment(_owner.transform))
                {
                    GameObject geometry = new GameObject("Scene-owned geometry", typeof(MeshRenderer));
                    presentation.Take(geometry.transform);
                    Transform root = presentation.root;
                    Assert.AreEqual(scene, root.gameObject.scene);
                    Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
                    Assert.IsFalse(root);
                    Assert.IsFalse(geometry);
                }
                _owner = null;
                Assert.IsTrue(_source);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void DetachedRig_UsesTheOriginalSourceIdentityForIdleAndPalette()
        {
            _recipe.idle.seed = 39;
            _recipe.idle.swayDegrees = 5f;
            _recipe.idle.swayFrequency = 0.7f;
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
            using (CreatureRig direct = new CreatureRig())
            {
                Assert.IsTrue(direct.Init(_recipe, _owner.transform, _material, _material, _meshes, 1f));
                FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
                _rig.Tick(2.3f, 0.1f, frame);
                direct.Tick(2.3f, 0.1f, frame);
                Assert.That(Quaternion.Angle(_rig.armRotation, direct.armRotation), Is.LessThan(0.0001f));
                MaterialPropertyBlock detachedColour = new MaterialPropertyBlock();
                MaterialPropertyBlock directColour = new MaterialPropertyBlock();
                _rig.partTransforms[0].GetComponent<Renderer>().GetPropertyBlock(detachedColour, 0);
                direct.partTransforms[0].GetComponent<Renderer>().GetPropertyBlock(directColour, 0);
                Assert.AreEqual(directColour.GetColor("_BaseColor"), detachedColour.GetColor("_BaseColor"));
            }
        }
    }
}
