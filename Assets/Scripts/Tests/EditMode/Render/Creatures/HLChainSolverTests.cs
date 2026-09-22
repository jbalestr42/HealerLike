using System;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLChainSolverTests
    {
        static float[] Lengths(int n = 24) { var a = new float[n]; for (int i = 0; i < n; i++) a[i] = .2f; return a; }
        internal static Vector3[] Rest(int n = 24)
        {
            var a = new Vector3[n + 1];
            for (int i = 0; i < n; i++) a[i + 1] = a[i] + new Vector3(Mathf.Cos(i * .4f), Mathf.Sin(i * .4f), 0) * .2f;
            return a;
        }
        static void Check(Vector3[] a, Vector3 root)
        {
            Assert.That(Vector3.Distance(a[0], root), Is.LessThan(1e-6));
            for (int i = 1; i < a.Length; i++) { Assert.IsTrue(HLChainSolver.Finite(a[i])); Assert.That(Vector3.Distance(a[i], a[i - 1]), Is.EqualTo(.2f).Within(1e-5)); }
        }
        [TestCase(2, 1, .5f)] [TestCase(-1, .4f, 2)] [TestCase(0, 0, 0)]
        public void ReachableTargetPreservesLengths(float x, float y, float z)
        {
            var a = Rest(); var target = new Vector3(x, y, z);
            var result = new HLChainSolver().Solve(a, Lengths(), Vector3.zero, target, Vector3.up, 128);
            Assert.IsTrue(result.reached, result.error.ToString()); Assert.LessOrEqual(Vector3.Distance(a[24], target), .001f); Check(a, Vector3.zero);
        }
        [Test] public void BeyondReachClampsAndFullReachDoesNotFalseClamp()
        {
            var a = Rest(); var solver = new HLChainSolver();
            var r = solver.Solve(a, Lengths(), Vector3.zero, new Vector3(20, 0, 0), Vector3.up);
            Assert.IsTrue(r.clamped); Assert.IsFalse(r.reached); Assert.Less(r.error, 1e-5); Assert.That(a[24].x, Is.EqualTo(4.8f).Within(1e-5)); Check(a, Vector3.zero);
            r = solver.Solve(a, Lengths(), Vector3.zero, new Vector3(4.8f, 0, 0), Vector3.up);
            Assert.IsTrue(r.reached); Assert.IsFalse(r.clamped);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void DegenerateChainsReachDeterministically(int mode)
        {
            var a = new Vector3[25]; for (int i = 0; i < a.Length; i++) if (mode != 0) a[i] = Vector3.up * .2f * i;
            var b = (Vector3[])a.Clone(); Vector3 target = mode == 2 ? Vector3.zero : Vector3.up;
            var solver = new HLChainSolver(); var r = solver.Solve(a, Lengths(), Vector3.zero, target, Vector3.up);
            var s = solver.Solve(b, Lengths(), Vector3.zero, target, Vector3.up);
            Assert.IsTrue(r.reached); Assert.AreEqual(r.error, s.error); CollectionAssert.AreEqual(a, b); Check(a, Vector3.zero);
        }
        [Test] public void SeededSweepAndTranslatedRoot()
        {
            var random = new System.Random(1943); var root = new Vector3(3, 2, -4);
            for (int k = 0; k < 60; k++)
            {
                var a = Rest(); for (int i = 0; i < a.Length; i++) a[i] += root;
                var target = root + new Vector3((float)random.NextDouble() * 5 - 2.5f, (float)random.NextDouble() * 2, (float)random.NextDouble() * 5 - 2.5f);
                var r = new HLChainSolver().Solve(a, Lengths(), root, target, Vector3.up, 256);
                Assert.IsTrue(r.reached, $"{k}: {r.error}"); Check(a, root);
            }
        }
        [Test] public void InvalidInputsAreRejected()
        {
            var l = Lengths(); l[3] = .3f;
            Assert.Throws<ArgumentException>(() => new HLChainSolver().Solve(Rest(), l, Vector3.zero, Vector3.one, Vector3.up));
            l[3] = float.NaN;
            Assert.Throws<ArgumentException>(() => new HLChainSolver().Solve(Rest(), l, Vector3.zero, Vector3.one, Vector3.up));
            Assert.Throws<ArgumentException>(() => new HLChainSolver().Solve(Rest(), Lengths(), Vector3.zero, Vector3.one, Vector3.up, 0));
            var a = Rest(); a[2].x = float.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => new HLChainSolver().Solve(a, Lengths(), Vector3.zero, Vector3.one, Vector3.up));
        }
        [Test] public void IterationLimitReportsActualResidual()
        {
            var a = Rest(); var r = new HLChainSolver().Solve(a, Lengths(), Vector3.zero, new Vector3(4, 1, 0), Vector3.up, 1, 1e-7f);
            Assert.AreEqual(1, r.iterations); Assert.AreEqual(r.error <= 1e-7f, r.reached); Check(a, Vector3.zero);
        }
        [Test] public void RestPoseAlreadyReachedRemainsWithinTolerance()
        {
            var a = Rest(); var r = new HLChainSolver().Solve(a, Lengths(), a[0], a[24], Vector3.up);
            Assert.IsTrue(r.reached); Check(a, Vector3.zero);
        }
    }
}
