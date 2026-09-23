using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    public class ProjectileVisualObserverTests
    {
        public class DeliveryProbe : MonoBehaviour, IDeliverySource
        {
            public int begins;
            public int updates;
            public int contacts;
            public int ends;
            public bool accepts = true;
            public DeliveryStyle style;

            public bool BeginDelivery(int token, DeliveryStyle value, Transform projectile, Vector3 end)
            {
                begins++;
                style = value;
                return accepts;
            }

            public void UpdateDelivery(int token, Vector3 position)
            {
                updates++;
            }

            public void ContactDelivery(int token, Vector3 position, GameObject target)
            {
                contacts++;
            }

            public void EndDelivery(int token)
            {
                ends++;
            }
        }

        GameObject _source;
        GameObject _first;
        GameObject _second;
        GameObject _projectileObject;
        Projectile _projectile;
        ProjectileVisualObserver _observer;
        CreatureRecipe _recipe;
        Material _material;
        CreatureBuilder _builder;

        static Entity EntityFixture(GameObject go)
        {
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            TestHelpers.SetPrivateField(entity, "_targetPoint", go);
            return entity;
        }

        GameObject _managerGo;

        [SetUp]
        public void Setup()
        {
            _source = new GameObject("HLSource");
            Entity entity = EntityFixture(_source);
            _first = new GameObject("HLFirst");
            EntityFixture(_first);
            _first.transform.position = Vector3.one;
            _second = new GameObject("HLSecond");
            EntityFixture(_second);
            _second.transform.position = Vector3.right * 2f;
            GameObject model = new GameObject("HLModel");
            model.transform.SetParent(_source.transform, false);
            EntityModel entityModel = model.AddComponent<EntityModel>();
            TestHelpers.SetPrivateField(entity, "_model", entityModel);
            _recipe = CreatureValidatorTests.Recipe();
            _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
            _builder = model.AddComponent<CreatureBuilder>();
            _builder.SetRecipe(_recipe, _material, PrimitiveMeshesTests.Meshes());
            _builder.Init(entity);
            _projectileObject = new GameObject("HLProjectile", typeof(LineRenderer));
            _projectile = _projectileObject.AddComponent<Projectile>();
            _observer = _projectileObject.AddComponent<ProjectileVisualObserver>();
            _managerGo = new GameObject("HLRenderManager");
            _observer.Init(_managerGo.AddComponent<RenderManager>(), null);
            _projectile.Init(_source, _first, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
        }

        [TearDown]
        public void Cleanup()
        {
            if (_observer)
            {
                TestHelpers.InvokePrivate(_observer, "OnDestroy");
            }

            if (_builder)
            {
                TestHelpers.InvokePrivate(_builder, "OnDestroy");
            }

            Object.DestroyImmediate(_projectileObject);
            Object.DestroyImmediate(_managerGo);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void ObserverDispatchesThroughInterfaceWithoutCreatureBuilder()
        {
            GameObject model = _builder.gameObject;
            TestHelpers.InvokePrivate(_builder, "OnDestroy");
            Object.DestroyImmediate(_builder);
            DeliveryProbe probe = model.AddComponent<DeliveryProbe>();
            TestHelpers.SetPrivateField(_observer, "_deliveryStyle", DeliveryStyle.Arc);

            _observer.Init(_source);

            Assert.AreEqual(1, probe.begins);
            Assert.AreEqual(DeliveryStyle.Arc, probe.style);
            TestHelpers.InvokePrivate(_observer, "LateUpdate");
            Assert.AreEqual(0, probe.updates); // the source follows the projectile itself
            _projectile.OnHit.Invoke(new OnHitData { target = _first });
            Assert.AreEqual(1, probe.contacts);
            _observer.enabled = false;
            TestHelpers.InvokePrivate(_observer, "OnDisable");
            Assert.AreEqual(1, probe.ends);
        }

        [Test]
        public void MissingOrDecliningSourcePreservesOriginalRendererStates()
        {
            GameObject model = _builder.gameObject;
            TestHelpers.InvokePrivate(_builder, "OnDestroy");
            Object.DestroyImmediate(_builder);
            _observer.Init(_source);
            LineRenderer visible = _projectileObject.GetComponent<LineRenderer>();
            Assert.IsTrue(visible.enabled);
            GameObject child = new GameObject("HLHiddenRenderer", typeof(MeshRenderer));
            child.transform.SetParent(_projectileObject.transform);
            Renderer hidden = child.GetComponent<Renderer>();
            hidden.enabled = false;
            DeliveryProbe probe = model.AddComponent<DeliveryProbe>();
            probe.accepts = false;

            _observer.Init(_source);
            TestHelpers.InvokePrivate(_observer, "LateUpdate");
            _projectile.OnHit.Invoke(new OnHitData { target = _first });

            Assert.AreEqual(0, _observer.gestureToken);
            Assert.IsTrue(visible.enabled);
            Assert.IsFalse(hidden.enabled);
            Assert.AreEqual(0, probe.updates);
            Assert.AreEqual(0, probe.contacts);
            probe.accepts = true;
            _observer.Init(_source);
            Assert.IsFalse(visible.enabled);
            probe.enabled = false;
            TestHelpers.InvokePrivate(_observer, "LateUpdate");
            Assert.IsTrue(visible.enabled);
            Assert.IsFalse(hidden.enabled);
            Assert.AreEqual(1, probe.ends);
        }

        [Test]
        public void DecliningAdapterDoesNotPreventAnotherAdapterPresenting()
        {
            GameObject model = _builder.gameObject;
            TestHelpers.InvokePrivate(_builder, "OnDestroy");
            Object.DestroyImmediate(_builder);
            model.AddComponent<DeliveryProbe>().accepts = false;
            DeliveryProbe accepted = model.AddComponent<DeliveryProbe>();

            _observer.Init(_source);

            Assert.AreEqual(1, accepted.begins);
            Assert.AreNotEqual(0, _observer.gestureToken);
            Assert.IsFalse(_projectileObject.GetComponent<LineRenderer>().enabled);
        }

        [Test]
        public void InitBindsBeforeSynchronousHitsAndPreservesOrderedTargets()
        {
            Assert.AreSame(_first, _observer.capturedTarget);
            Assert.AreSame(_first, _observer.capturedTargetPoint);
            Assert.AreNotEqual(0, _observer.gestureToken);
            TestHelpers.SetPrivateField(_observer, "_preserveContactPath", true);

            _projectile.OnHit.Invoke(new OnHitData { source = _source, target = _first });
            _projectile.OnHit.Invoke(new OnHitData { source = _source, target = _second });
            _projectile.SetTarget(null);

            Assert.AreEqual(2, _observer.contacts.Count);
            Assert.AreSame(_first, _observer.contacts[0].target);
            Assert.AreSame(_second, _observer.contacts[1].target);
            Assert.AreEqual(_first.transform.position, _observer.contacts[0].position);
            Assert.AreEqual(2, _builder.rig.activeArmCount);
            Assert.IsFalse(_projectileObject.GetComponent<LineRenderer>().enabled);
            Assert.IsTrue(_projectile.enabled);
        }

        [Test]
        public void LateRetargetDoesNotOverwriteContactAndDisableUnsubscribes()
        {
            _projectile.OnHit.AddListener(_ => _projectile.SetTarget(_second));

            _projectile.OnHit.Invoke(new OnHitData { target = _first });
            TestHelpers.InvokePrivate(_observer, "LateUpdate");

            Assert.AreSame(_first, _observer.contacts[0].target);
            Assert.AreSame(_second, _projectile.target);
            _observer.enabled = false;
            TestHelpers.InvokePrivate(_observer, "OnDisable");
            _projectile.OnHit.Invoke(new OnHitData { target = _second });
            Assert.AreEqual(1, _observer.contacts.Count);
            Assert.AreEqual(0, _observer.gestureToken);
        }

        [Test]
        public void ReinitDoesNotDuplicateListenerOrMoveProjectile()
        {
            Vector3 position = _projectile.transform.position;

            _observer.Init(_source);
            _observer.Init(_source);
            _projectile.OnHit.Invoke(new OnHitData { target = _first });

            Assert.AreEqual(1, _observer.contacts.Count);
            Assert.AreEqual(position, _projectile.transform.position);
            Assert.AreSame(_first, _projectile.target);
        }

        [Test]
        public void Init_WithManager_TakesTokensFromManager()
        {
            GameObject managerGo = new GameObject("HLRenderManager");
            RenderManager manager = managerGo.AddComponent<RenderManager>();
            int previous = manager.NextDeliveryToken();
            _observer.Init(manager, null);

            _observer.Init(_source);

            Assert.AreEqual(previous + 1, _observer.gestureToken);
            Object.DestroyImmediate(managerGo);
        }

        [Test]
        public void AuthoredThrownStyleIsRejectedWithoutMovingProjectile()
        {
            TestHelpers.SetPrivateField(_observer, "_deliveryStyle", DeliveryStyle.Thrown);

            _observer.Init(_source);

            Assert.AreEqual(0, _observer.gestureToken);
            Assert.AreEqual(DeliveryStyle.Thrown, _observer.deliveryStyle);
            Assert.IsTrue(_projectileObject.GetComponent<LineRenderer>().enabled);
            Assert.AreEqual(Vector3.zero, _projectile.transform.position);
        }
    }
}
