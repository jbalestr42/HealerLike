using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class HLCreatureValidator
    {
        public static bool TryValidate(HLCreatureRecipe data, out string error)
        {
            error = null;
            if (!data || data.parts == null || data.parts.Length == 0 || data.parts.Length > 24)
                return Fail("Require 1..24 parts.", out error);
            var ids = new HashSet<string>();
            for (int i = 0; i < data.parts.Length; i++)
            {
                var p = data.parts[i];
                if (string.IsNullOrEmpty(p.id) || !ids.Add(p.id) || (i == 0 ? p.parent != -1 : p.parent < 0 || p.parent >= i))
                    return Fail("Require unique IDs, one root, and earlier parents.", out error);
                if (!HLChainSolver.Finite(p.localPosition) || !HLChainSolver.Finite(p.localEuler) || !Positive(p.dimensions)
                    || !Colour(p.colour) || !HLChainSolver.Finite(p.glow) || p.glow < 0 || (int)p.primitive < 0 || (int)p.primitive > 4
                    || (p.primitive == HLPrimitive.Torus && (!HLChainSolver.Finite(p.torusTubeRatio) || p.torusTubeRatio <= 0 || p.torusTubeRatio >= 1)))
                    return Fail("Invalid primitive settings.", out error);
            }
            if (!HLChainSolver.Finite(data.targetLocal) || data.sourceLocal == null || data.arms == null || data.arms.Length > 8)
                return Fail("Invalid sockets or arms.", out error);
            foreach (var source in data.sourceLocal) if (!HLChainSolver.Finite(source)) return Fail("Nonfinite socket.", out error);
            foreach (var a in data.arms)
            {
                if (a.bodyPart < 0 || a.bodyPart >= data.parts.Length || a.sourceSocketIndex < 0 || a.sourceSocketIndex >= data.sourceLocal.Length
                    || a.segmentCount < 2 || a.segmentCount > 128 || !Positive(a.segmentLength) || !Positive(a.radius)
                    || !HLChainSolver.Finite(a.rootLocal) || !HLChainSolver.Finite(a.bendPole) || !Colour(a.colour)
                    || a.restJoints == null || a.restJoints.Length != a.segmentCount + 1 || a.restJoints[0] != Vector3.zero)
                    return Fail("Invalid arm or missing source socket.", out error);
                for (int i = 0; i <= a.segmentCount; i++)
                    if (!HLChainSolver.Finite(a.restJoints[i]) || (i > 0 && Mathf.Abs(Vector3.Distance(a.restJoints[i - 1], a.restJoints[i]) - a.segmentLength) > 1e-5f))
                        return Fail("Rest pose does not preserve link lengths.", out error);
            }
            var r = data.roots;
            if (r.count < 4 || r.count > 6 || !Positive(r.footRadius) || !Positive(r.thickness) || r.footRadius + r.thickness > .46f
                || !Positive(r.hipHeight) || !Positive(r.kneeHeight) || !HLChainSolver.Finite(r.angularOffset) || !Colour(r.colour))
                return Fail("Roots exceed the cell footprint or have invalid settings.", out error);
            var idle = data.idle;
            if (!Nonnegative(idle.swayDegrees) || !Nonnegative(idle.swayFrequency) || !Nonnegative(idle.breathAmount)
                || idle.breathAmount >= 1 || !Nonnegative(idle.breathFrequency)) return Fail("Invalid idle settings.", out error);
            return true;
        }
        static bool Positive(float v) => HLChainSolver.Finite(v) && v > 0;
        static bool Nonnegative(float v) => HLChainSolver.Finite(v) && v >= 0;
        static bool Positive(Vector3 v) => Positive(v.x) && Positive(v.y) && Positive(v.z);
        static bool Colour(Color c) => HLChainSolver.Finite(c.r) && HLChainSolver.Finite(c.g) && HLChainSolver.Finite(c.b) && HLChainSolver.Finite(c.a);
        static bool Fail(string message, out string error) { error = message; return false; }
    }
}
