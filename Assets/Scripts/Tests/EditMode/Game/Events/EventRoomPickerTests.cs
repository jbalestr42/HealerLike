using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Events
{

public class EventRoomPickerTests
{
    class TestEventRoom : AEventRoom
    {
        public override void Play(IEventRoomHost host)
        {
            host.EndEvent();
        }
    }

    List<AEventRoom> _created = new List<AEventRoom>();

    [TearDown]
    public void TearDown()
    {
        foreach (AEventRoom eventRoom in _created)
        {
            Object.DestroyImmediate(eventRoom);
        }
        _created.Clear();
    }

    AEventRoom CreateEvent()
    {
        AEventRoom eventRoom = ScriptableObject.CreateInstance<TestEventRoom>();
        _created.Add(eventRoom);
        return eventRoom;
    }

    static EventRoomChance Chance(AEventRoom eventRoom, float weight)
    {
        return new EventRoomChance { eventRoom = eventRoom, weight = weight };
    }

    static Dictionary<AEventRoom, int> Draw(List<EventRoomChance> events, int count)
    {
        System.Random random = new System.Random(0);
        Dictionary<AEventRoom, int> draws = new Dictionary<AEventRoom, int>();
        for (int i = 0; i < count; i++)
        {
            AEventRoom picked = EventRoomPicker.Pick(events, random);
            draws.TryGetValue(picked, out int drawn);
            draws[picked] = drawn + 1;
        }
        return draws;
    }

    [Test]
    public void Pick_NoEvent_IsNull()
    {
        Assert.IsNull(EventRoomPicker.Pick(null, new System.Random(0)));
        Assert.IsNull(EventRoomPicker.Pick(new List<EventRoomChance>(), new System.Random(0)));
    }

    [Test]
    public void Pick_NoEventWithAWeight_IsNull()
    {
        List<EventRoomChance> events = new List<EventRoomChance> { Chance(CreateEvent(), 0f), Chance(null, 5f), null };

        Assert.IsNull(EventRoomPicker.Pick(events, new System.Random(0)));
    }

    [Test]
    public void Pick_SingleEvent_IsAlwaysIt()
    {
        AEventRoom only = CreateEvent();
        List<EventRoomChance> events = new List<EventRoomChance> { Chance(only, 0.3f) };

        Dictionary<AEventRoom, int> draws = Draw(events, 100);

        Assert.AreEqual(100, draws[only]);
    }

    [Test]
    public void Pick_NeverDrawsAnEventWithoutWeightOrAsset()
    {
        AEventRoom drawn = CreateEvent();
        AEventRoom never = CreateEvent();
        List<EventRoomChance> events = new List<EventRoomChance> { Chance(never, 0f), Chance(null, 2f), Chance(drawn, 1f) };

        Dictionary<AEventRoom, int> draws = Draw(events, 200);

        CollectionAssert.AreEquivalent(new[] { drawn }, draws.Keys);
    }

    [Test]
    public void Pick_EachEventInProportionToItsWeight()
    {
        // 1 out of 4 and 3 out of 4
        AEventRoom rare = CreateEvent();
        AEventRoom common = CreateEvent();
        List<EventRoomChance> events = new List<EventRoomChance> { Chance(rare, 1f), Chance(common, 3f) };

        Dictionary<AEventRoom, int> draws = Draw(events, 4000);

        Assert.AreEqual(0.25f, draws[rare] / 4000f, 0.03f);
        Assert.AreEqual(0.75f, draws[common] / 4000f, 0.03f);
    }

    [Test]
    public void Pick_SameSeed_SameEvents()
    {
        List<EventRoomChance> events = new List<EventRoomChance> { Chance(CreateEvent(), 1f), Chance(CreateEvent(), 1f), Chance(CreateEvent(), 2f) };
        System.Random first = new System.Random(42);
        System.Random second = new System.Random(42);

        List<AEventRoom> firstDraws = Enumerable.Range(0, 20).Select(_ => EventRoomPicker.Pick(events, first)).ToList();
        List<AEventRoom> secondDraws = Enumerable.Range(0, 20).Select(_ => EventRoomPicker.Pick(events, second)).ToList();

        CollectionAssert.AreEqual(firstDraws, secondDraws);
    }
}

}
