using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class CreatureLooksTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    GameObject CreateView(string name)
    {
        GameObject view = new GameObject(name);
        _objects.Add(view);
        return view;
    }

    CreatureLooks CreateLooks()
    {
        CreatureLooks looks = CreateTracked<CreatureLooks>();
        looks.ally = CreateView("Ally");
        looks.enemy = CreateView("Enemy");
        return looks;
    }

    [Test]
    public void GetView_MappedEntity_ReturnsMappedView()
    {
        CreatureLooks looks = CreateLooks();
        EntityData data = CreateTracked<EntityData>();
        GameObject view = CreateView("Mapped");
        looks.entities[data] = view;

        GameObject result = looks.GetView(data, Entity.EntityType.Computer);

        Assert.AreSame(view, result);
    }

    [Test]
    public void GetView_UnmappedPlayerEntity_ReturnsAlly()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(CreateTracked<EntityData>(), Entity.EntityType.Player);

        Assert.AreSame(looks.ally, result);
    }

    [Test]
    public void GetView_UnmappedComputerEntity_ReturnsEnemy()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(CreateTracked<EntityData>(), Entity.EntityType.Computer);

        Assert.AreSame(looks.enemy, result);
    }

    [Test]
    public void GetView_NullData_ReturnsSideDefault()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(null, Entity.EntityType.Player);

        Assert.AreSame(looks.ally, result);
    }
}

}
