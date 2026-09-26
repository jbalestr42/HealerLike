using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // One count per page, both sides at the actual board scale. Render-only fixture, not a gameplay roster.
    public sealed class SpellSourceFixture : IDisposable
    {
        readonly RenderManager _manager;
        readonly StageCaptureSession _session;
        readonly List<(VisualElement element, StyleEnum<Visibility> visibility)> _hiddenUi =
            new List<(VisualElement, StyleEnum<Visibility>)>();
        readonly List<GameObject> _hidden = new List<GameObject>();
        readonly List<CreatureRig> _rigs = new List<CreatureRig>();
        readonly List<ArmPool> _pools = new List<ArmPool>();
        readonly List<Object> _objects = new List<Object>();
        readonly List<VisualElement> _labels = new List<VisualElement>();
        readonly LookVocabulary _vocabulary;
        readonly Material _material;
        readonly VisualElement _ui;

        public SpellSourceFixture(RenderManager manager, StageCaptureSession session)
        {
            _manager = manager;
            _session = session;
            _vocabulary = RenderAssets.Load<LookVocabulary>("Assets/Render/Creatures/Data/LookVocabulary.asset");
            _material = RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat");
            _ui = session.actions.ui.GetComponent<UIDocument>().rootVisualElement;
            foreach (VisualElement child in _ui.Children())
            { _hiddenUi.Add((child, child.style.visibility)); child.style.visibility = Visibility.Hidden; }
            foreach (ARigHost host in Object.FindObjectsByType<ARigHost>(FindObjectsSortMode.None))
                if (host.gameObject.activeSelf)
                { _hidden.Add(host.gameObject); host.gameObject.SetActive(false); }
        }

        public IEnumerator Capture()
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                for (int count = 0; count < 3; count++)
                {
                    Label("GRAMMAR FIXTURE: " + head + " / count " + LookComposer.Copies((CountBand)count),
                        0.04f, 0.04f, 12);
                    Label("Authored grammar at board scale, not roster gameplay", 0.04f, 0.08f, 10);
                    for (int side = 0; side < 2; side++)
                    {
                        float x = 0.5f;
                        float y = side == 0 ? 0.64f : 0.31f;
                        Vector3 at = Ground(x, y);
                        UnitChannels channels = new UnitChannels
                        {
                            side = (LookSide)side, head = head, count = (CountBand)count,
                            stem = StemBand.Steady, mass = MassBand.Light, reach = ReachBand.Short,
                            accessory = AccessoryKind.None, accent = EffectFamily.Damage
                        };
                        CreatureRecipe recipe = LookComposer.Compose(channels, _vocabulary);
                        _objects.Add(recipe);
                        GameObject parent = new GameObject("Grammar fixture " + head);
                        _objects.Add(parent);
                        CreatureRig rig = new CreatureRig();
                        _session.output.Check(rig.Init(recipe, parent.transform, _material, _manager.meshes),
                            "Fixture assembled " + channels.side + "/" + head + "/" + channels.count);
                        _rigs.Add(rig);
                        rig.SetPresentationForward(_manager.gameCamera.transform.forward);
                        // Fixture uses the same art scale as real rigs, with fixture-only board placement.
                        rig.Tick(0f, 0f, new FootFrame(at, Vector3.up, _manager.player.grid.size));
                        int outlets = recipe.parts.Count(p => p.isSource);
                        Label(channels.side + "  " + LookComposer.Copies(channels.count) + " / " + outlets + " outlets",
                            0.04f, 1f - y + 0.06f, 11);
                        using var source = new CastSourceLease(rig);
                        source.TryGet(out Vector3 start);
                        if (side == 0)
                        {
                            ArmPool pool = new ArmPool();
                            pool.Init(rig, _material, _manager.meshes, _manager.deliveryVocabulary);
                            pool.BeginDelivery(1, DeliveryStyle.Direct, null, start + Vector3.right * 0.7f);
                            pool.Tick(0.3f);
                            _pools.Add(pool);
                        }
                        // Every outlet is shown as a short ray, labelled diagnostic geometry on this fixture only.
                        foreach (CreaturePart part in recipe.parts.Where(p => p.isSource))
                        {
                            CreatureSources.Resolve(rig, part.sourceId, out Vector3 point);
                            GameObject ray = new GameObject("Diagnostic outlet ray");
                            _objects.Add(ray);
                            LineRenderer line = ray.AddComponent<LineRenderer>();
                            line.sharedMaterial = _material;
                            line.startColor = line.endColor = Color.yellow;
                            line.startWidth = 0.015f;
                            line.endWidth = 0.006f;
                            line.positionCount = 2;
                            int index = Array.FindIndex(recipe.parts, p => p.sourceId == part.sourceId);
                            Vector3 normal = rig.partTransforms[index].TransformDirection(
                                part.sourceAnchor == ShapeAnchor.Bottom ? Vector3.down : Vector3.up);
                            line.SetPosition(0, point);
                            line.SetPosition(1, point + normal * 0.22f);
                        }
                    }
                    Label("Short rays: all outlets. Plant thread: first held delivery.", 0.04f, 0.93f, 10);
                    yield return AStageRun.Wait(0.1f);
                    yield return _session.Capture("fixture-" + head + "-" + LookComposer.Copies((CountBand)count),
                        "Labelled grammar fixture; diagnostic outlet rays; original board scale");
                    Clear();
                }
            }
        }

        Vector3 Ground(float x, float y)
        {
            Plane ground = new Plane(Vector3.up, Vector3.up * _manager.board.max.y);
            Ray ray = _manager.gameCamera.ViewportPointToRay(new Vector3(x, y));
            ground.Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }

        void Label(string text, float x, float y, int size)
        {
            var label = new Label(text);
            label.style.position = Position.Absolute;
            label.style.left = Length.Percent(x * 100f);
            label.style.top = Length.Percent(y * 100f);
            label.style.fontSize = size;
            label.style.width = Length.Percent(92f);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = Color.white;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            label.pickingMode = PickingMode.Ignore;
            _ui.Add(label);
            _labels.Add(label);
        }

        void Clear()
        {
            foreach (ArmPool pool in _pools) pool.Dispose();
            foreach (CreatureRig rig in _rigs) rig.Dispose();
            foreach (Object value in _objects) if (value) Object.Destroy(value);
            foreach (VisualElement label in _labels) label.RemoveFromHierarchy();
            _pools.Clear(); _rigs.Clear(); _objects.Clear(); _labels.Clear();
        }

        public void Dispose()
        {
            Clear();
            foreach (GameObject hidden in _hidden) if (hidden) hidden.SetActive(true);
            foreach (var entry in _hiddenUi) entry.element.style.visibility = entry.visibility;
        }
    }
}
