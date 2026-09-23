using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class HLGrassBuildKeyTests
{
    GameObject _gridObject;
    GridManager _grid;

    [SetUp]
    public void SetUp()
    {
        _gridObject = new GameObject("HLGrassBuildKeyGrid");
        _gridObject.transform.position = new Vector3(2f, 0f, -1f);
        _grid = _gridObject.AddComponent<GridManager>();
        _grid.width = 4;
        _grid.height = 2;
        _grid.size = 1.5f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gridObject);
    }

    [Test]
    public void Constructor_Grid_CopiesFootprint()
    {
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

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
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        Assert.IsTrue(key.Matches(new HLGrassBuildKey(_grid, 0.5f, 3, 100)));
    }

    [Test]
    public void Matches_AnyInputChanged_ReturnsFalse()
    {
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        Assert.IsFalse(key.Matches(new HLGrassBuildKey(_grid, 0.6f, 3, 100)));
        Assert.IsFalse(key.Matches(new HLGrassBuildKey(_grid, 0.5f, 4, 100)));
        Assert.IsFalse(key.Matches(new HLGrassBuildKey(_grid, 0.5f, 3, 101)));
        _gridObject.transform.position = Vector3.zero;
        Assert.IsFalse(key.Matches(new HLGrassBuildKey(_grid, 0.5f, 3, 100)));
    }

    [Test]
    public void GenerateLayout_FiniteGrid_ReturnsBudgetBlades()
    {
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        HLBladeSeed[] layout = key.GenerateLayout();

        Assert.AreEqual(100, layout.Length);
    }

    [Test]
    public void GenerateLayout_NonFiniteCellSize_ReturnsNull()
    {
        _grid.size = float.NaN;
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        Assert.IsNull(key.GenerateLayout());
    }

    [Test]
    public void FieldRect_Grid_ReturnsWorldCorners()
    {
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        Vector4 rect = key.FieldRect();

        Assert.AreEqual(new Vector4(-1f, -2.5f, 5f, 0.5f), rect); // centre (2, -1), half size (3, 1.5)
    }

    [Test]
    public void CalculateBounds_Grid_CoversFieldAndBladeEnvelope()
    {
        HLGrassBuildKey key = new HLGrassBuildKey(_grid, 0.5f, 3, 100);

        Bounds bounds = key.CalculateBounds();

        Assert.AreEqual(2f, bounds.center.x);
        Assert.AreEqual(-1f, bounds.center.z);
        Assert.AreEqual(7.7f, bounds.size.x, 0.0001f); // 4 * 1.5 + 2 * 0.85
        Assert.AreEqual(4.7f, bounds.size.z, 0.0001f); // 2 * 1.5 + 2 * 0.85
    }
}
}
