using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public abstract class GrowthStoneFixture
{
    protected LookVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
        GrowthStoneVocabulary.Apply(_vocabulary);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_vocabulary);
    }

    protected static Vector2 Project(LookPart part, Vector3 direction)
    {
        float centre = Vector3.Dot(part.position, direction);
        float extent = LookMeasure.Extent(
            part.size * 0.5f,
            Quaternion.Inverse(Quaternion.Euler(part.euler)) * direction,
            part.shape
        );
        return new Vector2(centre - extent, centre + extent);
    }

    protected static Vector3 Pole(LookPart part, ShapeAnchor anchor)
    {
        return part.position
            + Quaternion.Euler(part.euler)
                * Vector3.Scale(ProceduralShapeMeshes.Anchor(part.shape, anchor), part.size);
    }

    protected static Bounds Box(LookPart part)
    {
        Quaternion rotation = Quaternion.Euler(part.euler);
        Bounds bounds = new Bounds(part.position, Vector3.zero);
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = Vector3.Scale(part.size * 0.5f, new Vector3(x, y, z));
                    bounds.Encapsulate(part.position + rotation * corner);
                }
            }
        }

        return bounds;
    }
}
}
