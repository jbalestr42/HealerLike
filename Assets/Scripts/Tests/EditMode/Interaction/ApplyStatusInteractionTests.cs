using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Interaction
{

// The sandbox puts the clicked units in a status, one more stack on each click
public class ApplyStatusInteractionTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    StatusData _status;
    BuffHandlerFactory _handler;
    Entity _unit;
    GameObject _modelChild;
    GameObject _wall;

    [SetUp]
    public void SetUp()
    {
        GameplayTag tag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        tag.name = "Vulnerable";
        AddTagBuffFactory addTag = Track(ScriptableObject.CreateInstance<AddTagBuffFactory>());
        addTag.data = new AddTagBuffData { tag = tag };
        _handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            maxStacks = 5,
            stackAcrossSources = true,
            buffFactoryList = new List<ABuffFactory> { addTag },
            tags = new List<GameplayTag> { tag },
        };
        _status = Track(ScriptableObject.CreateInstance<StatusData>());
        _status.title = "Vulnerable";
        _status.tag = tag;
        _status.buffHandlerFactory = _handler;

        _unit = _units.Create(100f, 100f, "Unit");
        _unit.GetComponent<BuffManager>().isEnabled = true;
        // The collider hit by the mouse is usually deep in the model
        GameObject model = new GameObject("Model");
        model.transform.SetParent(_unit.transform);
        _modelChild = new GameObject("Mesh");
        _modelChild.transform.SetParent(model.transform);
        _wall = Track(new GameObject("Wall"));
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    int Stacks()
    {
        BuffManager buffManager = _unit.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        BuffManager.BuffHandlerData given = buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _handler);
        return given != null ? given.currentStacks : 0;
    }

    [Test]
    public void Apply_OnAChildOfTheUnit_PutsTheUnitInTheStatus()
    {
        new ApplyStatusInteraction(_status).Apply(_modelChild);

        Assert.AreEqual(1, Stacks());
        Assert.IsTrue(_unit.HasStatus(_status));
    }

    [Test]
    public void Apply_EachClick_OneMoreStack()
    {
        ApplyStatusInteraction interaction = new ApplyStatusInteraction(_status);

        interaction.Apply(_modelChild);
        interaction.Apply(_modelChild);
        interaction.Apply(_unit.gameObject);

        Assert.AreEqual(3, Stacks());
    }

    [Test]
    public void Apply_OnSomethingElse_GivesNothing()
    {
        Assert.DoesNotThrow(() => new ApplyStatusInteraction(_status).Apply(_wall));
        Assert.AreEqual(0, Stacks());
        Assert.IsFalse(_unit.HasStatus(_status));
    }

    [Test]
    public void IsValidTarget_OnlyAUnit()
    {
        ApplyStatusInteraction interaction = new ApplyStatusInteraction(_status);

        Assert.IsTrue(interaction.IsValidTarget(_modelChild));
        Assert.IsFalse(interaction.IsValidTarget(_wall));
    }
}

}
