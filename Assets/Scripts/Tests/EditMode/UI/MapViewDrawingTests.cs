using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// A new drawing reuses the rooms and lines of the previous one: creating a TextMeshPro button is slow
public class MapViewDrawingTests
{
    GameObject _go;
    MapView _view;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Map", typeof(RectTransform));
        TestHelpers.WithLoggingDisabled(() => _view = _go.AddComponent<MapView>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    static RunState CreateRun(int floorCount, int seed)
    {
        return new RunState(MapGenerator.GenerateLayout(floorCount, 5, 4, new System.Random(seed)));
    }

    static int RoomCount(RunState run)
    {
        return run.map.GetAllNodes().Count();
    }

    int CountComponents<T>(bool includeHidden) where T : Component
    {
        return _go.GetComponentsInChildren<T>(includeHidden).Length;
    }

    [Test]
    public void Display_OneButtonPerRoom()
    {
        RunState run = CreateRun(6, 0);

        _view.Display(run, true);

        Assert.AreEqual(RoomCount(run), _view.roomButtons.Count);
        Assert.AreEqual(RoomCount(run), CountComponents<Button>(false));
    }

    [Test]
    public void Display_Again_ReusesTheSameRoomsAndLines()
    {
        RunState run = CreateRun(6, 0);
        _view.Display(run, true);
        List<Button> firstRooms = _view.roomButtons.ToList();
        int lineCount = CountComponents<Image>(true);

        _view.Display(run, true);

        CollectionAssert.AreEqual(firstRooms, _view.roomButtons);
        Assert.AreEqual(lineCount, CountComponents<Image>(true));
    }

    [Test]
    public void Display_SmallerMap_HidesTheRoomsLeftOver()
    {
        RunState big = CreateRun(8, 0);
        RunState small = CreateRun(3, 1);
        _view.Display(big, true);
        int created = CountComponents<Button>(true);

        _view.Display(small, true);

        Assert.AreEqual(RoomCount(small), _view.roomButtons.Count);
        Assert.AreEqual(RoomCount(small), CountComponents<Button>(false));
        // Kept for a later bigger map
        Assert.AreEqual(created, CountComponents<Button>(true));
    }

    [Test]
    public void ReusedRoom_ClickSelectsItsNewRoomOnce()
    {
        RunState first = CreateRun(6, 0);
        RunState second = CreateRun(6, 1);
        _view.Display(first, true);
        _view.Display(second, true);
        List<MapNode> selected = new List<MapNode>();
        _view.OnNodeSelected.AddListener(selected.Add);

        // The first room of the first floor is available
        Button startRoom = _view.roomButtons.First(button => button.interactable);
        startRoom.onClick.Invoke();

        Assert.AreEqual(1, selected.Count);
        CollectionAssert.Contains(second.map.startNodes, selected[0]);
    }

    [Test]
    public void ReusedRoom_OutlineOnlyWhenItsStyleHasOne()
    {
        // Selectable: the start rooms get an outline
        RunState run = CreateRun(6, 0);
        _view.Display(run, true);
        // Then every room of the map is locked or visited: the outline of the reused ones must go
        run.TravelTo(run.map.startNodes[0]);
        run.TravelTo(run.map.startNodes[0].next[0]);

        _view.Display(run, true);

        foreach (Button room in _view.roomButtons)
        {
            Outline outline = room.GetComponent<Outline>();
            bool shown = outline != null && outline.enabled;
            MapNodeState state = run.GetNodeState(run.map.GetAllNodes().First(node => room.name == $"Room {node}"));
            Assert.AreEqual(MapView.GetNodeStyle(state, Color.white, true).hasOutline, shown, room.name);
        }
    }
}

}
