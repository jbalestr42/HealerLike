using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLProjectileVisualObserverTests
    {
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
    }
}
