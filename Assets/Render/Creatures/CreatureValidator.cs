using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class CreatureValidator
    {
        public static bool TryValidate(CreatureRecipe data, out string error)
        {
            error = null;
            if (!data || data.parts == null || data.parts.Length == 0 || data.parts.Length > 40)
            {
                return Fail("Require 1..40 parts.", out error);
            }

            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < data.parts.Length; i++)
            {
                CreaturePart part = data.parts[i];
                bool isParentValid = i == 0 ? part.parent == -1 : part.parent >= 0 && part.parent < i;
                if (string.IsNullOrEmpty(part.id) || !ids.Add(part.id) || !isParentValid)
                {
                    return Fail("Require unique IDs, one root, and earlier parents.", out error);
                }

                float ratio = part.torusTubeRatio;
                bool isTorusValid = part.primitive != Primitive.Torus
                    || (float.IsFinite(ratio) && ratio > 0f && ratio < 1f);
                Vector3 position = part.localPosition;
                Vector3 euler = part.localEuler;
                bool isPositionFinite = float.IsFinite(position.x) && float.IsFinite(position.y)
                    && float.IsFinite(position.z);
                bool isEulerFinite = float.IsFinite(euler.x) && float.IsFinite(euler.y) && float.IsFinite(euler.z);
                if (!isPositionFinite || !isEulerFinite || !Positive(part.dimensions) || !Colour(part.colour)
                    || !float.IsFinite(part.glow) || part.glow < 0f
                    || (int)part.primitive < 0 || (int)part.primitive > 5 || !isTorusValid)
                {
                    return Fail("Invalid primitive settings.", out error);
                }
            }

            Vector3 target = data.targetLocal;
            bool isTargetFinite = float.IsFinite(target.x) && float.IsFinite(target.y) && float.IsFinite(target.z);
            if (!isTargetFinite || data.sourceLocal == null || data.arms == null || data.arms.Length > 8)
            {
                return Fail("Invalid sockets or arms.", out error);
            }

            foreach (Vector3 source in data.sourceLocal)
            {
                if (!float.IsFinite(source.x) || !float.IsFinite(source.y) || !float.IsFinite(source.z))
                {
                    return Fail("Nonfinite socket.", out error);
                }
            }

            foreach (ArmDefinition arm in data.arms)
            {
                Vector3 rootLocal = arm.rootLocal;
                Vector3 pole = arm.bendPole;
                bool isRootFinite = float.IsFinite(rootLocal.x) && float.IsFinite(rootLocal.y)
                    && float.IsFinite(rootLocal.z);
                bool isPoleFinite = float.IsFinite(pole.x) && float.IsFinite(pole.y) && float.IsFinite(pole.z);
                if (arm.bodyPart < 0 || arm.bodyPart >= data.parts.Length
                    || arm.sourceSocketIndex < 0 || arm.sourceSocketIndex >= data.sourceLocal.Length
                    || arm.segmentCount < 2 || arm.segmentCount > 128
                    || !Positive(arm.segmentLength) || !Positive(arm.radius)
                    || !isRootFinite || !isPoleFinite
                    || !Colour(arm.colour)
                    || arm.restJoints == null || arm.restJoints.Length != arm.segmentCount + 1
                    || arm.restJoints[0] != Vector3.zero)
                {
                    return Fail("Invalid arm or missing source socket.", out error);
                }

                for (int i = 0; i <= arm.segmentCount; i++)
                {
                    Vector3 joint = arm.restJoints[i];
                    if (!float.IsFinite(joint.x) || !float.IsFinite(joint.y) || !float.IsFinite(joint.z))
                    {
                        return Fail("Rest pose does not preserve link lengths.", out error);
                    }

                    if (i > 0)
                    {
                        float link = Vector3.Distance(arm.restJoints[i - 1], arm.restJoints[i]);
                        if (Mathf.Abs(link - arm.segmentLength) > 0.00001f)
                        {
                            return Fail("Rest pose does not preserve link lengths.", out error);
                        }
                    }
                }
            }

            RootDefinition roots = data.roots;
            if (roots.count < 4 || roots.count > 14 || roots.segments < 1 || roots.segments > 4
                || !Positive(roots.footRadius) || !Positive(roots.thickness)
                || roots.footRadius + roots.thickness > 0.46f
                || !Positive(roots.hipHeight) || !Positive(roots.kneeHeight)
                || !float.IsFinite(roots.angularOffset) || !Colour(roots.colour))
            {
                return Fail("Roots exceed the cell footprint or have invalid settings.", out error);
            }

            IdleDefinition idle = data.idle;
            if (!Nonnegative(idle.swayDegrees) || !Nonnegative(idle.swayFrequency) || !Nonnegative(idle.breathAmount)
                || idle.breathAmount >= 1f || !Nonnegative(idle.breathFrequency))
            {
                return Fail("Invalid idle settings.", out error);
            }

            return true;
        }

        static bool Positive(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }

        static bool Nonnegative(float value)
        {
            return float.IsFinite(value) && value >= 0f;
        }

        static bool Positive(Vector3 value)
        {
            return Positive(value.x) && Positive(value.y) && Positive(value.z);
        }

        static bool Colour(Color colour)
        {
            return float.IsFinite(colour.r) && float.IsFinite(colour.g)
                && float.IsFinite(colour.b) && float.IsFinite(colour.a);
        }

        static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
