using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{

public class ToolkitRunMapPresentationTests
{
    internal static RunState CreateRun()
    {
        MapNode left = new MapNode(0, 0, MapNodeType.Combat);
        MapNode right = new MapNode(0, 2, MapNodeType.Combat);
        MapNode rest = new MapNode(1, 1, MapNodeType.Rest);
        MapNode treasure = new MapNode(1, 2, MapNodeType.Treasure);
        MapNode boss = new MapNode(2, 1, MapNodeType.Boss);
        left.Connect(rest);
        right.Connect(rest);
        right.Connect(treasure);
        rest.Connect(boss);
        treasure.Connect(boss);
        return new RunState(
            new RunMap(
                new List<List<MapNode>>
                {
                    new List<MapNode> { left, right },
                    new List<MapNode> { rest, treasure },
                },
                boss,
                3
            )
        );
    }

    [TestCase(288f)]
    [TestCase(390f)]
    [TestCase(844f)]
    public void Layout_PreservesColumnOrderAndTouchTargets(float viewportWidth)
    {
        RunMap map = CreateRun().map;
        Vector2 size = ToolkitRunMapPresentation.CanvasSize(map, viewportWidth);
        float width = ToolkitRunMapPresentation.NodeWidth(map, size.x);
        Assert.GreaterOrEqual(width, 45f);
        Assert.GreaterOrEqual(ToolkitRunMapPresentation.NodeHeight, 45f);
        Assert.Greater(
            ToolkitRunMapPresentation.NodeCenter(map.startNodes[1], map, size.x).x,
            ToolkitRunMapPresentation.NodeCenter(map.startNodes[0], map, size.x).x + width
        );
        Assert.Less(
            ToolkitRunMapPresentation.NodeCenter(map.boss, map, size.x).y,
            ToolkitRunMapPresentation.NodeCenter(map.startNodes[0], map, size.x).y
        );
        Assert.AreEqual(size.x * 0.5f, ToolkitRunMapPresentation.NodeCenter(map.boss, map, size.x).x);
    }

    [Test]
    public void Canvas_WideMapScrollsRatherThanShrinkingTouchTargets()
    {
        RunMap source = CreateRun().map;
        RunMap map = new RunMap(new List<List<MapNode>> { new List<MapNode>(source.startNodes) }, source.boss, 9);
        Vector2 size = ToolkitRunMapPresentation.CanvasSize(map, 320f);
        Assert.Greater(size.x, 320f);
        Assert.GreaterOrEqual(ToolkitRunMapPresentation.NodeWidth(map, size.x), 45f);
    }

    [Test]
    public void Focus_EntryIncludesAvailableFloorAndProgressMovesTowardSummit()
    {
        RunState run = CreateRun();
        Vector2 size = ToolkitRunMapPresentation.CanvasSize(run.map, 320f);
        Vector2 viewport = new Vector2(320f, 224f);
        Vector2 first = ToolkitRunMapPresentation.FocusOffset(run, size, viewport);
        float firstY = ToolkitRunMapPresentation.NodeCenter(run.map.startNodes[0], run.map, size.x).y;
        Assert.That(firstY - first.y, Is.InRange(34f, viewport.y - 34f));
        run.TravelTo(run.map.startNodes[0]);
        run.TravelTo(run.GetAvailableNodes()[0]);
        Vector2 next = ToolkitRunMapPresentation.FocusOffset(run, size, viewport);
        Assert.Less(next.y, first.y);
        Assert.GreaterOrEqual(next.y, 0f);
    }

    [TestCase(MapNodeType.Combat)]
    [TestCase(MapNodeType.Elite)]
    [TestCase(MapNodeType.Treasure)]
    [TestCase(MapNodeType.Rest)]
    [TestCase(MapNodeType.Boss)]
    public void EveryRoomType_HasAnExplanation(MapNodeType type)
    {
        Assert.IsNotEmpty(ToolkitRunMapPresentation.Description(type));
    }
}
}
