using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitRoomPaletteTests
    {
        ToolkitTestPanel _panel;
        VisualElement _root;
        ToolkitMapGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _root.style.width = 1440f;
            _root.style.height = 900f;
            _panel.root.Add(_root);
            _root.Q("map-panel").RemoveFromClassList("is-hidden");
            _graph = new ToolkitMapGraph(_root.Q<ScrollView>("map-scroll"), null);
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            _panel.Dispose();
        }

        // One room of every type on the first floor, with the boss above them.
        static RunState CreateSixTypeRun()
        {
            MapNodeType[] types =
            {
                MapNodeType.Combat,
                MapNodeType.Elite,
                MapNodeType.Treasure,
                MapNodeType.Rest,
                MapNodeType.Event,
            };
            MapNode boss = new MapNode(1, 0, MapNodeType.Boss);
            List<MapNode> first = new List<MapNode>();
            for (int i = 0; i < types.Length; i++)
            {
                MapNode room = new MapNode(0, i, types[i]);
                room.Connect(boss);
                first.Add(room);
            }

            return new RunState(new RunMap(new List<List<MapNode>> { first }, boss, types.Length));
        }

        [UnityTest]
        public IEnumerator ResolveStyles_Forest_GivesEveryRoomTypeItsOwnGlyphColour()
        {
            yield return CheckPalette(false);
        }

        [UnityTest]
        public IEnumerator ResolveStyles_Moon_GivesEveryRoomTypeItsOwnGlyphColour()
        {
            yield return CheckPalette(true);
        }

        IEnumerator CheckPalette(bool moon)
        {
            ToolkitTheme.Apply(_root, moon ? Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme") : null);
            RunState run = CreateSixTypeRun();
            _graph.Display(run, true);
            yield return null;
            yield return null;
            Dictionary<Color, MapNodeType> seen = new Dictionary<Color, MapNodeType>();
            foreach (MapNode node in run.map.GetAllNodes())
            {
                Button button = _root.Q<Button>($"map-node-{node.floor}-{node.column}");
                Color colour = button.Q("map-glyph").resolvedStyle.color;
                Assert.IsFalse(
                    seen.TryGetValue(colour, out MapNodeType other),
                    $"{node.type} shares its glyph colour {colour} with {other}"
                );
                seen.Add(colour, node.type);
            }

            Assert.AreEqual(6, seen.Count);
        }

        [UnityTest]
        public IEnumerator ResolveStyles_RestSubtitle_StaysOutOfTheNodeFlow()
        {
            // The subtitle rule lives in the theme's sheets, so without a theme every label resolves in the flow
            ToolkitTheme.Apply(_root, null);
            RunState run = CreateSixTypeRun();
            EntityData ally = ScriptableObject.CreateInstance<EntityData>();
            run.AddDeadAlly(ally);
            _graph.Display(run, true);
            yield return null;
            yield return null;

            Label subtitle = _root.Q<Button>("map-node-0-3").Q<Label>("map-subtitle");

            Assert.AreEqual(Position.Absolute, subtitle.resolvedStyle.position);
            Assert.AreEqual("1 FALLEN", subtitle.text);
            Object.DestroyImmediate(ally);
        }
    }
}
