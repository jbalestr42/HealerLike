using System.Collections;
using NUnit.Framework;
using UI.Toolkit;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceActionsTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        StageInterfaceActions _actions;
        VisualElement _root;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _root = _panel.root;
            _root.style.width = 360f;
            _root.style.height = 400f;
            ToolkitTheme.Apply(_root, null);
            VisualElement party = new VisualElement { name = "party-list" };
            party.style.width = 280f;
            _root.Add(party);
            _root.Add(new Label { name = "detail-title" });
            _root.Add(new Label { name = "detail-description" });
            _view = new ToolkitGameView(_root);
            _view.OnInspect.AddListener(_view.ShowDetail);
            _actions = new StageInterfaceActions();
        }

        [TearDown]
        public void TearDown()
        {
            _actions.Dispose();
            _view.Release();
            _panel.Dispose();
        }

        [UnityTest]
        public IEnumerator InspectCard_DesktopInfoHidden_UsesRealFocusWithoutActivatingTheCard()
        {
            int activations = 0;
            int inspections = 0;
            ToolkitCardModel model = new ToolkitCardModel { key = "creature", title = "Focused creature",
                description = "Real card inspection", isEnabled = true, activate = _ => activations++ };
            _view.OnInspect.AddListener(_ => inspections++);
            _view.SetCards("party-list", new[] { model });
            yield return null;
            yield return null;
            Button card = _root.Q("party-list").Q<Button>("data-card");
            Button info = card.parent.Q<Button>("card-info");
            Assert.AreEqual(DisplayStyle.None, info.resolvedStyle.display);

            yield return _actions.InspectCard(card);

            Assert.AreSame(card, card.focusController.focusedElement);
            Assert.AreEqual(model.title, _root.Q<Label>("detail-title").text);
            Assert.AreEqual(model.description, _root.Q<Label>("detail-description").text);
            Assert.AreEqual(1, inspections);
            Assert.AreEqual(0, activations);

            yield return _actions.InspectCard(card);

            Assert.AreEqual(2, inspections, "A card that kept focus across resize can be inspected again.");
            Assert.AreEqual(0, activations);
        }
    }
}
