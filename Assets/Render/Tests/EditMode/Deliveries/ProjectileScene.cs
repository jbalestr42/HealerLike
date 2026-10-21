using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Deliveries
{

// A shooter with its model and delivery probe, two targets, a projectile carrying the observer, and a manager
public class ProjectileScene
{
    public GameObject source;
    public GameObject first;
    public GameObject second;
    public GameObject model;
    public GameObject projectileObject;
    public Projectile projectile;
    public ProjectileVisualObserver observer;
    public DeliveryProbe probe;
    public GameObject managerGo;
    public RenderManager manager;
    readonly List<Object> _scriptableObjects = new List<Object>();

    static Entity EntityFixture(GameObject go)
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_targetPoint", go);
        return entity;
    }

    public DataType CreateTracked<DataType>() where DataType : ScriptableObject
    {
        DataType instance = ScriptableObject.CreateInstance<DataType>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    // A positive flat value is damage, a negative one heals
    public ConsumerFactory CreateConsumer(float value)
    {
        ConsumerFactory consumer = CreateTracked<ConsumerFactory>();
        FlatValue flat = new FlatValue();
        flat.data = new FlatValueData { value = value };
        consumer.data = new ConsumerData { value = flat };
        return consumer;
    }

    // An unclaimed shot flies as its own tip
    public bool IsFree()
    {
        FreeShot shot = projectileObject.GetComponent<FreeShot>();
        return shot && shot.enabled;
    }

    // Shoots the projectile again carrying the consumers, the observer reads them in its Init
    public void Shoot(params AConsumerFactory[] consumers)
    {
        projectile.Init(source, first, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>(consumers));
    }

    public void Create()
    {
        source = new GameObject("Source");
        Entity entity = EntityFixture(source);
        first = new GameObject("First");
        EntityFixture(first);
        first.transform.position = Vector3.one;
        second = new GameObject("Second");
        EntityFixture(second);
        second.transform.position = Vector3.right * 2f;
        model = new GameObject("Model");
        model.transform.SetParent(source.transform, false);
        EntityModel entityModel = model.AddComponent<EntityModel>();
        TestHelpers.SetPrivateField(entity, "_model", entityModel);
        probe = model.AddComponent<DeliveryProbe>();
        projectileObject = new GameObject("Projectile", typeof(LineRenderer));
        projectile = projectileObject.AddComponent<Projectile>();
        observer = projectileObject.AddComponent<ProjectileVisualObserver>();
        managerGo = new GameObject("RenderManager");
        manager = managerGo.AddComponent<RenderManager>();
        TestHelpers.SetPrivateField(manager, "_deliveryVocabulary", RenderTestAssets.LoadDeliveryVocabulary());
        TestHelpers.SetPrivateField(manager, "_meshes", RenderTestAssets.LoadMeshes());
        observer.Init(manager, DeliveryStyle.Direct);
        Shoot();
    }

    public void Destroy()
    {
        TestHelpers.InvokePrivate(observer, "OnDestroy");
        foreach (FreeShot shot in projectileObject.GetComponents<FreeShot>())
        {
            TestHelpers.InvokePrivate(shot, "OnDestroy");
        }

        Object.DestroyImmediate(projectileObject);
        Object.DestroyImmediate(managerGo);
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }

        _scriptableObjects.Clear();
    }
}

}
