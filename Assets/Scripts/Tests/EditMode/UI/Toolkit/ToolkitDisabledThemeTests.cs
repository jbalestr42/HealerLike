using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitDisabledThemeTests
    {
        ToolkitTestPanel _panel;
        VisualElement _root;
        ToolkitGameView _view;
        ToolkitMapGraph _graph;
        RunState _run;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _root.style.width = 1440f;
            _root.style.height = 900f;
            _panel.root.Add(_root);
            _view = new ToolkitGameView(_root);
            _view.SetCards("party-list", new[]
            {
                new ToolkitCardModel { title = "Unavailable creature", isEnabled = false },
            });
            _root.Q("map-panel").RemoveFromClassList("is-hidden");
            _run = ToolkitRunMapPresentationTests.CreateRun();
            _graph = new ToolkitMapGraph(_root.Q<ScrollView>("map-scroll"), null);
            _graph.Display(_run, true);
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            _view.Release();
            _panel.Dispose();
        }

        [UnityTest]
        public IEnumerator ResolveStyles_ForestDisabledControls_PreserveAuthoredPaletteAndMapStates()
        {
            yield return CheckTheme(false);
        }

        [UnityTest]
        public IEnumerator ResolveStyles_MoonDisabledControls_PreserveAuthoredPaletteAndMapStates()
        {
            yield return CheckTheme(true);
        }

        IEnumerator CheckTheme(bool moon)
        {
            ToolkitTheme.Apply(_root, moon ? Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme") : null);
            yield return null;
            yield return null;
            Button locked = _root.Q<Button>("map-node-2-1");
            Assert.IsFalse(locked.enabledInHierarchy);
            AssertColour(locked, moon ? Rgb(23, 27, 37) : Rgb(23, 37, 37));
            Assert.AreEqual(1f, locked.resolvedStyle.opacity);
            Button card = _root.Q("party-list").Q<Button>("data-card");
            Assert.IsFalse(card.enabledInHierarchy);
            Color cardColour = moon ? Rgb(29, 33, 45) : Rgb(29, 45, 45);
            AssertColour(card, cardColour);
            Assert.AreEqual(0.42f, card.resolvedStyle.opacity, 0.001f);

            Button first = _root.Q<Button>("map-node-0-0");
            Assert.IsTrue(first.enabledInHierarchy);
            AssertColour(first, moon ? Rgb(45, 51, 68) : Rgb(45, 68, 49));
            _run.TravelTo(_run.map.startNodes[0]);
            _graph.Display(_run, true);
            yield return null;
            yield return null;
            Assert.IsFalse(first.enabledInHierarchy);
            AssertColour(first, moon ? Rgb(35, 42, 61) : Rgb(61, 53, 35));
            _run.TravelTo(_run.GetAvailableNodes()[0]);
            _graph.Display(_run, true);
            yield return null;
            yield return null;
            AssertColour(first, moon ? Rgb(26, 29, 37) : Rgb(32, 37, 32));

            // Modal blocking disables ancestors, so descendants must retain the same palette.
            _root.SetEnabled(false);
            yield return null;
            yield return null;
            AssertColour(_root.Q<Button>("detail-equip-button"), cardColour);
            AssertColour(_root.Q<Button>("pause-button"), Color.clear);
            AssertColour(_root.Q<Button>("wave-button"), moon ? Rgb(153, 218, 210) : Rgb(189, 218, 156));
            AssertColour(card, cardColour);
            VisualElement input = _root.Q<DropdownField>("detail-targeting")
                .Q(className: "unity-base-popup-field__input");
            AssertColour(input, cardColour);
            Color ink = moon ? Rgb(164, 184, 235) : Rgb(229, 235, 228);
            Color border = moon ? Rgb(62, 69, 88) : Rgb(65, 88, 81);
            foreach (VisualElement control in new[] { card, _root.Q("detail-equip-button"), input })
            {
                Assert.AreEqual(ink, control.resolvedStyle.color, control.name + " disabled text");
                Assert.AreEqual(border, control.resolvedStyle.borderTopColor, control.name + " disabled border");
            }
        }

        static Color Rgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 255);
        }

        static void AssertColour(VisualElement button, Color expected)
        {
            Color actual = button.resolvedStyle.backgroundColor;
            string diagnostic = button.name + " background: " + actual;
            Assert.AreEqual(expected.r, actual.r, 0.001f, diagnostic);
            Assert.AreEqual(expected.g, actual.g, 0.001f, diagnostic);
            Assert.AreEqual(expected.b, actual.b, 0.001f, diagnostic);
            Assert.AreEqual(expected.a, actual.a, 0.001f, diagnostic);
            Assert.AreEqual(default(Background), button.resolvedStyle.backgroundImage, diagnostic);
        }
    }
}
