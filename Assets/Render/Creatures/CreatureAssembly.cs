using System.Collections.Generic;
using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Retained pivots keep held effects attached while accepted recipe values and owned meshes change together.
    public class CreatureAssembly : IDisposable
    {
        readonly List<CreaturePartView> _keptParts = new List<CreaturePartView>();
        readonly RootChain _roots = new RootChain();
        ShapeMeshCache _shapeMeshes = new ShapeMeshCache();
        Transform _seedSource;
        public CreatureSelection selection { get; set; }
        Transform[] _geometry = Array.Empty<Transform>();
        Renderer[] _renderers = Array.Empty<Renderer>();
        Transform[] _buds = Array.Empty<Transform>();
        public Transform root { get; private set; }
        public Transform sway { get; private set; }
        public CreatureRigData data { get; private set; }
        public int seed { get; private set; }

        public IReadOnlyList<Transform> geometry
        {
            get { return _geometry; }
        }

        public Transform[] buds
        {
            get { return _buds; }
        }

        public void Init(Transform parent, Transform seedSource = null)
        {
            _seedSource = seedSource ? seedSource : parent;
            root = new GameObject("GeneratedCreature").transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one / parent.lossyScale.x;
            sway = new GameObject("Sway").transform;
            sway.SetParent(root, false);
        }

        public bool Recompose(
            CreatureRecipe recipe,
            Material material,
            Material bodyMaterial,
            PrimitiveMeshes meshes,
            float cellSize,
            float health
        )
        {
            if (
                recipe.roots.count > 0
                && (
                    (!recipe.roots.segmentShape.isProcedural && meshes.cylinder == null)
                    || (!recipe.roots.jointShape.isProcedural && meshes.sphere == null)
                )
            )
            {
                return false;
            }

            ShapeMeshCache nextMeshes = new ShapeMeshCache();
            Mesh[] resolved = new Mesh[recipe.parts.Length];
            for (int i = 0; i < recipe.parts.Length; i++)
            {
                CreaturePart part = recipe.parts[i];
                resolved[i] = part.shape.isProcedural
                    ? nextMeshes.Get(part.shape, part.variant)
                    : meshes.GetMesh(part.primitive, part.variant);
                if (resolved[i] == null)
                {
                    nextMeshes.Dispose();
                    return false;
                }
            }

            CreatureRigData accepted = new CreatureRigData(recipe);
            int nextSeed = accepted.idle.seed ^ _seedSource.GetEntityId().GetHashCode();
            foreach (CreaturePartView view in _keptParts)
            {
                view.pivot.SetParent(sway, false);
                view.pivot.gameObject.SetActive(false);
            }

            _geometry = new Transform[accepted.parts.Length];
            _renderers = new Renderer[accepted.parts.Length];
            List<Transform> buds = new List<Transform>();
            Color ochre = Color.Lerp(accepted.wiltColour, accepted.stoneOchre, health);
            for (int i = 0; i < accepted.parts.Length; i++)
            {
                CreaturePart part = accepted.parts[i];
                if (i == _keptParts.Count)
                {
                    _keptParts.Add(new CreaturePartView());
                }

                Transform parent = part.parent >= 0 ? _keptParts[part.parent].pivot : sway;
                bool isBody = part.role == PartRole.Body || part.role == PartRole.Head || part.role == PartRole.Stem;
                _keptParts[i]
                    .Init(part, parent, resolved[i], isBody ? bodyMaterial : material, cellSize, nextSeed, ochre);
                _geometry[i] = _keptParts[i].geometry;
                _renderers[i] = _keptParts[i].renderer;
                if (part.role == PartRole.Tip)
                {
                    buds.Add(_keptParts[i].pivot);
                }
            }

            data = accepted;
            seed = nextSeed;
            MeasureAppearance();
            _buds = buds.ToArray();
            _roots.Init(data.roots, root, meshes, material, ColourJitter.Vary(data.roots.colour, seed));
            _shapeMeshes.Dispose();
            _shapeMeshes = nextMeshes;
            return true;
        }

        void MeasureAppearance()
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            float[] heights = new float[data.parts.Length];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = sway.InverseTransformPoint(_keptParts[i].pivot.position).y;
                min = Mathf.Min(min, heights[i]);
                max = Mathf.Max(max, heights[i]);
            }

            for (int i = 0; i < heights.Length; i++)
            {
                _keptParts[i].SetAppearanceDelay(Mathf.InverseLerp(min, max, heights[i]));
            }
        }

        public void Tick(
            float time,
            IdlePose idle,
            float cellSize,
            float pulse,
            float charge,
            float health,
            float light,
            float elapsed
        )
        {
            Color ochre = Color.Lerp(data.wiltColour, data.stoneOchre, health);
            for (int i = 0; i < data.parts.Length; i++)
            {
                _keptParts[i].Tick(time, idle, cellSize, pulse, charge, health, light, elapsed,
                    data.wiltColour, ochre, selection);
            }

            _roots.Place(sway, root, cellSize, elapsed);
        }

        public void CollectBodyMeshes(List<MeshFilter> into)
        {
            _roots.CollectMeshes(into);
            foreach (Transform part in _geometry)
            {
                MeshFilter filter = part ? part.GetComponent<MeshFilter>() : null;
                if (filter != null)
                {
                    into.Add(filter);
                }
            }
        }

        public Transform Pivot(int index)
        {
            return _keptParts[index].pivot;
        }

        public bool TryGetAnchors(float cellSize, out EffectAnchors anchors)
        {
            if (data == null || data.parts.Length == 0)
            {
                anchors = new EffectAnchors();
                return false;
            }

            return PartAnchors.TryMeasure(
                data.parts,
                data.neckLocal,
                data.sourceLocal,
                root,
                _keptParts[0].pivot,
                _renderers,
                cellSize,
                out anchors
            );
        }

        public Transform SourceTransform()
        {
            if (data == null) return null;
            for (int i = 0; i < data.parts.Length && i < _keptParts.Count; i++)
            {
                if (data.parts[i].isSource && _keptParts[i].pivot.gameObject.activeInHierarchy)
                    return _keptParts[i].pivot;
            }
            return null;
        }

        public void Dispose()
        {
            _roots.Clear();
            _shapeMeshes.Dispose();
            data = null;
            _geometry = Array.Empty<Transform>();
            _renderers = Array.Empty<Renderer>();
            _buds = Array.Empty<Transform>();
            Transform releasedRoot = root;
            root = null;
            sway = null;
            _keptParts.Clear();
            if (releasedRoot)
            {
                releasedRoot.gameObject.SetActive(false);
                RenderObjects.Release(releasedRoot.gameObject);
            }
        }
    }
}
