using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Parts laid out in body units around the creature's foot, turned into recipe parts in cells.
    // Every part hangs from the first one, which never rotates, so a part's local position is its offset from it.
    public class PartList
    {
        readonly List<CreaturePart> _parts = new List<CreaturePart>();
        readonly List<Vector3> _positions = new List<Vector3>();
        readonly List<LookPart> _sources = new List<LookPart>();
        readonly HashSet<string> _ids = new HashSet<string>();
        float _unit;

        public int count { get { return _parts.Count; } }

        // The first accessory part, every part before it is body and head
        int _accessoryStart = -1;
        public int accessoryStart { get { return _accessoryStart; } set { _accessoryStart = value; } }

        // The first part of each head copy, the head runs from the first of them to the accessory
        readonly List<int> _headStarts = new List<int>();
        public List<int> headStarts { get { return _headStarts; } }

        public PartList(float unit)
        {
            _unit = unit;
        }

        // Size is the part's bounding box in body units, whatever the mesh's own pivot
        public int Add(string id, Primitive primitive, Vector3 centre, Vector3 size, Color colour, Vector3 euler,
            float glow, PartRole role, int variant = 0)
        {
            _sources.Add(new LookPart
            {
                id = id,
                primitive = primitive,
                role = role,
                position = centre,
                euler = euler,
                size = size,
                glow = glow
            });

            PrimitiveMeshes.Fit(primitive, centre, size, Quaternion.Euler(euler), out Vector3 dimensions,
                out Vector3 pivot);
            string uniqueId = id;
            if (_ids.Contains(id))
            {
                uniqueId = id + _parts.Count;
            }

            _ids.Add(uniqueId);
            // The first part is the body, every other part hangs from it
            int parent = 0;
            Vector3 local = pivot;
            if (_parts.Count > 0)
            {
                local = pivot - _positions[0];
            }
            else
            {
                parent = -1;
            }

            _parts.Add(new CreaturePart
            {
                id = uniqueId,
                parent = parent,
                primitive = primitive,
                localPosition = local * _unit,
                localEuler = euler,
                dimensions = dimensions * _unit,
                colour = colour,
                glow = glow,
                role = role,
                variant = variant
            });
            _positions.Add(pivot);
            return _parts.Count - 1;
        }

        // A capsule from one point to another
        public int Link(string id, Vector3 from, Vector3 to, float thickness, Color colour, PartRole role)
        {
            Vector3 delta = to - from;
            Vector3 euler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
            Vector3 size = new Vector3(thickness, delta.magnitude + thickness, thickness);
            return Add(id, Primitive.Capsule, (from + to) * 0.5f, size, colour, euler, 0f, role);
        }

        // The part as it was added, centre and bounding box in body units before any pivot or mesh correction
        public LookPart Source(int index)
        {
            return _sources[index];
        }

        public CreaturePart[] ToArray()
        {
            return _parts.ToArray();
        }
    }
}
