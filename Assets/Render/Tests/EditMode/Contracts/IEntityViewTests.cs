using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render
{

public class FakeEntityView : MonoBehaviour, IEntityView
{
    public Entity entity;
    public RenderManager manager;
    public int initCount;

    public void Init(Entity entity, RenderManager manager)
    {
        this.entity = entity;
        this.manager = manager;
        initCount++;
    }
}

public class IEntityViewTests
{
    GameObject _entityGo;
    GameObject _managerGo;
    Entity _entity;
    RenderManager _manager;

    static FakeEntityView CreateView(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<FakeEntityView>();
    }

    [SetUp]
    public void SetUp()
    {
        _entityGo = new GameObject("entity");
        TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());
        _managerGo = new GameObject("manager");
        _manager = _managerGo.AddComponent<RenderManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_entityGo);
        Object.DestroyImmediate(_managerGo);
    }

    [Test]
    public void Init_ModelWalk_ReachesEveryViewWithEntityAndManager()
    {
        FakeEntityView body = CreateView(_entityGo.transform, "body");
        FakeEntityView head = CreateView(body.transform, "head");

        foreach (IEntityView view in _entityGo.GetComponentsInChildren<IEntityView>())
        {
            view.Init(_entity, _manager);
        }

        Assert.AreSame(_entity, body.entity);
        Assert.AreSame(_manager, body.manager);
        Assert.AreSame(_entity, head.entity);
        Assert.AreSame(_manager, head.manager);
    }

    [Test]
    public void Init_ModelWalk_CallsEachViewOnce()
    {
        FakeEntityView view = CreateView(_entityGo.transform, "view");

        foreach (IEntityView entityView in _entityGo.GetComponentsInChildren<IEntityView>())
        {
            entityView.Init(_entity, _manager);
        }

        Assert.AreEqual(1, view.initCount);
    }
}
}
