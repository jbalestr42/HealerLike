using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static UI.Toolkit.ToolkitTouchFixture;

namespace UI.Toolkit
{
    public class ToolkitTouchInputTests
    {
        ToolkitTouchFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ToolkitTouchFixture();
            _fixture.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void PointerStartGateOnlyInterpretsLegacyTouchIdsForStandaloneModule()
        {
            GameObject host = new GameObject("Pointer module fixture");
            host.SetActive(false);
            try
            {
                var legacy = host.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Touch[] samples = new[] { new Touch { fingerId = 2, phase = TouchPhase.Began } };
                int pointer = UnityEngine.UIElements.PointerId.touchPointerIdBase + 2;
                Assert.That(ToolkitTouchInput.CanBeginPointer(legacy, pointer, samples), Is.True);
                samples[0] = new Touch { fingerId = 2, phase = TouchPhase.Ended };
                Assert.That(ToolkitTouchInput.CanBeginPointer(legacy, pointer, samples), Is.False);
                Assert.That(ToolkitTouchInput.CanBeginPointer(legacy, pointer, System.Array.Empty<Touch>()), Is.False);
                Assert.That(ToolkitTouchInput.CanBeginPointer(legacy, UnityEngine.UIElements.PointerId.mousePointerId, samples), Is.False);
                Assert.That(ToolkitTouchInput.CanBeginPointer(legacy, UnityEngine.UIElements.PointerId.mousePointerId,
                    System.Array.Empty<Touch>()), Is.True);
                var modern = host.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                foreach (int id in new[] { pointer, UnityEngine.UIElements.PointerId.penPointerIdBase,
                    UnityEngine.UIElements.PointerId.mousePointerId })
                {
                    Assert.That(ToolkitTouchInput.CanBeginPointer(modern, id, samples), Is.True);
                }

                Assert.That(ToolkitTouchInput.CanBeginPointer(null, pointer, samples), Is.True);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void OnlyWorldOriginPressDismissesPopoverAndUiOriginCannotLeakToBoard()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                ToolkitGameView view = new ToolkitGameView(root);
                ToolkitGameUI ui = manager.gameObject.AddComponent<ToolkitGameUI>();
                TestHelpers.SetPrivateField(ui, "_view", view);
                input.Init(manager);
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    popover.Open(new ToolkitCardModel { title = "Persistent creature" });
                    input.ProcessTouch(2, TouchPhase.Began, Vector2.left);
                    input.ProcessTouch(2, TouchPhase.Moved, Vector2.right);
                    input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                    Assert.That(popover.isOpen, Is.True);
                    Assert.That(interaction.calls, Is.Empty);
                    input.ProcessTouch(3, TouchPhase.Began, Vector2.right);
                    Assert.That(popover.isOpen, Is.False);
                    Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Ignore));
                    Assert.That(root.Q("world-space").pickingMode, Is.EqualTo(PickingMode.Ignore));
                    input.ProcessTouch(3, TouchPhase.Canceled, Vector2.right);
                    Assert.That(interaction.calls, Is.Empty);
                }
                view.Release();
                TestHelpers.SetPrivateField(ui, "_view", null);
            });
        }

        [Test]
        public void Tap_OnCreature_OpensItsDetails()
        {
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            data.title = "Tapped creature";
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = _fixture.target.AddComponent<Entity>());
            entity.data = data;
            try
            {
                WithPopover((input, popover, view) =>
                {
                    Assert.That(input.Tap(Vector2.right), Is.False, "No selectable on the creature");
                    Assert.That(popover.isOpen, Is.True);
                    ToolkitCardModel shown = null;
                    view.OnInspect.AddListener(model => shown = model);
                    popover.Close();
                    input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                    input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                    Assert.That(popover.isOpen, Is.True, "A world tap opens the creature's details");
                    Assert.That(shown.source, Is.SameAs(entity));
                    Assert.That(shown.title, Is.EqualTo("Tapped creature"));
                });
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Tap_OnGround_KeepsDetailsClosed()
        {
            WithPopover((input, popover, view) =>
            {
                input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                Assert.That(popover.isOpen, Is.False);
            });
        }

        void WithPopover(System.Action<FixtureTouchInput, ToolkitPopover, ToolkitGameView> action)
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                // No interaction in progress: a tap selects instead of activating it
                manager.SetInteraction(null);
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                ToolkitGameView view = new ToolkitGameView(root);
                ToolkitGameUI ui = manager.gameObject.AddComponent<ToolkitGameUI>();
                TestHelpers.SetPrivateField(ui, "_view", view);
                input.Init(manager);
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    action(input, popover, view);
                }

                view.Release();
                TestHelpers.SetPrivateField(ui, "_view", null);
            });
        }

        [Test]
        public void NativeCancellationRunsBeforeTheEventSystemReleaseTranslation()
        {
            var order = (DefaultExecutionOrder)System.Attribute.GetCustomAttribute(typeof(ToolkitTouchInput),
                typeof(DefaultExecutionOrder));
            string[] scripts = UnityEditor.AssetDatabase.FindAssets("EventSystem t:MonoScript", new[] { "Packages/com.unity.ugui" });
            bool checkedSystem = false;
            foreach (string guid in scripts)
            {
                var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (script.GetClass() != typeof(UnityEngine.EventSystems.EventSystem))
                {
                    continue;
                }

                Assert.That(order.order, Is.LessThan(UnityEditor.MonoImporter.GetExecutionOrder(script)));
                checkedSystem = true;
            }
            Assert.That(checkedSystem, Is.True);
        }

        [Test]
        public void FirstTap_EntersPreviewsAndActivatesWithoutPreviousHover()
        {
            TapInteraction interaction = new TapInteraction();
            Assert.That(ToolkitTouchInput.Activate(interaction, _fixture.hit), Is.True);
            CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
        }

        [Test]
        public void InvalidTarget_DoesNotPreviewOrActivate()
        {
            TapInteraction interaction = new TapInteraction();
            interaction.isValid = false;
            Assert.That(ToolkitTouchInput.Activate(interaction, _fixture.hit), Is.False);
            Assert.That(interaction.calls, Is.Empty);
        }

        [Test]
        public void EmptyHit_DoesNotActivate()
        {
            TapInteraction interaction = new TapInteraction();
            Assert.That(ToolkitTouchInput.Activate(interaction, default), Is.False);
            Assert.That(interaction.calls, Is.Empty);
        }

        [Test]
        public void Gesture_StartingOnUi_CannotActivateWhenReleasedOverWorld()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                input.ProcessTouch(2, TouchPhase.Began, Vector2.left);
                input.ProcessTouch(2, TouchPhase.Moved, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                Assert.That(interaction.calls, Is.Empty);
            });
        }

        [Test]
        public void Gesture_FirstWorldTap_ActivatesOnce()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
            });
        }

        [Test]
        public void Update_HeldWorldTouch_KeepsInteractionUntilReleaseAndRestoresMouse()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                Assert.That(manager.enabled, Is.False);
                for (int frame = 0; frame < 12; frame++)
                {
                    UpdateTouch(input, TouchPhase.Stationary, Vector2.right);
                    Assert.That(manager.GetInteraction(), Is.SameAs(interaction));
                    Assert.That(interaction.calls, Is.Empty);
                }

                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
                input.captureTouches = System.Array.Empty<Touch>();
                UpdateInput(input);
                Assert.That(manager.enabled, Is.True);
                Assert.That(interaction.calls.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void Update_UiOwnedTouch_RemainsBlockedAcrossFramesAndNextWorldTapWorks()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                UpdateTouch(input, TouchPhase.Began, Vector2.left);
                UpdateTouch(input, TouchPhase.Stationary, Vector2.left);
                UpdateTouch(input, TouchPhase.Moved, Vector2.right);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                Assert.That(interaction.calls, Is.Empty);
                input.captureTouches = System.Array.Empty<Touch>();
                UpdateInput(input);
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                UpdateTouch(input, TouchPhase.Stationary, Vector2.right);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
            });
        }

        [Test]
        public void Init_DuringHeldTouch_RestoresThePreviousMouseOwner()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                Assert.That(manager.enabled, Is.False);
                input.Init(null);
                Assert.That(manager.enabled, Is.True);
                Assert.That(interaction.calls, Is.Empty);
                input.Init(manager);
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
            });
        }

        [Test]
        public void Gesture_DisabledLegacyInput_DoesNotReactivateOrDispatch()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                manager.enabled = false;
                input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                input.enabled = false;
                Assert.That(interaction.calls, Is.Empty);
                Assert.That(manager.enabled, Is.False);
            });
        }

        [Test]
        public void Gesture_CancelledTouch_DoesNotActivate()
        {
            _fixture.WithInput((input, manager, interaction) =>
            {
                input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Canceled, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                Assert.That(interaction.calls, Is.Empty);
            });
        }

        [Test]
        public void WorldGesture_PreservesUnitDragLifecycle()
        {
            FixtureDraggable drag = _fixture.target.AddComponent<FixtureDraggable>();
            _fixture.WithInput((input, manager, interaction) =>
            {
                manager.EndInteraction();
                input.ProcessTouch(4, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.right * 100f);
                input.ProcessTouch(4, TouchPhase.Ended, Vector2.right * 100f);
                CollectionAssert.AreEqual(new[] { "start", "drag", "drag", "end" }, drag.calls);
            });
        }

        [Test]
        public void DragCrossingInterface_CancelsWithoutReleasingIntoBoard()
        {
            FixtureDraggable drag = _fixture.target.AddComponent<FixtureDraggable>();
            _fixture.WithInput((input, manager, interaction) =>
            {
                manager.EndInteraction();
                input.ProcessTouch(4, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.right * 100f);
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.left);
                input.ProcessTouch(4, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "start", "drag", "cancel" }, drag.calls);
            });
        }

    }
}
