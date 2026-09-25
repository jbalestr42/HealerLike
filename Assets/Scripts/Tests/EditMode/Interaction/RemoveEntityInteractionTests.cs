using NUnit.Framework;
using UnityEngine;

namespace Interaction
{

public class RemoveEntityInteractionTests
{
    GameObject _entityGo;
    Entity _entity;
    GameObject _modelChild;
    GameObject _wall;

    [SetUp]
    public void SetUp()
    {
        _entityGo = new GameObject("Entity");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            _entity = _entityGo.AddComponent<Entity>();
        });

        // The collider hit by the mouse is usually deep in the model
        GameObject model = new GameObject("Model");
        model.transform.SetParent(_entityGo.transform);
        _modelChild = new GameObject("Mesh");
        _modelChild.transform.SetParent(model.transform);

        _wall = new GameObject("Wall");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_entityGo);
        Object.DestroyImmediate(_wall);
    }

    [Test]
    public void GetEntity_OnAChildOfTheEntity_ReturnsTheEntity()
    {
        Assert.AreSame(_entity, RemoveEntityInteraction.GetEntity(_modelChild));
    }

    [Test]
    public void GetEntity_OnTheEntityItself_ReturnsTheEntity()
    {
        Assert.AreSame(_entity, RemoveEntityInteraction.GetEntity(_entityGo));
    }

    [Test]
    public void GetEntity_OnSomethingElse_ReturnsNull()
    {
        Assert.IsNull(RemoveEntityInteraction.GetEntity(_wall));
    }

    [Test]
    public void IsValidTarget_OnAChildOfTheEntity_IsTrue()
    {
        Assert.IsTrue(new RemoveEntityInteraction().IsValidTarget(_modelChild));
    }

    [Test]
    public void IsValidTarget_OnSomethingElse_IsFalse()
    {
        Assert.IsFalse(new RemoveEntityInteraction().IsValidTarget(_wall));
    }

    [Test]
    public void GetLayerMask_OnlyHitsEntities()
    {
        Assert.AreEqual(1 << Layers.Entity, new RemoveEntityInteraction().GetLayerMask());
    }
}

}
