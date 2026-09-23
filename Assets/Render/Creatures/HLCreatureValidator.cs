using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class HLCreatureValidator
    {
        public static bool TryValidate(HLCreatureRecipe data, out string error)
        {
            error = null;
            if (!data || data.parts == null || data.parts.Length == 0 || data.parts.Length > 40)
            {
                return Fail("Require 1..40 parts.", out error);
            }

            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < data.parts.Length; i++)
            {
                HLPart part = data.parts[i];
                bool isParentValid = i == 0 ? part.parent == -1 : part.parent >= 0 && part.parent < i;
                if (string.IsNullOrEmpty(part.id) || !ids.Add(part.id) || !isParentValid)
                {
                    return Fail("Require unique IDs, one root, and earlier parents.", out error);
                }

                float ratio = part.torusTubeRatio;
                bool isTorusValid = part.primitive != HLPrimitive.Torus
                    || (HLChainSolver.Finite(ratio) && ratio > 0f && ratio < 1f);
                if (!HLChainSolver.Finite(part.localPosition) || !HLChainSolver.Finite(part.localEuler)
                    || !Positive(part.dimensions) || !Colour(part.colour)
                    || !HLChainSolver.Finite(part.glow) || part.glow < 0f
                    || (int)part.primitive < 0 || (int)part.primitive > 4 || !isTorusValid)
                {
                    return Fail("Invalid primitive settings.", out error);
                }
            }

            if (!HLChainSolver.Finite(data.targetLocal) || data.sourceLocal == null || data.arms == null
                || data.arms.Length > 8)
            {
                return Fail("Invalid sockets or arms.", out error);
            }

            foreach (Vector3 source in data.sourceLocal)
            {
                if (!HLChainSolver.Finite(source))
                {
                    return Fail("Nonfinite socket.", out error);
                }
            }

            foreach (HLArmDefinition arm in data.arms)
            {
                if (arm.bodyPart < 0 || arm.bodyPart >= data.parts.Length
                    || arm.sourceSocketIndex < 0 || arm.sourceSocketIndex >= data.sourceLocal.Length
                    || arm.segmentCount < 2 || arm.segmentCount > 128
                    || !Positive(arm.segmentLength) || !Positive(arm.radius)
                    || !HLChainSolver.Finite(arm.rootLocal) || !HLChainSolver.Finite(arm.bendPole)
                    || !Colour(arm.colour)
                    || arm.restJoints == null || arm.restJoints.Length != arm.segmentCount + 1
                    || arm.restJoints[0] != Vector3.zero)
                {
                    return Fail("Invalid arm or missing source socket.", out error);
                }

                for (int i = 0; i <= arm.segmentCount; i++)
                {
                    if (!HLChainSolver.Finite(arm.restJoints[i]))
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

            HLRootDefinition roots = data.roots;
            if (roots.count < 4 || roots.count > 8 || !Positive(roots.footRadius) || !Positive(roots.thickness)
                || roots.footRadius + roots.thickness > 0.46f
                || !Positive(roots.hipHeight) || !Positive(roots.kneeHeight)
                || !HLChainSolver.Finite(roots.angularOffset) || !Colour(roots.colour))
            {
                return Fail("Roots exceed the cell footprint or have invalid settings.", out error);
            }

            HLIdleDefinition idle = data.idle;
            if (!Nonnegative(idle.swayDegrees) || !Nonnegative(idle.swayFrequency) || !Nonnegative(idle.breathAmount)
                || idle.breathAmount >= 1f || !Nonnegative(idle.breathFrequency))
            {
                return Fail("Invalid idle settings.", out error);
            }

            return true;
        }

        static bool Positive(float value)
        {
            return HLChainSolver.Finite(value) && value > 0f;
        }

        static bool Nonnegative(float value)
        {
            return HLChainSolver.Finite(value) && value >= 0f;
        }

        static bool Positive(Vector3 value)
        {
            return Positive(value.x) && Positive(value.y) && Positive(value.z);
        }

        static bool Colour(Color colour)
        {
            return HLChainSolver.Finite(colour.r) && HLChainSolver.Finite(colour.g)
                && HLChainSolver.Finite(colour.b) && HLChainSolver.Finite(colour.a);
        }

        static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
