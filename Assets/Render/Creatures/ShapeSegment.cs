using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public struct ShapeSegment
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public static ShapeSegment Fit(Vector3 bottom, Vector3 top, Vector3 from, Vector3 to, float width)
        {
            Vector3 delta = to - from;
            float lengthSquared = delta.sqrMagnitude;
            ShapeSegment pose = new ShapeSegment { position = from, rotation = Quaternion.identity };
            if (lengthSquared <= 0.000000000001f)
            {
                return pose;
            }

            Vector3 axis = top - bottom;
            float lateral = axis.x * axis.x + axis.z * axis.z;
            if (lateral * width * width >= lengthSquared)
            {
                width = Mathf.Sqrt(lengthSquared / lateral) * 0.99f;
            }

            float height = Mathf.Sqrt(Mathf.Max(0f, lengthSquared - lateral * width * width)) / Mathf.Abs(axis.y);
            pose.scale = new Vector3(width, height, width);
            pose.rotation = Quaternion.FromToRotation(Vector3.Scale(axis, pose.scale), delta);
            Vector3 middle = Vector3.Scale((bottom + top) * 0.5f, pose.scale);
            pose.position = (from + to) * 0.5f - pose.rotation * middle;
            return pose;
        }
    }
}
