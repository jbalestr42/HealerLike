using System.Collections.Generic;
using NUnit.Framework;

namespace UI.Toolkit
{
    public class ToolkitEncounterBarPhaseTests
    {
        static RunState CreateRunInRoom(MapNodeType type)
        {
            MapNode room = new MapNode(0, 0, type);
            MapNode boss = new MapNode(1, 0, MapNodeType.Boss);
            room.Connect(boss);
            RunState run = new RunState(
                new RunMap(new List<List<MapNode>> { new List<MapNode> { room } }, boss, 1)
            );
            run.TravelTo(room);
            return run;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PhaseText_EventView_DoesNotReadAsBattle(bool isPreparing)
        {
            string text = ToolkitEncounterBar.PhaseText(ViewType.Event, false, isPreparing, false);

            Assert.AreNotEqual("IN BATTLE", text);
            Assert.AreNotEqual("PREPARATION", text);
        }

        [Test]
        public void PhaseText_EventView_NamesNeitherEventNorRest()
        {
            // The same view hosts the event rooms and the rest room, so the text cannot claim either one.
            string text = ToolkitEncounterBar.PhaseText(ViewType.Event, false, false, false);

            StringAssert.DoesNotContain("EVENT", text);
            StringAssert.DoesNotContain("REST", text);
        }

        [Test]
        public void PhaseText_MapView_StaysExpeditionMap()
        {
            Assert.AreEqual("EXPEDITION MAP", ToolkitEncounterBar.PhaseText(ViewType.Map, false, true, false));
        }

        [Test]
        public void PhaseText_UpgradeView_StaysChooseAReward()
        {
            Assert.AreEqual("CHOOSE A REWARD", ToolkitEncounterBar.PhaseText(ViewType.Upgrade, false, false, false));
        }

        [Test]
        public void PhaseText_GameViewPreparing_ReadsPreparation()
        {
            Assert.AreEqual("PREPARATION", ToolkitEncounterBar.PhaseText(ViewType.Game, false, true, false));
        }

        [Test]
        public void PhaseText_GameViewNotPreparing_ReadsInBattle()
        {
            Assert.AreEqual("IN BATTLE", ToolkitEncounterBar.PhaseText(ViewType.Game, false, false, false));
        }

        [Test]
        public void PhaseText_StartOnBoss_ReadsSummitReached()
        {
            Assert.AreEqual("SUMMIT REACHED", ToolkitEncounterBar.PhaseText(ViewType.Game, true, true, true));
        }

        [Test]
        public void PhaseText_StartOffBoss_ReadsReady()
        {
            Assert.AreEqual("READY", ToolkitEncounterBar.PhaseText(ViewType.Game, true, true, false));
        }

        [TestCase(MapNodeType.Combat, "Room 1 · Combat")]
        [TestCase(MapNodeType.Elite, "Room 1 · Elite")]
        [TestCase(MapNodeType.Treasure, "Room 1 · Treasure")]
        [TestCase(MapNodeType.Rest, "Room 1 · Rest")]
        [TestCase(MapNodeType.Event, "Room 1 · Event")]
        [TestCase(MapNodeType.Boss, "Room 1 · Boss")]
        public void RoomText_InRoom_NamesTheRoomType(MapNodeType type, string expected)
        {
            Assert.AreEqual(expected, ToolkitEncounterBar.RoomText(CreateRunInRoom(type)));
        }

        [Test]
        public void RoomText_BeforeFirstRoom_AsksToChooseARoute()
        {
            RunState run = ToolkitRunMapPresentationTests.CreateRun();

            Assert.AreEqual("Choose your route", ToolkitEncounterBar.RoomText(run));
        }

        [Test]
        public void RoomText_NoRun_AsksToChooseARoute()
        {
            Assert.AreEqual("Choose your route", ToolkitEncounterBar.RoomText(null));
        }
    }
}
