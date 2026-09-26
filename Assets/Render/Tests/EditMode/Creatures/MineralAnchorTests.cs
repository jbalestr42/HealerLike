using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class MineralAnchorTests : MineralMeshFixture
{
    [Test]
    public void Anchor_SlantedCrown_SitsOnItsFaceInteriorRatherThanAnIsolatedHighestCorner()
    {
        ShapeProfile shape = ShapeProfile.Block(fracture: 0.85f);
        Mesh mesh = Build(shape, 17);
        Vector3 anchor = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, 17);
        AssertOnSurface(mesh, anchor);
        Assert.Less(anchor.y, mesh.bounds.max.y - 0.005f);
        foreach (Vector3 point in mesh.vertices)
        {
            Assert.Greater(Vector3.Distance(anchor, point), 0.01f);
        }
    }

    [TestCase(7)]
    [TestCase(15)]
    [TestCase(31)]
    public void Anchor_FullyClippedBlockCrown_UsesItsHighestSurfacePoint(int variant)
    {
        // Retain the profile which exposed the removed-cap bug independently of future art tuning.
        ShapeProfile shape = ShapeProfile.Block(0.28f, 0.12f, 0.08f, 0.82f);
        Mesh mesh = Build(shape, variant);
        Vector3 anchor = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, variant);
        Assert.That(
            anchor.y,
            Is.EqualTo(mesh.bounds.max.y).Within(0.000001f),
            "A removed crown must not move the accent onto a broad sloping side."
        );
        AssertOnSurface(mesh, anchor);
    }

    [TestCase(MassBand.Light)]
    [TestCase(MassBand.Sturdy)]
    [TestCase(MassBand.Heavy)]
    public void Compose_AuthoredFiveSpears_KeepEveryAccentAtItsBladeApex(MassBand mass)
    {
        LookVocabulary vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
        CreatureRecipe recipe = null;
        try
        {
            GrowthStoneVocabulary.Apply(vocabulary);
            LookPart source = System.Array.Find(
                vocabulary.heads[HeadKind.Spear].stone,
                part => part.id == "SpearBlade"
            );
            LookPart sourceTip = System.Array.Find(
                vocabulary.heads[HeadKind.Spear].stone,
                part => part.attachTo == "SpearBlade"
            );
            recipe = LookComposer.Compose(
                RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Spear, CountBand.Many, mass: mass),
                vocabulary
            );
            Assert.IsNotNull(recipe);
            int blades = 0;
            for (int i = 0; i < recipe.parts.Length; i++)
            {
                CreaturePart blade = recipe.parts[i];
                if (!blade.id.StartsWith("SpearBlade", System.StringComparison.Ordinal))
                {
                    continue;
                }

                blades++;
                Mesh mesh = Build(blade.shape, blade.variant);
                Vector3 apex = ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Top, blade.variant);
                Assert.That(apex.y, Is.EqualTo(mesh.bounds.max.y).Within(0.000001f));
                AssertOnSurface(mesh, apex);
                CreaturePart tip = recipe.parts[i + 1];
                Assert.IsTrue(tip.id.StartsWith(sourceTip.id, System.StringComparison.Ordinal));
                Vector3 bottom = ProceduralShapeMeshes.Anchor(tip.shape, ShapeAnchor.Bottom, tip.variant);
                Vector3 bladeContact =
                    blade.localPosition
                    + Quaternion.Euler(blade.localEuler) * Vector3.Scale(blade.dimensions, apex);
                Vector3 tipContact =
                    tip.localPosition + Quaternion.Euler(tip.localEuler) * Vector3.Scale(tip.dimensions, bottom);
                float scale = blade.dimensions.y / source.size.y;
                Assert.That(
                    Vector3.Distance(bladeContact, tipContact),
                    Is.EqualTo(sourceTip.position.magnitude * scale).Within(0.00001f),
                    "The authored small overlap must remain at the blade apex, not halfway down a side."
                );
            }

            Assert.AreEqual(5, blades);
        }
        finally
        {
            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(vocabulary);
        }
    }

    [TestCase(0.25f)]
    [TestCase(1f)]
    public void Resolve_FractureEdits_KeepActualSlabAndAccentSurfacesAttached(float fracture)
    {
        LookPart[] fragment =
        {
            new LookPart
            {
                id = "Slab",
                shape = ShapeProfile.Block(fracture: fracture),
                primitive = Primitive.Stone,
                pivot = ShapeAnchor.Bottom,
                size = new Vector3(0.7f, 1.4f, 0.4f),
                euler = new Vector3(0f, 23f, -8f),
            },
            new LookPart
            {
                id = "Accent",
                shape = ShapeProfile.Shard(fracture: fracture),
                primitive = Primitive.Stone,
                pivot = ShapeAnchor.Bottom,
                attachTo = "Slab",
                attachAt = ShapeAnchor.Top,
                size = new Vector3(0.15f, 0.25f, 0.14f),
            },
        };
        LookVocabulary vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
        CreatureRecipe recipe = null;
        try
        {
            vocabulary.heads[HeadKind.Fork] = new LookVocabulary.HeadEntry { stone = fragment };
            recipe = LookComposer.Compose(
                RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Fork),
                vocabulary
            );
            Assert.IsNotNull(recipe);
            CreaturePart slab = System.Array.Find(recipe.parts, part => part.id == "Slab");
            CreaturePart accent = System.Array.Find(recipe.parts, part => part.id == "Accent");
            Vector3[] contacts = new Vector3[2];
            CreaturePart[] parts = { slab, accent };
            for (int i = 0; i < parts.Length; i++)
            {
                CreaturePart part = parts[i];
                ShapeAnchor selected = i == 0 ? ShapeAnchor.Top : ShapeAnchor.Bottom;
                Vector3 local = ProceduralShapeMeshes.Anchor(part.shape, selected, part.variant);
                AssertOnSurface(Build(part.shape, part.variant), local);
                contacts[i] =
                    part.localPosition + Quaternion.Euler(part.localEuler) * Vector3.Scale(part.dimensions, local);
            }

            Assert.That(Vector3.Distance(contacts[0], contacts[1]), Is.LessThan(0.00001f));
        }
        finally
        {
            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(vocabulary);
        }
    }
}
}
