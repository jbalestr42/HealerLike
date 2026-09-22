using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureValidatorTests
    {
        internal static HLCreatureRecipe Recipe()
        {
            var r = ScriptableObject.CreateInstance<HLCreatureRecipe>();
            r.parts = new[] { new HLPart { id = "HLBody", parent = -1, dimensions = Vector3.one, colour = Color.green } };
            r.sourceLocal = new[] { Vector3.up };
            r.arms = new[] { new HLArmDefinition { bodyPart = 0, segmentCount = 24, segmentLength = .2f, radius = .018f, restJoints = HLChainSolverTests.Rest(), bendPole = Vector3.up, colour = Color.green } };
            return r;
        }
        [TestCase(6, true)] [TestCase(7, true)] [TestCase(8, true)] [TestCase(9, false)]
        public void RootCrownSupportsEightLegsWithinCell(int count, bool valid)
        {
            var r = Recipe();
            try { r.roots.count = count; Assert.AreEqual(valid, HLCreatureValidator.TryValidate(r, out _)); }
            finally { Object.DestroyImmediate(r); }
        }
        [TestCase(40, true)] [TestCase(41, false)]
        public void JointDetailStillHasBoundedPartBudget(int count, bool valid)
        {
            var r = Recipe();
            try
            {
                var body = r.parts[0]; r.parts = new HLPart[count];
                for (int i = 0; i < count; i++) { r.parts[i] = body; r.parts[i].id = "HLPart" + i; r.parts[i].parent = i == 0 ? -1 : 0; }
                Assert.AreEqual(valid, HLCreatureValidator.TryValidate(r, out _));
            }
            finally { Object.DestroyImmediate(r); }
        }
        [Test] public void ValidTreeAndRestAreAccepted()
        { var r = Recipe(); try { Assert.IsTrue(HLCreatureValidator.TryValidate(r, out var error), error); } finally { Object.DestroyImmediate(r); } }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void InvalidTreeSocketAndRestAreRejected(int mode)
        {
            var r = Recipe();
            try
            {
                if (mode == 0) r.parts[0].parent = 0;
                if (mode == 1) r.parts = new[] { r.parts[0], r.parts[0] };
                if (mode == 2) r.sourceLocal = new Vector3[0];
                if (mode == 3) r.arms[0].restJoints[2] = Vector3.one * 100;
                if (mode == 4) r.roots.footRadius = 1;
                Assert.IsFalse(HLCreatureValidator.TryValidate(r, out _));
            }
            finally { Object.DestroyImmediate(r); }
        }
    }
}
