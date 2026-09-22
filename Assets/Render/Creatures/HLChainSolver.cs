using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLChainResult
    {
        public readonly bool reached, clamped;
        public readonly Vector3 effectiveTarget;
        public readonly float error;
        public readonly int iterations;
        public HLChainResult(bool reached, bool clamped, Vector3 target, float error, int iterations)
        { this.reached = reached; this.clamped = clamped; effectiveTarget = target; this.error = error; this.iterations = iterations; }
    }

    /// <summary>Allocation-free equal-link FABRIK. No scene, object, clock or random dependencies.</summary>
    public sealed class HLChainSolver
    {
        public HLChainResult Solve(Vector3[] joints, float[] lengths, Vector3 root, Vector3 target,
            Vector3 bendPole, int maxIterations = 32, float tolerance = .001f)
        {
            if (joints == null || lengths == null || lengths.Length < 2 || joints.Length != lengths.Length + 1)
                throw new ArgumentException("A chain requires N equal links and N+1 joints, N >= 2.");
            if (!Finite(root) || !Finite(target) || !Finite(bendPole) || !Finite(tolerance) || tolerance <= 0 || maxIterations < 1)
                throw new ArgumentException("Invalid solver settings.");
            float total = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (!Finite(lengths[i]) || lengths[i] <= 0 || Mathf.Abs(lengths[i] - lengths[0]) > lengths[0] * 1e-6f)
                    throw new ArgumentException("Lengths must be finite, positive and equal.");
                total += lengths[i];
            }
            for (int i = 0; i < joints.Length; i++)
                if (!Finite(joints[i])) throw new ArgumentException("Nonfinite joint.");
            if (!Finite(total)) throw new ArgumentException("Chain length overflow.");
            int n = lengths.Length;
            Vector3 delta = target - root;
            float distance = delta.magnitude;
            if (!Finite(distance)) throw new ArgumentException("Target distance overflow.");
            Vector3 aim = Direction(delta, Vector3.up);
            if (distance >= total)
            {
                joints[0] = root;
                for (int i = 0; i < n; i++) joints[i + 1] = joints[i] + aim * lengths[i];
                Vector3 effective = root + aim * total;
                return new HLChainResult((joints[n] - target).magnitude <= tolerance, distance > total + tolerance,
                    effective, (joints[n] - effective).magnitude, 0);
            }
            Vector3 side = bendPole - aim * Vector3.Dot(bendPole, aim);
            if (side.sqrMagnitude < 1e-12f)
            {
                Vector3 axis = Mathf.Abs(aim.x) < Mathf.Abs(aim.y)
                    ? (Mathf.Abs(aim.x) < Mathf.Abs(aim.z) ? Vector3.right : Vector3.forward)
                    : (Mathf.Abs(aim.y) < Mathf.Abs(aim.z) ? Vector3.up : Vector3.forward);
                side = axis - aim * Vector3.Dot(axis, aim);
            }
            side.Normalize();
            bool collinear = true;
            for (int i = 1; i <= n; i++)
                if (Vector3.Cross(joints[i] - joints[0], aim).sqrMagnitude > 1e-10f) { collinear = false; break; }
            if (collinear)
            {
                // A straight chain cannot discover a bend through projection alone. Seed a regular
                // arc in the pole plane; bisection chooses its chord without stochastic jitter.
                double low = 0, high = 2 * System.Math.PI / n;
                for (int k = 0; k < 48; k++)
                {
                    double angle = (low + high) * .5;
                    double chord = lengths[0] * System.Math.Sin(n * angle * .5) / System.Math.Sin(angle * .5);
                    if (chord > distance) low = angle; else high = angle;
                }
                double step = (low + high) * .5;
                joints[0] = root;
                for (int i = 0; i < n; i++)
                {
                    double angle = (i - (n - 1) * .5) * step;
                    joints[i + 1] = joints[i] + lengths[i] * (aim * (float)System.Math.Cos(angle) + side * (float)System.Math.Sin(angle));
                }
            }
            int iterations = 0;
            float error;
            do
            {
                joints[n] = target;
                for (int i = n - 1; i >= 0; i--)
                    joints[i] = joints[i + 1] + Direction(joints[i] - joints[i + 1], -aim) * lengths[i];
                joints[0] = root;
                for (int i = 0; i < n; i++)
                    joints[i + 1] = joints[i] + Direction(joints[i + 1] - joints[i], aim) * lengths[i];
                iterations++;
                error = (joints[n] - target).magnitude;
            } while (error > tolerance && iterations < maxIterations);
            return new HLChainResult(error <= tolerance, false, target, error, iterations);
        }

        static Vector3 Direction(Vector3 value, Vector3 fallback) => value.sqrMagnitude > 1e-16f ? value.normalized : fallback;
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}
