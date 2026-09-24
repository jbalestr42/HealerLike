using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassBuildKeyTests
{
    // Four by two cells of 1.5 centred on (2, -1)
    static readonly Rect area = new Rect(-1f, -2.5f, 6f, 3f);

    static GrassBuildKey CreateKey(float cellSize = 1.5f, float surfaceY = 0.5f, uint seed = 3, int budget = 100)
    {
        return new GrassBuildKey(area, cellSize, surfaceY, seed, budget);
    }

    [Test]
    public void Constructor_Area_CopiesFootprint()
    {
        GrassBuildKey key = CreateKey();

        Assert.AreEqual(4, key.width);
        Assert.AreEqual(2, key.height);
        Assert.AreEqual(1.5f, key.cellSize);
        Assert.AreEqual(new Vector3(2f, 0f, -1f), key.origin);
        Assert.AreEqual(0.5f, key.surfaceY);
        Assert.AreEqual(3u, key.seed);
        Assert.AreEqual(100, key.budget);
    }

    [Test]
    public void Matches_SameInputs_ReturnsTrue()
    {
        GrassBuildKey key = CreateKey();

        Assert.IsTrue(key.Matches(CreateKey()));
    }

    [Test]
    public void Matches_AnyInputChanged_ReturnsFalse()
    {
        GrassBuildKey key = CreateKey();

        Assert.IsFalse(key.Matches(CreateKey(surfaceY: 0.6f)));
        Assert.IsFalse(key.Matches(CreateKey(seed: 4)));
        Assert.IsFalse(key.Matches(CreateKey(budget: 101)));
        Assert.IsFalse(key.Matches(new GrassBuildKey(new Rect(-3f, -1.5f, 6f, 3f), 1.5f, 0.5f, 3, 100)));
    }

    [Test]
    public void GenerateLayout_FiniteArea_ReturnsTheGridUnderTheBudget()
    {
        GrassBuildKey key = CreateKey();

        TuftSeed[] layout = key.GenerateLayout();

        Assert.AreEqual(98, layout.Length); // 14 by 7, the widest grid of at most 100
    }

    [Test]
    public void GenerateLayout_NonFiniteCellSize_ReturnsNull()
    {
        GrassBuildKey key = CreateKey(cellSize: float.NaN);

        Assert.IsNull(key.GenerateLayout());
    }

    [Test]
    public void FieldRect_Area_ReturnsWorldCorners()
    {
        GrassBuildKey key = CreateKey();

        Vector4 rect = key.FieldRect();

        Assert.AreEqual(new Vector4(-1f, -2.5f, 5f, 0.5f), rect); // centre (2, -1), half size (3, 1.5)
    }

    [Test]
    public void CalculateBounds_Area_CoversFieldAndTuftEnvelope()
    {
        GrassBuildKey key = CreateKey();

        Bounds bounds = key.CalculateBounds();

        Assert.AreEqual(2f, bounds.center.x);
        Assert.AreEqual(-1f, bounds.center.z);
        float envelope = GrassBounds.Envelope(1.5f);
        Assert.AreEqual(6f + 2f * envelope, bounds.size.x, 0.0001f); // 4 cells of 1.5
        Assert.AreEqual(3f + 2f * envelope, bounds.size.z, 0.0001f); // 2 cells of 1.5
    }
}

}
