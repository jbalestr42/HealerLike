using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class StageSpawnObjects
{
    static readonly string projectileFolder = "Assets/Prefabs/Projectiles/";

    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<Object> _created = new List<Object>();

    public static Entity CreateEntity(Transform parent, Entity.EntityType entityType, out Renderer modelRenderer,
        bool withHealth = false)
    {
        GameObject entityGo = new GameObject("Entity");
        entityGo.transform.SetParent(parent, false);
        ResourceAttribute health = withHealth
            ? TestHelpers.CreateResourceAttribute(entityGo, AttributeType.HealthMax, 100) : null;
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = entityGo.AddComponent<Entity>());
        if (health)
        {
            TestHelpers.SetPrivateField(entity, "_health", health);
        }
        entity.entityType = entityType;
        GameObject modelGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        modelGo.transform.SetParent(entityGo.transform, false);
        TestHelpers.SetPrivateField(entity, "_model", modelGo.AddComponent<EntityModel>());
        modelRenderer = modelGo.GetComponent<Renderer>();
        return entity;
    }

    // A projectile as EntityManager.SpawnProjectile hands it out, before Projectile.Init
    public Projectile SpawnProjectile(string prefabName)
    {
        GameObject projectileGo =
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(projectileFolder + prefabName + ".prefab"));
        _spawned.Add(projectileGo);
        return projectileGo.GetComponent<Projectile>();
    }

    // An area as EntityManager.SpawnProjectile hands it out, before its caller sets the source and radius
    public AreaOfEffect SpawnArea()
    {
        GameObject areaGo = new GameObject("Area");
        _spawned.Add(areaGo);
        return areaGo.AddComponent<AreaOfEffect>();
    }

    // What the caller then sets: a source entity whose one on hit consumer takes this value, and the radius
    public void ConfigureArea(AreaOfEffect area, float consumerValue)
    {
        GameObject sourceGo = new GameObject("Source");
        _spawned.Add(sourceGo);
        Entity source = null;
        TestHelpers.WithLoggingDisabled(() => source = sourceGo.AddComponent<Entity>());
        ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
        _created.Add(consumer);
        FlatValue value = new FlatValue();
        value.data = new FlatValueData { value = consumerValue };
        consumer.data = new ConsumerData { value = value };
        source.AddOnHitConsumer(consumer);

        area.source = sourceGo;
        area.radius = 2.5f;
    }

    public void Track(Object value)
    {
        _created.Add(value);
    }

    public void Dispose()
    {
        foreach (GameObject spawnedGo in _spawned)
        {
            Object.DestroyImmediate(spawnedGo);
        }

        _spawned.Clear();
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }

        _created.Clear();
    }
}

}
