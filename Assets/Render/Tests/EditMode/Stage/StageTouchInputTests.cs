using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageTouchInputTests
    {
        GameObject _target;
        RaycastHit _hit;

        [SetUp]
        public void SetUp()
        {
            _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _target.transform.position = new Vector3(13000f, 13000f, 13000f);
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(_target.transform.position - Vector3.forward * 2f,
                Vector3.forward, out _hit, 4f), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void FirstTap_EntersPreviewsAndActivatesWithoutPreviousHover()
        {
            TapInteraction interaction = new TapInteraction();
            Assert.That(StageTouchInput.Activate(interaction, _hit), Is.True);
            CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
        }

        [Test]
        public void InvalidTarget_DoesNotPreviewOrActivate()
        {
            TapInteraction interaction = new TapInteraction();
            interaction.isValid = false;
            Assert.That(StageTouchInput.Activate(interaction, _hit), Is.False);
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
            WithInput((input, manager, interaction) =>
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
            WithInput((input, manager, interaction) =>
            {
                input.ProcessTouch(2, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(2, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "enter", "preview", "click" }, interaction.calls);
            });
        }

        [Test]
        public void Gesture_DisabledLegacyInput_DoesNotReactivateOrDispatch()
        {
            WithInput((input, manager, interaction) =>
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
            WithInput((input, manager, interaction) =>
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
            FixtureDraggable drag = _target.AddComponent<FixtureDraggable>();
            WithInput((input, manager, interaction) =>
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
            FixtureDraggable drag = _target.AddComponent<FixtureDraggable>();
            WithInput((input, manager, interaction) =>
            {
                manager.EndInteraction();
                input.ProcessTouch(4, TouchPhase.Began, Vector2.right);
                input.ProcessTouch(4, TouchPhase.Moved, Vector2.left);
                input.ProcessTouch(4, TouchPhase.Ended, Vector2.right);
                CollectionAssert.AreEqual(new[] { "start", "cancel" }, drag.calls);
            });
        }

        public class FixtureDraggable : MonoBehaviour, IDraggable
        {
            public readonly List<string> calls = new List<string>();
            public bool CanDrag() { return true; }
            public void StartDrag(RaycastHit hit) { calls.Add("start"); }
            public void Drag(RaycastHit hit) { calls.Add("drag"); }
            public void EndDrag(RaycastHit hit) { calls.Add("end"); }
            public void CancelDrag() { calls.Add("cancel"); }
        }

        void WithInput(System.Action<FixtureTouchInput, InteractionManager, TapInteraction> action)
        {
            GameObject host = new GameObject("Touch fixture");
            try
            {
                InteractionManager manager = host.AddComponent<InteractionManager>();
                FixtureTouchInput input = host.AddComponent<FixtureTouchInput>();
                input.hit = _hit;
                input.Init(manager);
                TapInteraction interaction = new TapInteraction();
                manager.SetInteraction(interaction);
                action(input, manager, interaction);
            }
            finally
            {
                TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(host));
            }
        }

        public class FixtureTouchInput : StageTouchInput
        {
            public RaycastHit hit;

            public override bool IsOverInterface(Vector2 point)
            {
                return point.x < 0f;
            }

            protected override bool Raycast(Vector2 point, out RaycastHit result, int mask = Physics.DefaultRaycastLayers)
            {
                result = hit;
                return true;
            }
        }

        sealed class TapInteraction : AInteraction
        {
            public bool isValid = true;
            public readonly List<string> calls = new List<string>();

            public override int GetLayerMask() { return Physics.DefaultRaycastLayers; }
            public override bool IsValidTarget(GameObject target) { return isValid; }
            public override void OnMouseEnter(RaycastHit hit) { calls.Add("enter"); }
            public override void OnMouseOver(RaycastHit hit) { calls.Add("preview"); }
            public override void OnMouseClick(RaycastHit hit) { calls.Add("click"); }
        }
    }
}
