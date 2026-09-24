using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Deliveries
{

public class ChainSolverTests
{
    static float[] Lengths(int n = 24)
    {
        float[] lengths = new float[n];
        for (int i = 0; i < n; i++)
        {
            lengths[i] = 0.2f;
        }

        return lengths;
    }

    static void Check(Vector3[] joints, Vector3 root)
    {
        Assert.That(Vector3.Distance(joints[0], root), Is.LessThan(0.000001));
        for (int i = 1; i < joints.Length; i++)
        {
            Assert.IsTrue(float.IsFinite(joints[i].x) && float.IsFinite(joints[i].y)
                && float.IsFinite(joints[i].z));
            Assert.That(Vector3.Distance(joints[i], joints[i - 1]), Is.EqualTo(0.2f).Within(0.00001));
        }
    }

    [TestCase(2f, 1f, 0.5f)]
    [TestCase(-1f, 0.4f, 2f)]
    [TestCase(0f, 0f, 0f)]
    public void Solve_ReachableTarget_ReachesItAndPreservesLengths(float x, float y, float z)
    {
        Vector3[] joints = RenderTestAssets.CreateRestPose();
        Vector3 target = new Vector3(x, y, z);

        bool isSolved = new ChainSolver().Solve(joints, Lengths(), Vector3.zero, target, Vector3.up,
            out ChainResult result, 128);

        Assert.IsTrue(isSolved);
        Assert.IsTrue(result.reached, result.error.ToString());
        Assert.LessOrEqual(Vector3.Distance(joints[24], target), 0.001f);
        Check(joints, Vector3.zero);
    }

    [Test]
    public void Solve_TargetBeyondReach_ClampsButFullReachDoesNot()
    {
        Vector3[] joints = RenderTestAssets.CreateRestPose();
        ChainSolver solver = new ChainSolver();

        bool isSolved = solver.Solve(joints, Lengths(), Vector3.zero, new Vector3(20f, 0f, 0f), Vector3.up,
            out ChainResult result);

        Assert.IsTrue(isSolved);
        Assert.IsTrue(result.clamped);
        Assert.IsFalse(result.reached);
        Assert.Less(result.error, 0.00001);
        Assert.That(joints[24].x, Is.EqualTo(4.8f).Within(0.00001));
        Check(joints, Vector3.zero);
        Assert.IsTrue(solver.Solve(joints, Lengths(), Vector3.zero, new Vector3(4.8f, 0f, 0f), Vector3.up,
            out result));
        Assert.IsTrue(result.reached);
        Assert.IsFalse(result.clamped);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Solve_DegenerateChain_ReachesDeterministically(int mode)
    {
        Vector3[] joints = new Vector3[25];
        for (int i = 0; i < joints.Length; i++)
        {
            if (mode != 0)
            {
                joints[i] = Vector3.up * 0.2f * i;
            }
        }

        Vector3[] copy = (Vector3[])joints.Clone();
        Vector3 target = mode == 2 ? Vector3.zero : Vector3.up;
        ChainSolver solver = new ChainSolver();

        bool isSolved = solver.Solve(joints, Lengths(), Vector3.zero, target, Vector3.up, out ChainResult result);
        bool isSolvedAgain = solver.Solve(copy, Lengths(), Vector3.zero, target, Vector3.up,
            out ChainResult again);

        Assert.IsTrue(isSolved);
        Assert.IsTrue(isSolvedAgain);
        Assert.IsTrue(result.reached);
        Assert.AreEqual(result.error, again.error);
        CollectionAssert.AreEqual(joints, copy);
        Check(joints, Vector3.zero);
    }

    [Test]
    public void Solve_SeededTargetsAroundTranslatedRoot_ReachEveryTarget()
    {
        System.Random random = new System.Random(1943);
        Vector3 root = new Vector3(3f, 2f, -4f);
        for (int k = 0; k < 60; k++)
        {
            Vector3[] joints = RenderTestAssets.CreateRestPose();
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] += root;
            }

            float x = (float)random.NextDouble() * 5f - 2.5f;
            float y = (float)random.NextDouble() * 2f;
            float z = (float)random.NextDouble() * 5f - 2.5f;
            Vector3 target = root + new Vector3(x, y, z);

            bool isSolved = new ChainSolver().Solve(joints, Lengths(), root, target, Vector3.up,
                out ChainResult result, 256);

            Assert.IsTrue(isSolved);
            Assert.IsTrue(result.reached, $"{k}: {result.error}");
            Check(joints, root);
        }
    }

    [Test]
    public void Solve_InvalidInputs_LogsErrorAndReturnsFalse()
    {
        ChainSolver solver = new ChainSolver();
        Vector3 root = Vector3.zero;
        Vector3 target = Vector3.one;
        float[] lengths = Lengths();
        lengths[3] = 0.3f;
        Vector3[] joints = RenderTestAssets.CreateRestPose();
        joints[2].x = float.PositiveInfinity;
        LogAssert.Expect(LogType.Error, "[ChainSolver] Lengths must be finite, positive and equal.");
        LogAssert.Expect(LogType.Error, "[ChainSolver] Lengths must be finite, positive and equal.");
        LogAssert.Expect(LogType.Error, "[ChainSolver] Invalid solver settings.");
        LogAssert.Expect(LogType.Error, "[ChainSolver] Nonfinite joint.");

        bool isUnequalSolved = solver.Solve(RenderTestAssets.CreateRestPose(), lengths, root, target, Vector3.up,
            out ChainResult result);
        lengths[3] = float.NaN;
        bool isNaNSolved =
            solver.Solve(RenderTestAssets.CreateRestPose(), lengths, root, target, Vector3.up, out result);
        bool isZeroIterationSolved =
            solver.Solve(RenderTestAssets.CreateRestPose(), Lengths(), root, target, Vector3.up, out result, 0);
        bool isInfiniteJointSolved = solver.Solve(joints, Lengths(), root, target, Vector3.up, out result);

        Assert.IsFalse(isUnequalSolved);
        Assert.IsFalse(isNaNSolved);
        Assert.IsFalse(isZeroIterationSolved);
        Assert.IsFalse(isInfiniteJointSolved);
    }

    [Test]
    public void Solve_IterationLimitHit_ReportsActualResidual()
    {
        Vector3[] joints = RenderTestAssets.CreateRestPose();
        Vector3 target = new Vector3(4f, 1f, 0f);
        ChainSolver solver = new ChainSolver();

        bool isSolved = solver.Solve(joints, Lengths(), Vector3.zero, target, Vector3.up, out ChainResult result,
            1, 0.0000001f);

        Assert.IsTrue(isSolved);
        Assert.AreEqual(1, result.iterations);
        Assert.AreEqual(result.error <= 0.0000001f, result.reached);
        Check(joints, Vector3.zero);
    }

    [Test]
    public void Solve_TargetAtRestTip_StaysReached()
    {
        Vector3[] joints = RenderTestAssets.CreateRestPose();

        bool isSolved = new ChainSolver().Solve(joints, Lengths(), joints[0], joints[24], Vector3.up,
            out ChainResult result);

        Assert.IsTrue(isSolved);
        Assert.IsTrue(result.reached);
        Check(joints, Vector3.zero);
    }
}

}
