using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageTouchFixture : System.IDisposable
    {
        readonly List<Object> _owned = new List<Object>();
        public GameObject target { get; private set; }

        public RaycastHit hit;
        public void Init()
        {
            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _owned.Add(target);
            target.transform.position = new Vector3(13000f, 13000f, 13000f);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(target.transform.position - Vector3.forward * 2f, Vector3.forward, out hit,
                4f));
        }

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                Object item = _owned[i];
                TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(item));
            }

            _owned.Clear();
        }

        public void WithInput(System.Action<FixtureTouchInput, InteractionManager, TapInteraction> action)
        {
            GameObject host = new GameObject("Touch fixture");
            _owned.Add(host);
            InteractionManager manager = host.AddComponent<InteractionManager>();
            FixtureTouchInput input = host.AddComponent<FixtureTouchInput>();
            input.hit = hit;
            input.Init(manager);
            TapInteraction interaction = new TapInteraction();
            manager.SetInteraction(interaction);
            action(input, manager, interaction);
        }

        public void WithPlacement(System.Action<FixtureTouchInput, PlacementInteraction> action)
        {
            GameObject host = new GameObject("Placement touch fixture");
            _owned.Add(host);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            _owned.Add(data);
            data.model = new GameObject("Legacy placement model");
            _owned.Add(data.model);
            PlacementInteraction placement = new PlacementInteraction(data);
            EntityPlacementReadout.TryRead(placement, out _, out GameObject model, out _);
            _owned.Add(model);
            InteractionManager manager = host.AddComponent<InteractionManager>();
            FixtureTouchInput input = host.AddComponent<FixtureTouchInput>();
            input.hit = hit;
            input.Init(manager);
            manager.SetInteraction(placement);
            action(input, placement);
        }

        public static void UpdateTouch(StageTouchInput input, TouchPhase phase, Vector2 position)
        {
            input.captureTouches = new[]
            {
                new Touch
                {
                    fingerId = 2,
                    phase = phase,
                    position = position
                }
            };
            UpdateInput(input);
        }

        public static void UpdateInput(StageTouchInput input)
        {
            MethodInfo update = typeof(StageTouchInput).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);
            update.Invoke(input, null);
        }

        public class PlacementInteraction : EntityGridInteraction
        {
            public int previews;
            public int clicks;
            public PlacementInteraction(EntityData data) : base(data)
            {
            }

            public override bool IsValidTarget(GameObject target)
            {
                return true;
            }

            public override void OnMouseOver(RaycastHit hit)
            {
                previews++;
            }

            public override void OnMouseClick(RaycastHit hit)
            {
                clicks++;
            }

            public override void Cancel()
            {
            }

            public override void End()
            {
            }
        }

        public class FixtureDraggable : MonoBehaviour, IDraggable
        {
            public readonly List<string> calls = new List<string>();
            public bool CanDrag()
            {
                return true;
            }

            public void StartDrag(RaycastHit hit)
            {
                calls.Add("start");
            }

            public void Drag(RaycastHit hit)
            {
                calls.Add("drag");
            }

            public void EndDrag(RaycastHit hit)
            {
                calls.Add("end");
            }

            public void CancelDrag()
            {
                calls.Add("cancel");
            }
        }

        public class FixtureTouchInput : StageTouchInput
        {
            public RaycastHit hit;
            public override bool IsOverInterface(Vector2 point)
            {
                return point.x < 0f;
            }

            protected override bool Raycast(Vector2 point, out RaycastHit result, int mask
                = Physics.DefaultRaycastLayers)
            {
                result = hit;
                return true;
            }
        }

        public class TapInteraction : AInteraction
        {
            public bool isValid = true;
            public readonly List<string> calls = new List<string>();
            public override int GetLayerMask()
            {
                return Physics.DefaultRaycastLayers;
            }

            public override bool IsValidTarget(GameObject target)
            {
                return isValid;
            }

            public override void OnMouseEnter(RaycastHit hit)
            {
                calls.Add("enter");
            }

            public override void OnMouseOver(RaycastHit hit)
            {
                calls.Add("preview");
            }

            public override void OnMouseClick(RaycastHit hit)
            {
                calls.Add("click");
            }
        }
    }
}
