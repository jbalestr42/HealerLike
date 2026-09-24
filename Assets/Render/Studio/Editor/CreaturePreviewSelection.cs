using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The box drawn around the part selected in the parts inspector, twelve thin cylinders along its bounds' edges
    public class CreaturePreviewSelection
    {
        static readonly Color edgeColour = new Color(1f, 0.72f, 0.2f);
        static readonly float padding = 0.018f;
        static readonly float thickness = 0.003f;

        GameObject _box;
        Transform[] _edges;

        public void Init(StudioPreviewScene scene)
        {
            _box = scene.AddChild("Selected Part Bounds");
            _edges = new Transform[12];
            for (int i = 0; i < _edges.Length; i++)
            {
                _edges[i] = PrimitiveMeshes.Geometry("Selection Edge", _box.transform, scene.meshes.cylinder,
                    scene.material, edgeColour);
            }
            _box.SetActive(false);
        }

        // A part index outside the rig hides the box
        public void Show(CreatureRig rig, int partIndex)
        {
            if (!_box)
            {
                return;
            }

            bool isShown = rig != null && partIndex >= 0 && partIndex < rig.partTransforms.Count;
            _box.SetActive(isShown);
            if (!isShown)
            {
                return;
            }

            Bounds bounds = rig.partTransforms[partIndex].GetComponent<Renderer>().bounds;
            bounds.Expand(padding);
            int edge = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int a = 0; a < 2; a++)
                {
                    for (int b = 0; b < 2; b++)
                    {
                        PlaceEdge(_edges[edge], bounds, axis, a, b);
                        edge++;
                    }
                }
            }
        }

        // The edge along axis at the a-th side of the next axis and the b-th side of the last one
        static void PlaceEdge(Transform edge, Bounds bounds, int axis, int a, int b)
        {
            Vector3 start = bounds.min;
            Vector3 end = bounds.min;
            int next = (axis + 1) % 3;
            int last = (axis + 2) % 3;
            start[next] = bounds.min[next];
            if (a == 1)
            {
                start[next] = bounds.max[next];
            }

            start[last] = bounds.min[last];
            if (b == 1)
            {
                start[last] = bounds.max[last];
            }

            end[next] = start[next];
            end[last] = start[last];
            end[axis] = bounds.max[axis];
            PrimitiveMeshes.Segment(edge, start, end, thickness);
        }
    }
}
