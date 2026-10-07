using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitCardGestureTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        Button _button;
        GameObject _owner;
        EntityData _data;
        int _casts, _inspects;
        Drag _drag;
        [SetUp]
        public void Setup()
        {
            _casts = _inspects = 0; _drag = new Drag();
            _panel = new ToolkitTestPanel();
            var root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            root.style.width = 390; root.style.height = 844; root.style.flexShrink = 0;
            _panel.root.Add(root); ToolkitTheme.Apply(root, null);
            _view = new ToolkitGameView(root) { rosterDrag = _drag, screenPointProvider = point => point };
            _view.OnInspectRequested.AddListener(_ => _inspects++);
            _owner = new GameObject("Gesture spell"); _owner.SetActive(false);
            _data = ScriptableObject.CreateInstance<EntityData>();
        }
        void Bind(bool spell, bool enabled = true)
        {
            var model = new ToolkitCardModel { key = "entry", title = "Heal", isEnabled = enabled,
                canDrag = !spell, source = spell ? (object)_owner.AddComponent<CharacterSkillSlot>() : _data,
                activate = _ => _casts++ };
            _view.SetCards(spell ? "spell-list" : "party-list", new[] { model });
            _button = _view.root.Q(spell ? "spell-list" : "party-list").Q<Button>("data-card");
        }
        [TearDown]
        public void Teardown()
        { _view.Release(); _panel.Dispose(); Object.DestroyImmediate(_owner); Object.DestroyImmediate(_data); }
        void Down(Vector2 point)
        {
            using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point }))
                { e.target = _button; _button.SendEvent(e); }
        }
        void Move(Vector2 point)
        {
            using (var e = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point }))
                { e.target = _button; _button.SendEvent(e); }
        }
        void Up(Vector2 point)
        {
            using (var e = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point }))
                { e.target = _button; _button.SendEvent(e); }
        }
        IEnumerator Hold()
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + .48;
            while (UnityEditor.EditorApplication.timeSinceStartup < end)
            {
                yield return null;
            }
        }
        [UnityTest]
        public IEnumerator ShortSpellActivatesOnlyOnReleaseExactlyOnce()
        {
            Bind(true); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); Assert.That(_casts, Is.Zero);
            Up(point); Up(point);
            Assert.That(_casts, Is.EqualTo(1)); Assert.That(_inspects, Is.Zero);
        }
        [UnityTest]
        public IEnumerator DisabledSpellCanBeHeldAndItsReleaseNeverCasts()
        {
            Bind(true, false); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); yield return Hold(); Move(point);
            Assert.That(_inspects, Is.EqualTo(1)); Up(point);
            Assert.That(_casts, Is.Zero);
        }
        [UnityTest]
        public IEnumerator HeldUsableSpellReleaseNeverCasts()
        {
            Bind(true); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); yield return Hold(); Up(point);
            Assert.That(_casts, Is.Zero); Assert.That(_inspects, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ScrollCannotBecomeDeploymentAndHoldCannotBecomeDrag()
        {
            Bind(false); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); Move(point + Vector2.left * 30); Move(point + Vector2.down * 100); Up(point);
            Assert.That(_drag.begins, Is.Zero); Assert.That(_casts, Is.Zero);
            Down(point); yield return Hold(); Move(point); Move(point + Vector2.down * 100); Up(point);
            Assert.That(_drag.begins, Is.Zero); Assert.That(_inspects, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator OwnedDragEndsOnceAndTeardownCancelsPendingPreview()
        {
            Bind(false); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); Move(point + Vector2.down * 30); Up(point + Vector2.down * 40); Up(point);
            Assert.That(_drag.begins, Is.EqualTo(1)); Assert.That(_drag.ends, Is.EqualTo(1));
            Down(point); Move(point + Vector2.down * 30);
            _view.Release();
            Assert.That(_drag.active, Is.False); Assert.That(_casts, Is.Zero);
        }
        [UnityTest]
        public IEnumerator InterruptThenReleaseHasNoAction()
        {
            Bind(false); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            Down(point); Move(point + Vector2.down * 30); _view.CancelGestures(); Up(point);
            Assert.That(_drag.ends, Is.Zero); Assert.That(_drag.active, Is.False);
            Assert.That(_casts, Is.Zero);
        }
        [UnityTest]
        public IEnumerator OrphanedReleaseCannotSynthesizeAnotherSpellPress()
        {
            Bind(true); yield return null; yield return null;
            Vector2 point = _button.worldBound.center;
            _view.canBeginPointer = _ => true;
            Down(point); yield return Hold(); Up(point);
            _view.canBeginPointer = _ => false;
            Down(point); Up(point);
            Assert.That(_casts, Is.Zero); Assert.That(_inspects, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ExpiredRosterChoiceCannotCommitPendingDrag()
        {
            Bind(false); yield return null; yield return null;
            bool preparing = true;
            ((ToolkitCardModel)_button.userData).canBeginDrag = () => preparing;
            Vector2 point = _button.worldBound.center;
            Down(point); Move(point + Vector2.down * 30);
            preparing = false; Up(point + Vector2.down * 40);
            Assert.That(_drag.ends, Is.Zero); Assert.That(_drag.active, Is.False);
        }
        [UnityTest]
        public IEnumerator OrphanedNativePressDoesNotDismissPersistentCreatureDetails()
        {
            Bind(false); yield return null; yield return null;
            using (var popover = new ToolkitPopover(_view))
            {
                Vector2 point = _button.worldBound.center;
                Down(point); yield return Hold(); Up(point);
                Assert.That(popover.isOpen, Is.True);
                _view.canBeginPointer = _ => false;
                Down(point); Up(point);
                Assert.That(popover.isOpen, Is.True);
                Assert.That(_casts, Is.Zero);
            }
        }
        [UnityTest]
        public IEnumerator ControllerCancelInspectsUnavailableSpellAndSubmitKeepsActivationMeaning()
        {
            Bind(true, false); yield return null; yield return null;
            using (var popover = new ToolkitPopover(_view))
            {
                _button.Focus();
                using (var inspect = NavigationCancelEvent.GetPooled())
                {
                    _button.SendEvent(inspect);
                }

                Assert.That(popover.isOpen, Is.True);
                Assert.That(_inspects, Is.EqualTo(1)); Assert.That(_casts, Is.Zero);
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    _button.SendEvent(submit);
                }

                Assert.That(popover.isOpen, Is.False); Assert.That(_casts, Is.Zero);
                Bind(true); yield return null;
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    _button.SendEvent(submit);
                }

                Assert.That(_casts, Is.EqualTo(1));
                using (var inspect = NavigationCancelEvent.GetPooled())
                {
                    _button.SendEvent(inspect);
                }

                Assert.That(popover.isOpen, Is.True);
                using (var inspect = NavigationCancelEvent.GetPooled())
                {
                    _button.SendEvent(inspect);
                }

                Assert.That(popover.isOpen, Is.False); Assert.That(_casts, Is.EqualTo(1));
            }
        }
        void Key(KeyCode key)
        {
            using (var evt = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = key }))
            {
                _button.SendEvent(evt);
            }
        }
        void NavigationCancel()
        {
            using (var evt = NavigationCancelEvent.GetPooled())
            {
                _button.SendEvent(evt);
            }
        }
        // Synthetic keys cannot set legacy Input.GetKeyDown. Invoke its existing route
        // once, after or before navigation, without adding a second production key owner.
        void Escape(ToolkitGameActions actions, bool actionsFirst)
        {
            Key(KeyCode.Escape);
            if (actionsFirst)
            {
                TestHelpers.InvokePrivate(actions, "OnEscape");
            }

            NavigationCancel();
            if (!actionsFirst)
            {
                TestHelpers.InvokePrivate(actions, "OnEscape");
            }
        }
        void WithEscapeRoute(System.Action<ToolkitGameActions, ToolkitGameContext> check)
        {
            var settings = ToolkitMobileLayout.CreatePanelSettings(null);
            var document = _owner.GetComponent<UIDocument>() ?? _owner.AddComponent<UIDocument>();
            document.panelSettings = settings;
            var context = new ToolkitGameContext();
            float speed = Time.timeScale;
            using (var mobile = new ToolkitMobileLayout())
            using (var time = new ToolkitTimeControls())
            using (var map = new ToolkitMapPanel())
            {
                mobile.Init(_view, context, document);
                time.Init(null, context, _view);
                using (var actions = new ToolkitGameActions(null, context, _view, time, mobile, map))
                {
                    try { check(actions, context); }
                    finally { time.Resume(); Time.timeScale = speed; document.panelSettings = null;
                        Object.DestroyImmediate(settings); }
                }
            }
        }
        [UnityTest]
        public IEnumerator KeyboardEscapeWithFocusedCardPausesWithoutInspection()
        {
            Bind(true); yield return null; yield return null;
            _button.Focus();
            foreach (bool actionsFirst in new[] { false, true })
            {
                WithEscapeRoute((actions, context) =>
                {
                    Escape(actions, actionsFirst);
                    Assert.That(context.isPaused, Is.True, "Escape was swallowed by inspection");
                    Assert.That(_view.root.Q("detail-panel").ClassListContains("is-hidden"), Is.True);
                    Assert.That(_inspects, Is.Zero); Assert.That(_casts, Is.Zero);
                });
            }
        }
        [UnityTest]
        public IEnumerator KeyboardEscapeClosesNavigationInspectionWithoutPausing()
        {
            foreach (bool actionsFirst in new[] { false, true })
            {
                // A fresh entry also avoids relying on Editor Time.frameCount advancing.
                _view.SetCards("spell-list", new ToolkitCardModel[0]);
                Bind(true); yield return null; yield return null;
                _button.Focus();
                WithEscapeRoute((actions, context) =>
                {
                    NavigationCancel();
                    Assert.That(_view.root.Q("detail-panel").ClassListContains("is-hidden"), Is.False);
                    int inspections = _inspects;
                    Escape(actions, actionsFirst);
                    Assert.That(_view.root.Q("detail-panel").ClassListContains("is-hidden"), Is.True);
                    Assert.That(context.isPaused, Is.False, "Escape closed inspection then also paused");
                    Assert.That(_inspects, Is.EqualTo(inspections)); Assert.That(_casts, Is.Zero);
                });
            }
        }
        [UnityTest]
        public IEnumerator KeyboardEscapeCancelsSpellTargetingWithoutInspectionOrPause()
        {
            Bind(true); yield return null; yield return null;
            _button.Focus();
            var manager = _owner.AddComponent<InteractionManager>();
            foreach (bool actionsFirst in new[] { false, true })
            {
                WithEscapeRoute((actions, context) =>
                {
                    var targeting = new Targeting();
                    manager.SetInteraction(targeting);
                    context.interaction = manager;
                    Escape(actions, actionsFirst);
                    Assert.That(manager.GetInteraction(), Is.Null, "Escape failed to cancel targeting");
                    Assert.That(targeting.cancels, Is.EqualTo(1));
                    Assert.That(context.isPaused, Is.False);
                    Assert.That(_inspects, Is.Zero); Assert.That(_casts, Is.Zero);
                });
            }
        }
        [UnityTest]
        public IEnumerator KeyboardInspectShortcutsKeepInspectionWithoutActivation()
        {
            Bind(true); yield return null; yield return null;
            _button.Focus();
            using (var popover = new ToolkitPopover(_view))
            {
                foreach (KeyCode key in new[] { KeyCode.I, KeyCode.F1 })
                {
                    Key(key);
                    Assert.That(popover.isOpen, Is.True); Assert.That(_casts, Is.Zero);
                    _view.ClosePopover();
                }
            }

            Assert.That(_inspects, Is.EqualTo(2));
        }
        sealed class Targeting : AInteraction
        {
            public int cancels;
            public override int GetLayerMask() => 0;
            public override void Cancel() { cancels++; }
        }
        sealed class Drag : IToolkitRosterDrag
        {
            public int begins, ends;
            public bool active;
            public bool Begin(EntityData data, Vector2 point, System.Action<Entity> deployed)
            { begins++; active = true; return true; }
            public void Move(Vector2 point) { }
            public bool End(Vector2 point) { if (active) { ends++; } active = false; return true; }
            public void Cancel() { active = false; }
        }
    }
}
