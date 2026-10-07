using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitMapNodeTests
    {
        VisualElement _root;
        ToolkitMapGraph _graph;
        RunState _run;
        readonly List<EntityData> _allies = new List<EntityData>();

        [SetUp]
        public void SetUp()
        {
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _run = ToolkitRunMapPresentationTests.CreateRun();
            _graph = new ToolkitMapGraph(_root.Q<ScrollView>("map-scroll"), delegate { });
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            foreach (EntityData ally in _allies)
            {
                Object.DestroyImmediate(ally);
            }

            _allies.Clear();
        }

        void Fall()
        {
            EntityData ally = ScriptableObject.CreateInstance<EntityData>();
            _allies.Add(ally);
            _run.AddDeadAlly(ally);
        }

        Label Subtitle(int floor, int column)
        {
            return _root.Q<Button>($"map-node-{floor}-{column}").Q<Label>("map-subtitle");
        }

        // Rest is floor 1, column 1 in the shared test run; the first combat is floor 0, column 0.
        Label RestSubtitle()
        {
            return Subtitle(1, 1);
        }

        [TestCase(MapNodeType.Rest, MapNodeState.Locked, 1, "1 FALLEN")]
        [TestCase(MapNodeType.Rest, MapNodeState.Available, 3, "3 FALLEN")]
        [TestCase(MapNodeType.Rest, MapNodeState.Locked, 0, "")]
        [TestCase(MapNodeType.Rest, MapNodeState.Visited, 2, "")]
        [TestCase(MapNodeType.Rest, MapNodeState.Current, 2, "")]
        [TestCase(MapNodeType.Combat, MapNodeState.Available, 2, "")]
        [TestCase(MapNodeType.Event, MapNodeState.Locked, 2, "")]
        [TestCase(MapNodeType.Boss, MapNodeState.Locked, 2, "")]
        public void SubtitleText_RoomStateAndFallenCount_ReturnsExpected(
            MapNodeType type,
            MapNodeState state,
            int fallen,
            string expected
        )
        {
            Assert.AreEqual(expected, ToolkitRunMapPresentation.SubtitleText(type, state, fallen));
        }

        [Test]
        public void Display_RestWithFallenAllies_ShowsTheCount()
        {
            Fall();
            Fall();

            _graph.Display(_run, true);

            Assert.AreEqual("2 FALLEN", RestSubtitle().text);
            Assert.IsFalse(RestSubtitle().ClassListContains("is-hidden"));
        }

        [Test]
        public void Display_RestWithoutFallenAllies_HidesTheSubtitle()
        {
            _graph.Display(_run, true);

            Assert.IsTrue(RestSubtitle().ClassListContains("is-hidden"));
            Assert.IsEmpty(RestSubtitle().text);
        }

        [Test]
        public void Display_CombatWithFallenAllies_HidesTheSubtitle()
        {
            Fall();

            _graph.Display(_run, true);

            Assert.IsTrue(Subtitle(0, 0).ClassListContains("is-hidden"));
        }

        [Test]
        public void Display_AllyFallsWithoutProgress_UpdatesTheSubtitle()
        {
            // A battle in the current room kills an ally; the map reopens on the same room.
            _run.TravelTo(_run.map.startNodes[0]);
            _graph.Display(_run, true);
            Fall();

            _graph.Display(_run, true);

            Assert.AreEqual("1 FALLEN", RestSubtitle().text);
        }

        [Test]
        public void Display_AllyResurrectedWithoutProgress_HidesTheSubtitle()
        {
            Fall();
            _graph.Display(_run, true);
            _run.RemoveDeadAlly(_allies[0]);

            _graph.Display(_run, true);

            Assert.IsTrue(RestSubtitle().ClassListContains("is-hidden"));
        }

        [Test]
        public void Display_RestAlreadyVisited_HidesTheSubtitle()
        {
            Fall();
            _run.TravelTo(_run.map.startNodes[0]);
            _run.TravelTo(_run.GetAvailableNodes()[0]);
            _run.TravelTo(_run.map.boss);

            _graph.Display(_run, true);

            Assert.IsTrue(RestSubtitle().ClassListContains("is-hidden"));
        }
    }
}
