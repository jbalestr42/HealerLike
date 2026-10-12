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
    public void FindSummons_KeepsOnlyTheEntitiesWithTheSummonTag()
    {
        GameObject placed = new GameObject("Placed");
        GameObject summoned = new GameObject("Summoned");
        GameObject tagged = new GameObject("Tagged");
        GameplayTag summonTag = ScriptableObject.CreateInstance<GameplayTag>();
        GameplayTag otherTag = ScriptableObject.CreateInstance<GameplayTag>();
        try
        {
            // Adding Entity triggers Entity.Reset() (NREs without a full Init())
            TestHelpers.WithLoggingDisabled(() =>
            {
                placed.AddComponent<Entity>();
                summoned.AddComponent<Entity>().AddTag(summonTag);
                tagged.AddComponent<Entity>().AddTag(otherTag);
            });

            CollectionAssert.AreEqual(new[] { summoned }, EntityManager.FindSummons(new System.Collections.Generic.List<GameObject> { placed, summoned, tagged, null }, summonTag));
        }
        finally
        {
            Object.DestroyImmediate(placed);
            Object.DestroyImmediate(summoned);
            Object.DestroyImmediate(tagged);
            Object.DestroyImmediate(summonTag);
            Object.DestroyImmediate(otherTag);
        }
    }

    [Test]
    public void SpawnProjectile_InvokesOnProjectileSpawned_WithTheSpawnedGameObject()
    {
        GameObject spawnedGo = null;
        _entityManager.OnProjectileSpawned.AddListener(go => spawnedGo = go);

        GameObject projectileGo = _entityManager.SpawnProjectile(_projectilePrefab, Vector3.zero, Quaternion.identity);

        Assert.AreSame(projectileGo, spawnedGo);
    }

    [Test]
    public void SpawnProjectile_InvokesOnProjectileSpawned_BeforeProjectileInit()
    {
        bool hasSourceWhenSpawned = true;
        _entityManager.OnProjectileSpawned.AddListener(go => hasSourceWhenSpawned = go.GetComponent<Projectile>().source != null);

        _entityManager.SpawnProjectile(_projectilePrefab, Vector3.zero, Quaternion.identity);

        // Projectile.Init sets the source, and the skills call it after SpawnProjectile returns
        Assert.IsFalse(hasSourceWhenSpawned);
    }

    [Test]
    public void KillAll_KillsEveryEntityOfTheSide_NotTheOthers()
    {
        GameObject boardGo = new GameObject("Board");
        BoardLayout layout = ScriptableObject.CreateInstance<BoardLayout>();
        GameObject enemyA = new GameObject("Enemy A");
        GameObject enemyB = new GameObject("Enemy B");
        GameObject ally = new GameObject("Ally");
        try
        {
            Board board = boardGo.AddComponent<Board>();
            board.SetLayout(layout);
            _entityManager.board = board;
            // Adding Entity triggers Entity.Reset() (NREs without a full Init())
            TestHelpers.WithLoggingDisabled(() =>
            {
                enemyA.AddComponent<Entity>();
                enemyB.AddComponent<Entity>();
                ally.AddComponent<Entity>();
            });
            _entityManager.GetEntities(Entity.EntityType.Computer).AddRange(new[] { enemyA, enemyB });
            _entityManager.GetEntities(Entity.EntityType.Player).Add(ally);
            System.Collections.Generic.List<Entity> killed = new System.Collections.Generic.List<Entity>();
            _entityManager.OnEntityKilled.AddListener(killed.Add);

            // Destroy can't run outside the play mode: the entities stay in the scene, but not in the fight
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Enemy A: Destroy may not be called from edit mode"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Enemy B: Destroy may not be called from edit mode"));
            _entityManager.KillAll(Entity.EntityType.Computer);

            Assert.IsTrue(_entityManager.AreAllEntityDead(Entity.EntityType.Computer));
            CollectionAssert.AreEquivalent(new[] { enemyA.GetComponent<Entity>(), enemyB.GetComponent<Entity>() }, killed);
            CollectionAssert.AreEqual(new[] { ally }, _entityManager.GetEntities(Entity.EntityType.Player));
        }
        finally
        {
            Object.DestroyImmediate(enemyA);
            Object.DestroyImmediate(enemyB);
            Object.DestroyImmediate(ally);
            Object.DestroyImmediate(boardGo);
            Object.DestroyImmediate(layout);
        }
    }

    [Test]
    public void KillAll_NoEntityOfTheSide_DoesNothing()
    {
        Assert.DoesNotThrow(() => _entityManager.KillAll(Entity.EntityType.Computer));
        Assert.IsTrue(_entityManager.AreAllEntityDead(Entity.EntityType.Computer));
    }
}

}
