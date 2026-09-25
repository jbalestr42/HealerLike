using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // One part of a vocabulary fragment, in body units relative to the socket it hangs from
    [Serializable]
    public struct LookPart
    {
        public string id;
        public Primitive primitive;
        // Legacy keeps the baked primitive; procedural profiles use a centered unit box.
        public ShapeProfile shape;
        public PartRole role;
        public ColourRole colour;
        // Position names this mesh anchor; attachments offset it from an earlier part's anchor in this fragment.
        public ShapeAnchor pivot;
        public string attachTo;
        public ShapeAnchor attachAt;
        public Vector3 position;
        public Vector3 euler;
        public Vector3 size;
        public float glow;
        // The fewest copies the count band must allow for this part to show
        public CountBand minCount;
    }
}
