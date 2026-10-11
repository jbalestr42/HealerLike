using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The line growing from an entity to the unit its skill is about to be used on
public class SkillTargetLinesTests
{
    GameObject _go;
    GameObject _prefab;
    SkillTargetLines _lines;

    [SetUp]
    public void SetUp()
    {
        _prefab = new GameObject("SkillLine Prefab");
        LineRenderer prefabLine = _prefab.AddComponent<LineRenderer>();
        prefabLine.widthMultiplier = 0.07f;

        _go = new GameObject("Entity");
        _lines = _go.AddComponent<SkillTargetLines>();
        TestHelpers.SetPrivateField(_lines, "_linePrefab", prefabLine);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_prefab);
    }

    [Test]
    public void GetProgress_WhenTheWarningStarts_Zero()
    {
        Assert.AreEqual(0f, SkillTargetLines.GetProgress(1.5f, 1.5f), 0.0001f);
    }

    [Test]
    public void GetProgress_HalfwayThroughTheWarning_Half()
    {
        Assert.AreEqual(0.5f, SkillTargetLines.GetProgress(0.75f, 1.5f), 0.0001f);
    }

    [Test]
    public void GetProgress_Ready_One()
    {
        Assert.AreEqual(1f, SkillTargetLines.GetProgress(0f, 1.5f), 0.0001f);
    }

    [Test]
    public void GetProgress_BeforeTheWarning_StaysAtZero()
    {
        Assert.AreEqual(0f, SkillTargetLines.GetProgress(4f, 1.5f), 0.0001f);
    }

    [Test]
    public void SetLine_FullProgress_ReachesTheTarget()
    {
        Vector3 from = new Vector3(0f, 1f, 0f);
        Vector3 to = new Vector3(6f, 1f, 0f);

        _lines.SetLine(0, from, to, 1f, Color.green);

        LineRenderer line = _go.GetComponentInChildren<LineRenderer>();
        Assert.AreEqual(from, line.GetPosition(0));
        Assert.AreEqual(to, line.GetPosition(line.positionCount - 1));
    }

    [Test]
    public void SetLine_HalfProgress_StopsAtTheTopOfTheArc()
    {
        Vector3 from = new Vector3(0f, 1f, 0f);
        Vector3 to = new Vector3(6f, 1f, 0f);

        _lines.SetLine(0, from, to, 0.5f, Color.green);

        LineRenderer line = _go.GetComponentInChildren<LineRenderer>();
        Vector3 end = line.GetPosition(line.positionCount - 1);
        // Halfway along, raised by the height of the arc (6 units away, 0.35 per unit)
        Assert.AreEqual(3f, end.x, 0.0001f);
        Assert.AreEqual(1f + 6f * 0.35f, end.y, 0.0001f);
    }

    [Test]
    public void SetLine_TakesTheColor()
    {
        _lines.SetLine(0, Vector3.zero, Vector3.right, 1f, Color.red);

        LineRenderer line = _go.GetComponentInChildren<LineRenderer>();
        Assert.AreEqual(Color.red, line.startColor);
        Assert.AreEqual(Color.red, line.endColor);
    }

    [Test]
    public void HideFrom_HidesTheLinesFromTheIndexOn()
    {
        _lines.SetLine(0, Vector3.zero, Vector3.right, 1f, Color.red);
        _lines.SetLine(1, Vector3.zero, Vector3.forward, 1f, Color.red);

        _lines.HideFrom(1);

        Assert.AreEqual(1, _lines.visibleLineCount);
    }
}

}
