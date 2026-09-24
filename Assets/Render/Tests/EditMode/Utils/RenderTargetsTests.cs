using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render
{

public class RenderTargetsTests
{
    GameObject _unit;

    [SetUp]
    public void SetUp()
    {
        _unit = new GameObject("Unit");
        _unit.transform.position = new Vector3(1f, 0f, 2f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_unit);
    }

    static GameObject CreateChild(GameObject parent, Vector3 position)
    {
        GameObject child = new GameObject("Point");
        child.transform.SetParent(parent.transform, false);
        child.transform.position = position;
        return child;
    }

    [Test]
    public void Point_EntityTargetPoint_ReturnsIt()
    {
        GameObject point = CreateChild(_unit, new Vector3(1f, 3f, 2f));
        GameObject tagged = CreateChild(_unit, new Vector3(1f, 5f, 2f));
        tagged.AddComponent<SkillTargetPointTag>();
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _unit.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_targetPoint", point);

        Vector3 result = RenderTargets.Point(_unit);

        Assert.AreEqual(new Vector3(1f, 3f, 2f), result);
    }

    [Test]
    public void Point_OnlyTag_ReturnsTheTag()
    {
        GameObject tagged = CreateChild(_unit, new Vector3(1f, 5f, 2f));
        tagged.AddComponent<SkillTargetPointTag>();

        Vector3 result = RenderTargets.Point(_unit);

        Assert.AreEqual(new Vector3(1f, 5f, 2f), result);
    }

    [Test]
    public void Point_NoPoint_ReturnsTheUnit()
    {
        Vector3 result = RenderTargets.Point(_unit);

        Assert.AreEqual(new Vector3(1f, 0f, 2f), result);
    }

    [Test]
    public void Point_Null_ReturnsZero()
    {
        Vector3 result = RenderTargets.Point(null);

        Assert.AreEqual(Vector3.zero, result);
    }

    [Test]
    public void Anchor_NoPoint_ReturnsTheUnitTransform()
    {
        Transform result = RenderTargets.Anchor(_unit);

        Assert.AreSame(_unit.transform, result);
    }
}

}
