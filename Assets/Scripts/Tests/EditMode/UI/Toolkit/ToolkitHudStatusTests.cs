using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The Devotion and the ascension level in the header, the mana, Devotion and ascension explained in the popover
    public class ToolkitHudStatusTests
    {
        ToolkitTestRun _run;
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        ToolkitHudStatus _status;
        GameObject _characterObject;
        GameObject _devotionObject;
        Character _character;
        GameData _data;
        AscensionSettings _ascension;
        readonly List<ToolkitCardModel> _requested = new List<ToolkitCardModel>();

        [SetUp]
        public void SetUp()
        {
            _run = new ToolkitTestRun();
            _characterObject = new GameObject("Cleric");
            ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_characterObject, ResourceType.Mana, AttributeType.ManaMax, 30f);
            mana.SetValue(12f);
            TestHelpers.WithLoggingDisabled(() => _character = _characterObject.AddComponent<Character>());
            TestHelpers.SetPrivateField(_character, "_mana", mana);
            _run.player.character = _character;

            _data = ScriptableObject.CreateInstance<GameData>();
            _ascension = ScriptableObject.CreateInstance<AscensionSettings>();
            _ascension.levels.Add(new AscensionLevel { title = "Elites", description = "More elites." });
            _ascension.levels.Add(new AscensionLevel { title = "Cursed", description = "A cursed item." });
            _data.ascension = _ascension;
            _run.context.data = _data;

            _panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(root);
            _view = new ToolkitGameView(root);
            _requested.Clear();
            _view.OnInspectRequested.AddListener(_requested.Add);
            _status = new ToolkitHudStatus();
            _status.Init(_run.context, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _status.Dispose();
            _view.Release();
            _panel.Dispose();
            _run.Dispose();
            Object.DestroyImmediate(_characterObject);
            if (_devotionObject != null)
            {
                Object.DestroyImmediate(_devotionObject);
            }
            Object.DestroyImmediate(_ascension);
            Object.DestroyImmediate(_data);
        }

        VisualElement Element(string name)
        {
            return _view.root.Q(name);
        }

        static bool IsHidden(VisualElement element)
        {
            return element.ClassListContains("is-hidden");
        }

        static void Hover(VisualElement element)
        {
            using (PointerEnterEvent evt = PointerEnterEvent.GetPooled())
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }

        ResourceAttribute AddDevotion(float value, float max)
        {
            // Apart: the AttributeManager of the character holds the max mana
            _devotionObject = new GameObject("Devotion");
            ResourceAttribute devotion = TestHelpers.CreateResourceAttribute(_devotionObject, ResourceType.Devotion, AttributeType.DevotionMax, max);
            devotion.SetValue(value);
            TestHelpers.SetPrivateField(_character, "_devotion", devotion);
            return devotion;
        }

        void StartRun(int ascensionLevel)
        {
            TestHelpers.SetPrivateField(_run.ascension, "_run", new RunState(new RunMap(new List<List<MapNode>>(), new MapNode(0, 0, MapNodeType.Boss), 1), ascensionLevel));
        }

        [Test]
        public void Refresh_CharacterWithoutDevotion_HidesIt()
        {
            _status.Refresh();

            Assert.IsTrue(IsHidden(Element(ToolkitHudStatus.DevotionName)));
        }

        [Test]
        public void Refresh_Devotion_ShowsItsCountLit()
        {
            AddDevotion(2f, 5f);

            _status.Refresh();

            VisualElement devotion = Element(ToolkitHudStatus.DevotionName);
            Assert.IsFalse(IsHidden(devotion));
            Assert.AreEqual(ToolkitHudStatus.GetDevotionText(2, 5), ((Label)devotion).text);
            Assert.IsTrue(devotion.ClassListContains(ToolkitHudStatus.LitClass));
        }

        [Test]
        public void Refresh_EmptyDevotion_ShowsItUnlit()
        {
            AddDevotion(0f, 5f);

            _status.Refresh();

            Assert.IsFalse(Element(ToolkitHudStatus.DevotionName).ClassListContains(ToolkitHudStatus.LitClass));
        }

        [Test]
        public void Refresh_NoRun_HidesTheAscension()
        {
            _status.Refresh();

            Assert.IsTrue(IsHidden(Element(ToolkitHudStatus.AscensionName)));
        }

        [Test]
        public void Refresh_RunAtLevel0_HidesTheAscension()
        {
            StartRun(0);

            _status.Refresh();

            Assert.IsTrue(IsHidden(Element(ToolkitHudStatus.AscensionName)));
        }

        [Test]
        public void Refresh_AscendedRun_ShowsItsLevel()
        {
            StartRun(2);

            _status.Refresh();

            Assert.IsFalse(IsHidden(Element(ToolkitHudStatus.AscensionName)));
            Assert.AreEqual("Ascension 2", ((Label)Element(ToolkitHudStatus.AscensionName)).text);
        }

        [Test]
        public void Refresh_RunAboveTheMaxLevel_ShowsTheMaxLevel()
        {
            StartRun(9);

            _status.Refresh();

            Assert.AreEqual("Ascension 2", ((Label)Element(ToolkitHudStatus.AscensionName)).text);
        }

        [Test]
        public void GetAscensionText_OnAPhone_IsShort()
        {
            Assert.AreEqual("A3", ToolkitHudStatus.GetAscensionText(3, true));
            Assert.AreEqual("Ascension 3", ToolkitHudStatus.GetAscensionText(3, false));
        }

        [Test]
        public void HoverAscension_OpensItsMaluses()
        {
            StartRun(2);

            Hover(Element(ToolkitHudStatus.AscensionName));

            Assert.AreEqual(1, _requested.Count);
            Assert.AreEqual("Ascension 2", _requested[0].title);
            StringAssert.Contains("Elites", _requested[0].description);
            StringAssert.Contains("A cursed item.", _requested[0].description);
        }

        [Test]
        public void HoverAscension_NoRun_OpensNothing()
        {
            Hover(Element(ToolkitHudStatus.AscensionName));

            Assert.IsEmpty(_requested);
        }

        [Test]
        public void HoverMana_OpensItsValueAndWhatComesBack()
        {
            Hover(Element(ToolkitHudStatus.ManaName));

            Assert.AreEqual(1, _requested.Count);
            Assert.AreEqual("Mana 12 / 30", _requested[0].title);
            StringAssert.Contains("After each fight", _requested[0].description);
            StringAssert.DoesNotStartWith("<b>Mana</b>", _requested[0].description, "The title already names it");
        }

        [Test]
        public void HoverDevotion_OpensWhatItDoes()
        {
            AddDevotion(2f, 5f);

            Hover(Element(ToolkitHudStatus.DevotionName));

            Assert.AreEqual("Devotion 2 / 5", _requested[0].title);
            StringAssert.Contains("Back to 0 at the start of each fight.", _requested[0].description);
        }

        [Test]
        public void WithoutHeading_RemovesTheBoldNameLine()
        {
            Assert.AreEqual("Body", ToolkitHudStatus.WithoutHeading("<b>Mana</b>\nBody", "Mana"));
            Assert.AreEqual("Body", ToolkitHudStatus.WithoutHeading("Body", "Mana"));
        }
    }
}
