using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Equal-link FABRIK that allocates nothing and reads no scene, clock or random state
    public class ChainSolver
    {
        public bool Solve(Vector3[] joints, float[] lengths, Vector3 root, Vector3 target, Vector3 bendPole,
            out ChainResult result, int maxIterations = 32, float tolerance = 0.001f)
        {
            result = default;
            if (joints == null || lengths == null || lengths.Length < 2 || joints.Length != lengths.Length + 1)
            {
                Debug.LogError("[HLChainSolver] A chain requires N equal links and N+1 joints, N >= 2.");
                return false;
            }

            bool isRootFinite = float.IsFinite(root.x) && float.IsFinite(root.y) && float.IsFinite(root.z);
            bool isTargetFinite = float.IsFinite(target.x) && float.IsFinite(target.y) && float.IsFinite(target.z);
            bool isPoleFinite = float.IsFinite(bendPole.x) && float.IsFinite(bendPole.y) && float.IsFinite(bendPole.z);
            if (!isRootFinite || !isTargetFinite || !isPoleFinite || !float.IsFinite(tolerance) || tolerance <= 0f
                || maxIterations < 1)
            {
                Debug.LogError("[HLChainSolver] Invalid solver settings.");
                return false;
            }

            float total = 0f;
            for (int i = 0; i < lengths.Length; i++)
            {
                bool isEqual = Mathf.Abs(lengths[i] - lengths[0]) <= lengths[0] * 0.000001f;
                if (!float.IsFinite(lengths[i]) || lengths[i] <= 0f || !isEqual)
                {
                    Debug.LogError("[HLChainSolver] Lengths must be finite, positive and equal.");
                    return false;
                }

                total += lengths[i];
            }

            for (int i = 0; i < joints.Length; i++)
            {
                if (!float.IsFinite(joints[i].x) || !float.IsFinite(joints[i].y) || !float.IsFinite(joints[i].z))
                {
                    Debug.LogError("[HLChainSolver] Nonfinite joint.");
                    return false;
                }
            }

            if (!float.IsFinite(total))
            {
                Debug.LogError("[HLChainSolver] Chain length overflow.");
                return false;
            }

            int n = lengths.Length;
            Vector3 delta = target - root;
            float distance = delta.magnitude;
            if (!float.IsFinite(distance))
            {
                Debug.LogError("[HLChainSolver] Target distance overflow.");
                return false;
            }

            Vector3 aim = Direction(delta, Vector3.up);
            if (distance >= total)
            {
                joints[0] = root;
                for (int i = 0; i < n; i++)
                {
                    joints[i + 1] = joints[i] + aim * lengths[i];
                }

                Vector3 effective = root + aim * total;
                bool isReached = (joints[n] - target).magnitude <= tolerance;
                bool isClamped = distance > total + tolerance;
                result = new ChainResult(isReached, isClamped, effective, (joints[n] - effective).magnitude, 0);
                return true;
            }

            Vector3 side = bendPole - aim * Vector3.Dot(bendPole, aim);
            if (side.sqrMagnitude < 0.000000000001f)
            {
                Vector3 axis;
                if (Mathf.Abs(aim.x) < Mathf.Abs(aim.y))
                {
                    axis = Mathf.Abs(aim.x) < Mathf.Abs(aim.z) ? Vector3.right : Vector3.forward;
                }
                else
                {
                    axis = Mathf.Abs(aim.y) < Mathf.Abs(aim.z) ? Vector3.up : Vector3.forward;
                }

                side = axis - aim * Vector3.Dot(axis, aim);
            }

            side.Normalize();
            bool isCollinear = true;
            for (int i = 1; i <= n; i++)
            {
                if (Vector3.Cross(joints[i] - joints[0], aim).sqrMagnitude > 0.0000000001f)
                {
                    isCollinear = false;
                    break;
                }
            }

            if (isCollinear)
            {
                // A straight chain cannot find a bend through projection alone. Seed a regular
                // arc in the pole plane, bisection picks its chord without random jitter.
                double low = 0.0;
                double high = 2.0 * System.Math.PI / n;
                for (int k = 0; k < 48; k++)
                {
                    double angle = (low + high) * 0.5;
                    double chord = lengths[0] * System.Math.Sin(n * angle * 0.5) / System.Math.Sin(angle * 0.5);
                    if (chord > distance)
                    {
                        low = angle;
                    }
                    else
                    {
                        high = angle;
                    }
                }

                double step = (low + high) * 0.5;
                joints[0] = root;
                for (int i = 0; i < n; i++)
                {
                    double angle = (i - (n - 1) * 0.5) * step;
                    Vector3 direction = aim * (float)System.Math.Cos(angle) + side * (float)System.Math.Sin(angle);
                    joints[i + 1] = joints[i] + lengths[i] * direction;
                }
            }

            int iterations = 0;
            float error;
            do
            {
                joints[n] = target;
                for (int i = n - 1; i >= 0; i--)
                {
                    joints[i] = joints[i + 1] + Direction(joints[i] - joints[i + 1], -aim) * lengths[i];
                }

                joints[0] = root;
                for (int i = 0; i < n; i++)
                {
                    joints[i + 1] = joints[i] + Direction(joints[i + 1] - joints[i], aim) * lengths[i];
                }

                iterations++;
                error = (joints[n] - target).magnitude;
            }
            while (error > tolerance && iterations < maxIterations);

            result = new ChainResult(error <= tolerance, false, target, error, iterations);
            return true;
        }

        static Vector3 Direction(Vector3 value, Vector3 fallback)
        {
            return value.sqrMagnitude > 0.0000000000000001f ? value.normalized : fallback;
        }
    }
}
