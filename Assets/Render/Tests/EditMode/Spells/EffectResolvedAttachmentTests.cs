using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class EffectResolvedAttachmentTests
    {
        [Test]
        public void Build_UsesTheSameMineralVariantForSurfaceAttachmentsAndRenderedMeshes()
        {
            GameObject host = new GameObject("Resolved spell fragment");
            var parts = new EffectParts();
            ShapeProfile shape = ShapeProfile.Shard(fracture: 0.65f);
            LookPart[] authored =
            {
                new LookPart { id = "first", shape = shape, role = PartRole.Body, pivot = ShapeAnchor.Bottom,
                    size = new Vector3(0.4f, 1.1f, 0.5f), euler = new Vector3(0f, 15f, 20f) },
                new LookPart { id = "second", shape = shape, role = PartRole.Body, pivot = ShapeAnchor.Bottom,
                    attachTo = "first", attachAt = ShapeAnchor.Top, size = new Vector3(0.3f, 0.6f, 0.3f),
                    euler = new Vector3(0f, 0f, -25f) }
            };
            var recipe = new EffectRecipe { entry = new ElementEntry { parts = authored },
                palette = RenderTestAssets.LoadPalette(), count = 2, colour = Color.white };
            try
            {
                parts.Build(recipe, host.transform, RenderTestAssets.LoadMeshes(),
                    RenderTestAssets.LoadLookMaterial(), LookSide.Stone);
                Vector3 firstTop = parts.shapes[0].TransformPoint(ProceduralShapeMeshes.Anchor(shape,
                    ShapeAnchor.Top, LookComposer.Variant(0, 0)));
                Vector3 secondBottom = parts.shapes[1].TransformPoint(ProceduralShapeMeshes.Anchor(shape,
                    ShapeAnchor.Bottom, LookComposer.Variant(0, 1)));
                Assert.That(Vector3.Distance(firstTop, secondBottom), Is.LessThan(0.00001f));
                for (int i = 0; i < 2; i++)
                {
                    Mesh expected = ProceduralShapeMeshes.Create(shape, LookComposer.Variant(0, i));
                    try
                    {
                        Mesh actual = parts.shapes[i].GetComponent<MeshFilter>().sharedMesh;
                        CollectionAssert.AreEqual(expected.vertices, actual.vertices);
                    }
                    finally { Object.DestroyImmediate(expected); }
                }
                Assert.AreEqual(Vector3.zero, authored[1].position, "Placement must not rewrite the authored fragment.");
            }
            finally
            {
                parts.Dispose();
                Object.DestroyImmediate(host);
            }
        }
    }
}
