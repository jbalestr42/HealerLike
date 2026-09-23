using System;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLChainSolverTests
    {
        public static Vector3[] Rest(int n = 24)
        {
            Vector3[] joints = new Vector3[n + 1];
            for (int i = 0; i < n; i++)
            {
                joints[i + 1] = joints[i] + new Vector3(Mathf.Cos(i * 0.4f), Mathf.Sin(i * 0.4f), 0f) * 0.2f;
            }

            return joints;
        }

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
                Assert.IsTrue(HLChainSolver.Finite(joints[i]));
                Assert.That(Vector3.Distance(joints[i], joints[i - 1]), Is.EqualTo(0.2f).Within(0.00001));
            }
        }

        [TestCase(2f, 1f, 0.5f)]
        [TestCase(-1f, 0.4f, 2f)]
        [TestCase(0f, 0f, 0f)]
        public void ReachableTargetPreservesLengths(float x, float y, float z)
        {
            Vector3[] joints = Rest();
            Vector3 target = new Vector3(x, y, z);

            HLChainResult result = new HLChainSolver().Solve(joints, Lengths(), Vector3.zero, target, Vector3.up, 128);

            Assert.IsTrue(result.reached, result.error.ToString());
            Assert.LessOrEqual(Vector3.Distance(joints[24], target), 0.001f);
            Check(joints, Vector3.zero);
        }

        [Test]
        public void BeyondReachClampsAndFullReachDoesNotFalseClamp()
        {
            Vector3[] joints = Rest();
            HLChainSolver solver = new HLChainSolver();

            HLChainResult result = solver.Solve(joints, Lengths(), Vector3.zero, new Vector3(20f, 0f, 0f), Vector3.up);

            Assert.IsTrue(result.clamped);
            Assert.IsFalse(result.reached);
            Assert.Less(result.error, 0.00001);
            Assert.That(joints[24].x, Is.EqualTo(4.8f).Within(0.00001));
            Check(joints, Vector3.zero);
            result = solver.Solve(joints, Lengths(), Vector3.zero, new Vector3(4.8f, 0f, 0f), Vector3.up);
            Assert.IsTrue(result.reached);
            Assert.IsFalse(result.clamped);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DegenerateChainsReachDeterministically(int mode)
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
            HLChainSolver solver = new HLChainSolver();

            HLChainResult result = solver.Solve(joints, Lengths(), Vector3.zero, target, Vector3.up);
            HLChainResult again = solver.Solve(copy, Lengths(), Vector3.zero, target, Vector3.up);

            Assert.IsTrue(result.reached);
            Assert.AreEqual(result.error, again.error);
            CollectionAssert.AreEqual(joints, copy);
            Check(joints, Vector3.zero);
        }

        [Test]
        public void SeededSweepAndTranslatedRoot()
        {
            System.Random random = new System.Random(1943);
            Vector3 root = new Vector3(3f, 2f, -4f);
            for (int k = 0; k < 60; k++)
            {
                Vector3[] joints = Rest();
                for (int i = 0; i < joints.Length; i++)
                {
                    joints[i] += root;
                }

                float x = (float)random.NextDouble() * 5f - 2.5f;
                float y = (float)random.NextDouble() * 2f;
                float z = (float)random.NextDouble() * 5f - 2.5f;
                Vector3 target = root + new Vector3(x, y, z);

                HLChainResult result = new HLChainSolver().Solve(joints, Lengths(), root, target, Vector3.up, 256);

                Assert.IsTrue(result.reached, $"{k}: {result.error}");
                Check(joints, root);
            }
        }

        [Test]
        public void InvalidInputsAreRejected()
        {
            HLChainSolver solver = new HLChainSolver();
            Vector3 root = Vector3.zero;
            Vector3 target = Vector3.one;
            float[] lengths = Lengths();
            lengths[3] = 0.3f;
            Assert.Throws<ArgumentException>(() => solver.Solve(Rest(), lengths, root, target, Vector3.up));
            lengths[3] = float.NaN;
            Assert.Throws<ArgumentException>(() => solver.Solve(Rest(), lengths, root, target, Vector3.up));
            Assert.Throws<ArgumentException>(() => solver.Solve(Rest(), Lengths(), root, target, Vector3.up, 0));
            Vector3[] joints = Rest();
            joints[2].x = float.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => solver.Solve(joints, Lengths(), root, target, Vector3.up));
        }

        [Test]
        public void IterationLimitReportsActualResidual()
        {
            Vector3[] joints = Rest();
            Vector3 target = new Vector3(4f, 1f, 0f);
            HLChainSolver solver = new HLChainSolver();

            HLChainResult result = solver.Solve(joints, Lengths(), Vector3.zero, target, Vector3.up, 1, 0.0000001f);

            Assert.AreEqual(1, result.iterations);
            Assert.AreEqual(result.error <= 0.0000001f, result.reached);
            Check(joints, Vector3.zero);
        }

        [Test]
        public void RestPoseAlreadyReachedRemainsWithinTolerance()
        {
            Vector3[] joints = Rest();

            HLChainResult result = new HLChainSolver().Solve(joints, Lengths(), joints[0], joints[24], Vector3.up);

            Assert.IsTrue(result.reached);
            Check(joints, Vector3.zero);
        }
    }
}
