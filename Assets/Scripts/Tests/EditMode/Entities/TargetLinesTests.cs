using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The lines from a selected entity to its targets
public class TargetLinesTests
{
    GameObject _go;
    GameObject _prefab;
    TargetLines _lines;

    [SetUp]
    public void SetUp()
    {
        _prefab = new GameObject("TargetLine Prefab");
        LineRenderer prefabLine = _prefab.AddComponent<LineRenderer>();
        prefabLine.widthMultiplier = 0.2f;

        _go = new GameObject("Entity");
        _lines = _go.AddComponent<TargetLines>();
        TestHelpers.SetPrivateField(_lines, "_linePrefab", prefabLine);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_prefab);
    }

    List<LineRenderer> GetVisibleLines()
    {
        return new List<LineRenderer>(_go.GetComponentsInChildren<LineRenderer>(false));
    }

    [Test]
    public void SetLines_DrawsOneLineFromTheEntityToEachTarget()
    {
        Vector3 from = new Vector3(0f, 1f, 0f);
        List<Vector3> targets = new List<Vector3> { new Vector3(5f, 1f, 0f), new Vector3(0f, 1f, 5f) };

        _lines.SetLines(from, targets);

        List<LineRenderer> lines = GetVisibleLines();
        Assert.AreEqual(2, lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            Assert.AreEqual(from, lines[i].GetPosition(0));
            Assert.AreEqual(targets[i], lines[i].GetPosition(lines[i].positionCount - 1));
        }
    }

    [Test]
    public void SetLines_LinesComeFromThePrefab()
    {
        _lines.SetLines(Vector3.zero, new List<Vector3> { Vector3.one });

        Assert.AreEqual(0.2f, GetVisibleLines()[0].widthMultiplier, 0.0001f);
    }

    [Test]
    public void SetLines_WithFewerTargets_HidesTheLinesLeft()
    {
        _lines.SetLines(Vector3.zero, new List<Vector3> { Vector3.one, Vector3.right });

        _lines.SetLines(Vector3.zero, new List<Vector3> { Vector3.forward });

        Assert.AreEqual(1, _lines.visibleLineCount);
        LineRenderer line = GetVisibleLines()[0];
        Assert.AreEqual(Vector3.forward, line.GetPosition(line.positionCount - 1));
    }

    [Test]
    public void SetLines_CurvesUpBetweenTheEntityAndItsTarget()
    {
        _lines.SetLines(Vector3.zero, new List<Vector3> { new Vector3(10f, 0f, 0f) });

        LineRenderer line = GetVisibleLines()[0];
        Assert.Greater(line.positionCount, 2);
        Vector3 middle = line.GetPosition(line.positionCount / 2);
        Assert.Greater(middle.y, 0f);
    }

    [Test]
    public void GetArcPoint_StartsAndEndsOnTheEntities_AndPeaksAtTheMiddle()
    {
        Vector3 from = new Vector3(0f, 1f, 0f);
        Vector3 to = new Vector3(10f, 1f, 0f);

        Assert.AreEqual(from, TargetLines.GetArcPoint(from, to, 0f, 2f));
        Assert.AreEqual(to, TargetLines.GetArcPoint(from, to, 1f, 2f));
        Assert.AreEqual(new Vector3(5f, 3f, 0f), TargetLines.GetArcPoint(from, to, 0.5f, 2f));
        // A parabola: a quarter of the way it's at 3/4 of the height
        Assert.AreEqual(1f + 1.5f, TargetLines.GetArcPoint(from, to, 0.25f, 2f).y, 0.0001f);
    }

    [Test]
    public void GetScrollOffset_Decreases_SoTheDashesMoveTowardTheTarget()
    {
        float before = TargetLines.GetScrollOffset(1f, 0.6f, 0.3f);
        float after = TargetLines.GetScrollOffset(1.1f, 0.6f, 0.3f);

        // 0.6 units/s on dashes of 0.3: 0.2 of a dash in 0.1s, modulo one dash
        Assert.AreEqual(Mathf.Repeat(before - 0.2f, 1f), after, 0.0001f);
    }

    [Test]
    public void Show_False_HidesEveryLine()
    {
        _lines.SetLines(Vector3.zero, new List<Vector3> { Vector3.one, Vector3.right });

        _lines.Show(false);

        Assert.AreEqual(0, _lines.visibleLineCount);
    }
}

}
