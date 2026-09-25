using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class ShapeAnchorTests
    {
        readonly List<Mesh> _meshes = new List<Mesh>();

        [TearDown]
        public void TearDown()
        {
            foreach (Mesh mesh in _meshes)
            {
                Object.DestroyImmediate(mesh);
            }
            _meshes.Clear();
        }

        [Test]
        public void Anchor_CurvedGrowthAndSeededMinerals_MatchesActualMeshEndFaceCentres()
        {
            ShapeProfile block = ShapeProfile.Block(0.3f, 0.7f, 0.15f);
            block.bend = -0.8f;
            foreach (ShapeProfile shape in new[] { ShapeProfile.Leaf(0.85f, 1.7f),
                ShapeProfile.Segment(0.65f, 0.4f, -0.7f), block, ShapeProfile.Shard(),
                ShapeProfile.Bulb(2.2f, -0.5f), ShapeProfile.Ring(), ShapeProfile.Ring(0.3f, true) })
            {
                foreach (int variant in new[] { 0, 17, 213 })
                {
                    Mesh mesh = ProceduralShapeMeshes.Create(shape, variant);
                    _meshes.Add(mesh);
                    foreach (ShapeAnchor anchor in new[] { ShapeAnchor.Bottom, ShapeAnchor.Top })
                    {
                        Vector3 measured = EndCentre(mesh, anchor == ShapeAnchor.Top);
                        Vector3 queried = ProceduralShapeMeshes.Anchor(shape, anchor, variant);
                        Assert.That(Vector3.Distance(measured, queried), Is.LessThan(0.00001f), shape.kind.ToString());
                        Assert.AreEqual(queried, ProceduralShapeMeshes.Anchor(shape, anchor, variant));
                        Assert.IsTrue(RenderMath.IsFinite(queried));
                    }
                }
            }
        }

        static Vector3 EndCentre(Mesh mesh, bool top)
        {
            float height = top ? mesh.bounds.max.y : mesh.bounds.min.y;
            HashSet<Vector3> points = new HashSet<Vector3>();
            foreach (Vector3 point in mesh.vertices)
            {
                if (Mathf.Abs(point.y - height) < 0.000001f)
                {
                    points.Add(point);
                }
            }
            Vector3 sum = Vector3.zero;
            foreach (Vector3 point in points)
            {
                sum += point;
            }
            return sum / points.Count;
        }

        [Test]
        public void Anchor_BendEdit_ChangesTheContactOffsetWithoutChangingTheUnitBounds()
        {
            Vector3 straight = ProceduralShapeMeshes.Anchor(ShapeProfile.Leaf(0f), ShapeAnchor.Bottom);
            Vector3 bent = ProceduralShapeMeshes.Anchor(ShapeProfile.Leaf(0.9f), ShapeAnchor.Bottom);
            Assert.Greater(Mathf.Abs(straight.x - bent.x), 0.01f);
            Assert.AreEqual(-0.5f, bent.y, 0.00001f);
            Assert.AreEqual(Vector3.zero, ProceduralShapeMeshes.Anchor(ShapeProfile.Leaf(0.9f), ShapeAnchor.Center));
        }

        [Test]
        public void Anchor_ParameterExtremes_StayFiniteAndOnTheMeasuredEndPlane()
        {
            ShapeProfile shape = ShapeProfile.Leaf(1f, 3f);
            shape.taper = 0.95f;
            shape.radialSegments = 32;
            shape.lengthSegments = 24;
            Assert.That(ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top).y, Is.EqualTo(0.5f).Within(0.00001f));
            shape.bend = -1f;
            shape.fullness = 0.05f;
            shape.taper = -0.8f;
            Vector3 bottom = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Bottom);
            Assert.IsTrue(RenderMath.IsFinite(bottom));
            Assert.That(bottom.y, Is.EqualTo(-0.5f).Within(0.00001f));
        }

        [Test]
        public void Anchor_LegacyUsesNormalizedAxisAndInvalidInputsAreRejected()
        {
            Assert.AreEqual(Vector3.zero, ProceduralShapeMeshes.Anchor(default, ShapeAnchor.Center));
            Assert.AreEqual(Vector3.down * 0.5f, ProceduralShapeMeshes.Anchor(default, ShapeAnchor.Bottom));
            Assert.AreEqual(Vector3.up * 0.5f, ProceduralShapeMeshes.Anchor(default, ShapeAnchor.Top));
            Assert.Throws<ArgumentOutOfRangeException>(() => ProceduralShapeMeshes.Anchor(default, (ShapeAnchor)99));
            ShapeProfile invalid = ShapeProfile.Leaf();
            invalid.bend = float.NaN;
            Assert.Throws<ArgumentException>(() => ProceduralShapeMeshes.Anchor(invalid, ShapeAnchor.Bottom));
            Assert.Throws<ArgumentException>(() => ProceduralShapeMeshes.Anchor(invalid, ShapeAnchor.Center));
        }
    }
}
