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
