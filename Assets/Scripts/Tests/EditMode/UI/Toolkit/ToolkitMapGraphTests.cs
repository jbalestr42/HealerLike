using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
public class ToolkitMapGraphTests
{
    VisualElement _root;
    ToolkitMapGraph _graph;
    RunState _run;

    [SetUp]
    public void SetUp()
    {
        _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
        _run = ToolkitRunMapPresentationTests.CreateRun();
        _graph = new ToolkitMapGraph(_root.Q<ScrollView>("map-scroll"), delegate { });
    }

    [TearDown]
    public void TearDown() { _graph.Dispose(); }

    Button Node(MapNode node) { return _root.Q<Button>($"map-node-{node.floor}-{node.column}"); }

    [Test]
    public void Display_BindsUpstreamNodesAndAllConnections()
    {
        _graph.Display(_run, true);
        Assert.AreEqual(5, _root.Query<Button>(className: "map-node").ToList().Count);
        Assert.AreEqual(5, _root.Q<ToolkitMapConnections>("map-connections").edgeCount);
        Assert.AreSame(_run.map.startNodes[0], Node(_run.map.startNodes[0]).userData);
        Assert.IsTrue(Node(_run.map.startNodes[0]).enabledSelf);
        Assert.IsFalse(Node(_run.map.boss).enabledSelf);
    }

    [Test]
    public void Display_ProgressRestylesExistingButtonsWithoutRebuildingTopology()
    {
        _graph.Display(_run, true);
        MapNode first = _run.map.startNodes[0];
        Button button = Node(first);
        VisualElement paths = _root.Q("map-connections");
        _run.TravelTo(first);
        _graph.Display(_run, true);
        Assert.AreSame(button, Node(first));
        Assert.AreSame(paths, _root.Q("map-connections"));
        Assert.IsTrue(button.ClassListContains("is-current"));
        Assert.IsTrue(Node(_run.GetAvailableNodes()[0]).enabledSelf);
        Assert.IsFalse(Node(_run.map.startNodes[1]).enabledSelf);
        _run.TravelTo(_run.GetAvailableNodes()[0]);
        _graph.Display(_run, true);
        Assert.IsTrue(button.ClassListContains("is-visited"));
    }

    [Test]
    public void Display_InspectModeKeepsAvailabilityVisibleButCannotTravel()
    {
        _graph.Display(_run, false);
        Assert.IsTrue(Node(_run.map.startNodes[0]).ClassListContains("is-available"));
        foreach (Button button in _root.Query<Button>(className: "map-node").ToList())
            Assert.IsFalse(button.enabledSelf);
        Assert.IsNull(_run.currentNode);
    }

    [Test]
    public void Display_NewRunReplacesOldNodeReferences()
    {
        _graph.Display(_run, true);
        Button old = Node(_run.map.startNodes[0]);
        _run = ToolkitRunMapPresentationTests.CreateRun();
        _graph.Display(_run, true);
        Assert.IsNull(old.parent);
        Assert.AreNotSame(old, Node(_run.map.startNodes[0]));
        Assert.AreSame(_run.map.startNodes[0], Node(_run.map.startNodes[0]).userData);
    }

    [Test]
    public void Dispose_RemovesOwnedGraphAndDoesNotRepopulateOnLateRefresh()
    {
        _graph.Display(_run, true);
        _graph.Dispose();
        _graph.Display(_run, true);
        Assert.AreEqual(0, _root.Query<Button>(className: "map-node").ToList().Count);
        Assert.IsNull(_root.Q("map-connections"));
    }
}
}
