using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class RenderManagerTests
{
    GameObject _managerGo;
    GameObject _gameGo;
    RenderManager _manager;

    [SetUp]
    public void SetUp()
    {
        _managerGo = new GameObject("RenderManager");
        _manager = _managerGo.AddComponent<RenderManager>();
        _gameGo = new GameObject("Game");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_managerGo);
        Object.DestroyImmediate(_gameGo);
    }

    [Test]
    public void Init_NullEntityManager_StaysDetached()
    {
        TestHelpers.WithLoggingDisabled(() => _manager.Init(null, null));

        Assert.IsNull(_manager.entityManager);
        Assert.IsNull(_manager.player);
    }

    [Test]
    public void Init_EntityManager_KeepsEntityManagerAndPlayer()
    {
        EntityManager entityManager = _gameGo.AddComponent<EntityManager>();
        PlayerBehaviour player = _gameGo.AddComponent<PlayerBehaviour>();

        _manager.Init(entityManager, player);

        Assert.AreSame(entityManager, _manager.entityManager);
        Assert.AreSame(player, _manager.player);
    }

    [Test]
    public void Init_SameEntityManagerTwice_KeepsFirstPlayer()
    {
        EntityManager entityManager = _gameGo.AddComponent<EntityManager>();
        PlayerBehaviour player = _gameGo.AddComponent<PlayerBehaviour>();
        _manager.Init(entityManager, player);

        _manager.Init(entityManager, null);

        Assert.AreSame(player, _manager.player);
    }
}
}
