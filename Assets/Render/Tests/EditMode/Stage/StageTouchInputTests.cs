using NUnit.Framework;
using UnityEngine;
using static HealerLike.Render.Stage.StageTouchFixture;

namespace HealerLike.Render.Stage
{
    public class StageTouchInputTests
    {
        StageTouchFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new StageTouchFixture();
            _fixture.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void FirstTap_EntersPreviewsAndActivatesWithoutPreviousHover()
        {
            TapInteraction interaction = new TapInteraction();
            Assert.That(StageTouchInput.Activate(interaction, _fixture.hit), Is.True);
            CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
        }

        [Test]
        public void InvalidTarget_DoesNotPreviewOrActivate()
        {
            TapInteraction interaction = new TapInteraction();
            interaction.isValid = false;
            Assert.That(StageTouchInput.Activate(interaction, _fixture.hit), Is.False);
            Assert.That(interaction.calls, Is.Empty);
        }

        [Test]
        public void EmptyHit_DoesNotActivate()
        {
            TapInteraction interaction = new TapInteraction();
            Assert.That(StageTouchInput.Activate(interaction, default), Is.False);
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
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.right * 2f);
                input.ProcessTouch(4, TouchPhase.Ended, Vector2.right * 2f);
                CollectionAssert.AreEqual(new[] { "start", "drag", "end" }, drag.calls);
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
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.left);
                input.ProcessTouch(4, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "start", "cancel" }, drag.calls);
            });
        }

    }
}
