using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class EntityManagerTests
{
    GameObject _go;
    EntityManager _entityManager;
    GameObject _projectilePrefab;

    [SetUp]
    public void SetUp()
    {
        // Standalone component, never going through EntityManager.instance
        _go = new GameObject("EntityManager");
        _entityManager = _go.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");

        _projectilePrefab = new GameObject("Projectile");
        _projectilePrefab.AddComponent<Projectile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_projectilePrefab);
    }

    [Test]
    public void SpawnProjectile_InvokesOnProjectileSpawned_WithTheSpawnedProjectile()
    {
        Projectile spawnedProjectile = null;
        _entityManager.OnProjectileSpawned.AddListener(projectile => spawnedProjectile = projectile);

        GameObject projectileGo = _entityManager.SpawnProjectile(_projectilePrefab, Vector3.zero, Quaternion.identity);

        Assert.AreSame(projectileGo.GetComponent<Projectile>(), spawnedProjectile);
    }

    [Test]
    public void SpawnProjectile_InvokesOnProjectileSpawned_BeforeProjectileInit()
    {
        bool hasSourceWhenSpawned = true;
        _entityManager.OnProjectileSpawned.AddListener(projectile => hasSourceWhenSpawned = projectile.source != null);

        _entityManager.SpawnProjectile(_projectilePrefab, Vector3.zero, Quaternion.identity);

        // Projectile.Init sets the source, and the skills call it after SpawnProjectile returns
        Assert.IsFalse(hasSourceWhenSpawned);
    }
}

}
