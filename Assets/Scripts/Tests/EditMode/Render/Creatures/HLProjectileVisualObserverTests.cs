using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLProjectileVisualObserverTests
    {
        public sealed class HLDeliveryProbe : MonoBehaviour, IHLDeliverySource
        {
            public int begins, updates, contacts, ends;
            public bool accepts = true;
            public HLDeliveryStyle style;
            public bool BeginDelivery(int token, HLDeliveryStyle value, Transform projectile, Vector3 end) { begins++; style = value; return accepts; }
            public void UpdateDelivery(int token, Vector3 position) { updates++; }
            public void ContactDelivery(int token, Vector3 position, GameObject target) { contacts++; }
            public void EndDelivery(int token) { ends++; }
        }
        [Test] public void ObserverDispatchesThroughInterfaceWithoutCreatureBuilder()
        {
            var model = builder.gameObject;
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Object.DestroyImmediate(builder);
            var probe = model.AddComponent<HLDeliveryProbe>();
            TestHelpers.SetPrivateField(observer, "deliveryStyle", HLDeliveryStyle.Arc);
            observer.Init(source);
            Assert.AreEqual(1, probe.begins); Assert.AreEqual(HLDeliveryStyle.Arc, probe.style);
            TestHelpers.InvokePrivate(observer, "LateUpdate"); Assert.AreEqual(1, probe.updates);
            projectile.OnHit.Invoke(new OnHitData { target = first }); Assert.AreEqual(1, probe.contacts);
            observer.enabled = false; TestHelpers.InvokePrivate(observer, "OnDisable");
            Assert.AreEqual(1, probe.ends);
        }
        [Test] public void MissingOrDecliningSourcePreservesOriginalRendererStates()
        {
            var model = builder.gameObject;
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Object.DestroyImmediate(builder);
            observer.Init(source);
            var visible = projectileObject.GetComponent<LineRenderer>();
            Assert.IsTrue(visible.enabled);
            var child = new GameObject("HLHiddenRenderer", typeof(MeshRenderer));
            child.transform.SetParent(projectileObject.transform);
            var hidden = child.GetComponent<Renderer>(); hidden.enabled = false;
            var probe = model.AddComponent<HLDeliveryProbe>(); probe.accepts = false;
            observer.Init(source); TestHelpers.InvokePrivate(observer, "LateUpdate");
            projectile.OnHit.Invoke(new OnHitData { target = first });
            Assert.AreEqual(0, observer.GestureToken); Assert.IsTrue(visible.enabled); Assert.IsFalse(hidden.enabled);
            Assert.AreEqual(0, probe.updates); Assert.AreEqual(0, probe.contacts);
            probe.accepts = true; observer.Init(source);
            Assert.IsFalse(visible.enabled);
            probe.enabled = false; TestHelpers.InvokePrivate(observer, "LateUpdate");
            Assert.IsTrue(visible.enabled); Assert.IsFalse(hidden.enabled); Assert.AreEqual(1, probe.ends);
        }
        [Test] public void DecliningAdapterDoesNotPreventAnotherAdapterPresenting()
        {
            var model = builder.gameObject;
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Object.DestroyImmediate(builder);
            model.AddComponent<HLDeliveryProbe>().accepts = false;
            var accepted = model.AddComponent<HLDeliveryProbe>();
            observer.Init(source);
            Assert.AreEqual(1, accepted.begins); Assert.AreNotEqual(0, observer.GestureToken);
            Assert.IsFalse(projectileObject.GetComponent<LineRenderer>().enabled);
        }
        GameObject source, first, second, projectileObject;
        Projectile projectile;
        HLProjectileVisualObserver observer;
        HLCreatureRecipe recipe;
        Material material;
        HLCreatureBuilder builder;
        static Entity EntityFixture(GameObject go)
        {
            Entity e = null; TestHelpers.WithLoggingDisabled(() => e = go.AddComponent<Entity>());
            TestHelpers.SetPrivateField(e, "_targetPoint", go); return e;
        }
        [SetUp] public void Setup()
        {
            source = new GameObject("HLSource"); var entity = EntityFixture(source);
            first = new GameObject("HLFirst"); EntityFixture(first); first.transform.position = Vector3.one;
            second = new GameObject("HLSecond"); EntityFixture(second); second.transform.position = Vector3.right * 2;
            var model = new GameObject("HLModel"); model.transform.SetParent(source.transform, false);
            var entityModel = model.AddComponent<EntityModel>(); TestHelpers.SetPrivateField(entity, "_model", entityModel);
            recipe = HLCreatureValidatorTests.Recipe(); material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            builder = model.AddComponent<HLCreatureBuilder>(); builder.SetRecipe(recipe, material); builder.Init(entity);
            projectileObject = new GameObject("HLProjectile", typeof(LineRenderer)); projectile = projectileObject.AddComponent<Projectile>();
            observer = projectileObject.AddComponent<HLProjectileVisualObserver>();
            projectile.Init(source, first, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
        }
        [TearDown] public void Cleanup()
        {
            if (observer) TestHelpers.InvokePrivate(observer, "OnDestroy");
            if (builder) TestHelpers.InvokePrivate(builder, "OnDestroy");
            Object.DestroyImmediate(projectileObject); Object.DestroyImmediate(source); Object.DestroyImmediate(first); Object.DestroyImmediate(second);
            Object.DestroyImmediate(recipe); Object.DestroyImmediate(material); HLPrimitiveMeshes.ReleaseAll();
        }
        [Test] public void InitBindsBeforeSynchronousHitsAndPreservesOrderedTargets()
        {
            Assert.AreSame(first, observer.CapturedTarget); Assert.AreSame(first, observer.CapturedTargetPoint); Assert.AreNotEqual(0, observer.GestureToken);
            TestHelpers.SetPrivateField(observer, "preserveContactPath", true);
            projectile.OnHit.Invoke(new OnHitData { source = source, target = first });
            projectile.OnHit.Invoke(new OnHitData { source = source, target = second });
            projectile.SetTarget(null);
            Assert.AreEqual(2, observer.Contacts.Count); Assert.AreSame(first, observer.Contacts[0].target); Assert.AreSame(second, observer.Contacts[1].target);
            Assert.AreEqual(first.transform.position, observer.Contacts[0].position); Assert.AreEqual(2, builder.Rig.ActiveArmCount);
            Assert.IsFalse(projectileObject.GetComponent<LineRenderer>().enabled); Assert.IsTrue(projectile.enabled);
        }
        [Test] public void LateRetargetDoesNotOverwriteContactAndDisableUnsubscribes()
        {
            projectile.OnHit.AddListener(_ => projectile.SetTarget(second));
            projectile.OnHit.Invoke(new OnHitData { target = first }); TestHelpers.InvokePrivate(observer, "LateUpdate");
            Assert.AreSame(first, observer.Contacts[0].target); Assert.AreSame(second, projectile.target);
            observer.enabled = false; TestHelpers.InvokePrivate(observer, "OnDisable"); projectile.OnHit.Invoke(new OnHitData { target = second });
            Assert.AreEqual(1, observer.Contacts.Count); Assert.AreEqual(0, observer.GestureToken);
        }
        [Test] public void ReinitDoesNotDuplicateListenerOrMoveProjectile()
        {
            Vector3 position = projectile.transform.position;
            observer.Init(source); observer.Init(source); projectile.OnHit.Invoke(new OnHitData { target = first });
            Assert.AreEqual(1, observer.Contacts.Count); Assert.AreEqual(position, projectile.transform.position);
            Assert.AreSame(first, projectile.target);
        }
        [Test] public void ExecutionOrderPrecedesBuilder()
        {
            var observerOrder = (DefaultExecutionOrder)System.Attribute.GetCustomAttribute(typeof(HLProjectileVisualObserver), typeof(DefaultExecutionOrder));
            var builderOrder = (DefaultExecutionOrder)System.Attribute.GetCustomAttribute(typeof(HLCreatureBuilder), typeof(DefaultExecutionOrder));
            Assert.Less(observerOrder.order, builderOrder.order);
        }
        [Test] public void AuthoredThrownStyleIsRejectedWithoutMovingProjectile()
        {
            TestHelpers.SetPrivateField(observer, "deliveryStyle", HLDeliveryStyle.Thrown);
            observer.Init(source); Assert.AreEqual(0, observer.GestureToken);
            Assert.AreEqual(HLDeliveryStyle.Thrown, observer.DeliveryStyle);
            Assert.IsTrue(projectileObject.GetComponent<LineRenderer>().enabled);
            Assert.AreEqual(Vector3.zero, projectile.transform.position);
        }
    }
}
