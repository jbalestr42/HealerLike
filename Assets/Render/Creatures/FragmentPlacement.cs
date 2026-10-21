using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Resolve explicit attachment points while composing. The resulting recipe remains independent mesh parts.
    public static class FragmentPlacement
    {
        public static bool TryValidate(LookPart[] fragment, CountBand band, out string error)
        {
            error = null;
            if (fragment == null || fragment.Length == 0)
            {
                error = "The fragment has no parts.";
                return false;
            }

            Dictionary<string, int> first = new Dictionary<string, int>();
            HashSet<string> ambiguous = new HashSet<string>();
            for (int i = 0; i < fragment.Length; i++)
            {
                LookPart part = fragment[i];
                if (
                    string.IsNullOrEmpty(part.id)
                    || !Valid(part.pivot)
                    || !Valid(part.attachAt)
                    || !part.shape.IsValid()
                    || (part.isSource && !CreatureSources.Valid(part.sourceAnchor))
                )
                {
                    error = "A fragment part has an invalid name, anchor or shape profile.";
                    return false;
                }

                if (first.ContainsKey(part.id))
                {
                    ambiguous.Add(part.id);
                }
                else
                {
                    first.Add(part.id, i);
                }
            }

            for (int i = 0; i < fragment.Length; i++)
            {
                LookPart part = fragment[i];
                if (part.minCount > band || string.IsNullOrEmpty(part.attachTo))
                {
                    continue;
                }

                if (
                    !first.TryGetValue(part.attachTo, out int target)
                    || target >= i
                    || ambiguous.Contains(part.attachTo)
                    || fragment[target].minCount > band
                )
                {
                    error =
                        "Part '"
                        + part.id
                        + "' must attach to an earlier unique active part in its fragment: '"
                        + part.attachTo
                        + "'.";
                    return false;
                }
            }

            return true;
        }

        public static bool TryResolve(
            LookPart[] fragment,
            CountBand band,
            int seed,
            int firstPart,
            out LookPart[] resolved,
            out string error
        )
        {
            resolved = null;
            if (!TryValidate(fragment, band, out error))
            {
                return false;
            }

            List<LookPart> placed = new List<LookPart>(fragment.Length);
            Dictionary<string, int> byId = new Dictionary<string, int>();
            Dictionary<(ShapeProfile, ShapeAnchor, int), Vector3> anchors =
                new Dictionary<(ShapeProfile, ShapeAnchor, int), Vector3>();
            foreach (LookPart source in fragment)
            {
                if (source.minCount > band)
                {
                    continue;
                }

                LookPart part = source;
                Vector3 point = source.position;
                if (!string.IsNullOrEmpty(source.attachTo))
                {
                    int targetIndex = byId[source.attachTo];
                    LookPart target = placed[targetIndex];
                    Vector3 targetAnchor = Anchor(
                        target.shape,
                        source.attachAt,
                        LookComposer.Variant(seed, firstPart + targetIndex),
                        anchors
                    );
                    point +=
                        target.position + Quaternion.Euler(target.euler) * Vector3.Scale(target.size, targetAnchor);
                }

                Vector3 ownAnchor = Anchor(
                    source.shape,
                    source.pivot,
                    LookComposer.Variant(seed, firstPart + placed.Count),
                    anchors
                );
                part.position = point - Quaternion.Euler(source.euler) * Vector3.Scale(source.size, ownAnchor);
                byId[source.id] = placed.Count;
                placed.Add(part);
            }

            resolved = placed.ToArray();
            return true;
        }

        static Vector3 Anchor(
            ShapeProfile shape,
            ShapeAnchor anchor,
            int variant,
            Dictionary<(ShapeProfile, ShapeAnchor, int), Vector3> cache
        )
        {
            if (anchor == ShapeAnchor.Center)
            {
                return Vector3.zero;
            }

            if (shape.kind != ShapeKind.Block && shape.kind != ShapeKind.Shard)
            {
                variant = 0;
            }

            (ShapeProfile shape, ShapeAnchor anchor, int variant) key = (shape, anchor, variant);
            if (!cache.TryGetValue(key, out Vector3 point))
            {
                point = ProceduralShapeMeshes.Anchor(shape, anchor, variant);
                cache.Add(key, point);
            }

            return point;
        }

        static bool Valid(ShapeAnchor anchor)
        {
            return anchor >= ShapeAnchor.Center && anchor <= ShapeAnchor.Top;
        }

        public static bool Add(
            PartList parts,
            LookVocabulary vocabulary,
            UnitChannels channels,
            LookPart[] fragment,
            Vector3 at,
            float scale,
            CountBand band,
            int seed
        )
        {
            if (
                !FragmentPlacement.TryResolve(
                    fragment,
                    band,
                    seed,
                    parts.count,
                    out LookPart[] resolved,
                    out string error
                )
            )
            {
                Debug.LogError("[LookComposer] " + error);
                return false;
            }

            foreach (LookPart part in resolved)
            {
                Color colour = vocabulary.Colour(part.colour, channels.accent, channels.side);
                parts.Add(
                    part.id,
                    part.primitive,
                    at + part.position * scale,
                    part.size * scale,
                    colour,
                    part.euler,
                    part.glow,
                    part.role,
                    LookComposer.Variant(seed, parts.count),
                    part.shape,
                    part.isSource && parts.accessoryStart < 0,
                    part.sourceAnchor,
                    ((int)channels.head).ToString()
                );
            }

            return true;
        }
    }
}
