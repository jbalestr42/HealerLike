using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // Measures a composed unit on the board camera's screen plane: how far its accessory reaches past the outline,
    // how large its heads read and how far apart a fan of them sits
    public static class LookMeasure
    {
        // The board camera's pitch, an accessory is measured on its screen plane
        static readonly Quaternion boardCamera = Quaternion.Euler(StageCalibration.PortraitPitch, 0f, 0f);
        // An accessory part is sampled at its centre and its six face centres
        static readonly int samples = 7;

        // How far the accessory reaches past the body and head on the board camera's screen plane, in cells
        public static float AccessoryReach(UnitChannels channels, LookVocabulary vocabulary)
        {
            PartList parts = LookComposer.Layout(channels, vocabulary);
            if (parts == null || channels.accessory == AccessoryKind.None)
            {
                return 0f;
            }
            return OutlineReach(parts, vocabulary.Unit(channels.side));
        }

        // The head's screen box on the board camera, the smaller of its width and height in cells; a fanned head is
        // measured one copy at a time and the smallest copy counts
        public static float HeadSpan(UnitChannels channels, LookVocabulary vocabulary)
        {
            PartList parts = LookComposer.Layout(channels, vocabulary);
            if (parts == null)
            {
                return 0f;
            }

            float span = float.MaxValue;
            for (int i = 0; i < parts.headStarts.Count; i++)
            {
                Rect box = ScreenBox(parts, parts.headStarts[i], HeadEnd(parts, i));
                span = Mathf.Min(span, Mathf.Min(box.width, box.height));
            }
            return span * vocabulary.Unit(channels.side);
        }

        // The smallest screen gap between two neighbouring head copies in cells, negative when their boxes overlap
        public static float HeadGap(UnitChannels channels, LookVocabulary vocabulary)
        {
            PartList parts = LookComposer.Layout(channels, vocabulary);
            if (parts == null)
            {
                return 0f;
            }

            float gap = float.MaxValue;
            for (int i = 1; i < parts.headStarts.Count; i++)
            {
                Rect previous = ScreenBox(parts, parts.headStarts[i - 1], HeadEnd(parts, i - 1));
                Rect box = ScreenBox(parts, parts.headStarts[i], HeadEnd(parts, i));
                float gapX = Mathf.Max(box.xMin - previous.xMax, previous.xMin - box.xMax);
                float gapY = Mathf.Max(box.yMin - previous.yMax, previous.yMin - box.yMax);
                gap = Mathf.Min(gap, Mathf.Max(gapX, gapY));
            }
            return gap * vocabulary.Unit(channels.side);
        }

        // Each accessory part is sampled at its centre and its six face centres, the body and head as the ellipsoids
        // in their boxes
        public static float OutlineReach(PartList parts, float bodyUnit)
        {
            Vector3 right = boardCamera * Vector3.right;
            Vector3 up = boardCamera * Vector3.up;
            float reach = 0f;
            for (int i = Mathf.Max(0, parts.accessoryStart); i < parts.count; i++)
            {
                LookPart part = parts.Source(i);
                Quaternion rotation = Quaternion.Euler(part.euler);
                for (int k = 0; k < samples; k++)
                {
                    Vector3 point = part.position;
                    if (k > 0)
                    {
                        int axis = (k - 1) / 2;
                        Vector3 offset = Vector3.zero;
                        float face = 0.5f;
                        if (k % 2 == 0)
                        {
                            face = -0.5f;
                        }
                        offset[axis] = face * part.size[axis];
                        point += rotation * offset;
                    }

                    Vector2 screen = new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));
                    float nearest = float.MaxValue;
                    for (int j = 0; j < parts.accessoryStart; j++)
                    {
                        nearest = Mathf.Min(nearest, Outside(parts.Source(j), screen, right, up));
                    }

                    reach = Mathf.Max(reach, nearest);
                }
            }
            return reach * bodyUnit;
        }

        // How far a box of this half size reaches along a direction
        public static float Extent(Vector3 half, Vector3 direction)
        {
            Vector3 scaled = Vector3.Scale(half, direction);
            return scaled.magnitude;
        }

        // Generated profiles can reach their unit-box corners, especially bent growth and broad mineral slabs.
        // Their oriented box is a conservative bound; Legacy keeps the original ellipsoid measurement.
        public static float Extent(Vector3 half, Vector3 direction, ShapeProfile shape)
        {
            if (!shape.isProcedural) return Extent(half, direction);
            return Mathf.Abs(half.x * direction.x) + Mathf.Abs(half.y * direction.y)
                + Mathf.Abs(half.z * direction.z);
        }

        // Where head copy i ends: the next copy's branch, the accessory, or the last part
        static int HeadEnd(PartList parts, int copy)
        {
            if (copy + 1 < parts.headStarts.Count)
            {
                return parts.headStarts[copy + 1];
            }
            if (parts.accessoryStart >= 0)
            {
                return parts.accessoryStart;
            }
            return parts.count;
        }

        // The screen box of parts start to end on the board camera, in body units; a fanned copy's branch is skipped
        static Rect ScreenBox(PartList parts, int start, int end)
        {
            Vector3 right = boardCamera * Vector3.right;
            Vector3 up = boardCamera * Vector3.up;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = start; i < end; i++)
            {
                LookPart part = parts.Source(i);
                if (part.id == HeadFan.BranchId)
                {
                    continue;
                }

                Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
                Vector3 half = part.size * 0.5f;
                Vector2 centre = new Vector2(Vector3.Dot(part.position, right), Vector3.Dot(part.position, up));
                Vector2 extent = new Vector2(Extent(half, inverse * right, part.shape), Extent(half, inverse * up, part.shape));
                min = Vector2.Min(min, centre - extent);
                max = Vector2.Max(max, centre + extent);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // How far a screen point lies outside a part's screen rectangle
        static float Outside(LookPart part, Vector2 point, Vector3 right, Vector3 up)
        {
            Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
            Vector3 half = part.size * 0.5f;
            float halfX = Extent(half, inverse * right, part.shape);
            float halfY = Extent(half, inverse * up, part.shape);
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - Vector3.Dot(part.position, right)) - halfX);
            float dy = Mathf.Max(0f, Mathf.Abs(point.y - Vector3.Dot(part.position, up)) - halfY);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
