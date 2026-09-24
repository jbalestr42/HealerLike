using NUnit.Framework;
using UnityEngine;

namespace Grids
{

public class GridManagerTests
{
    GameObject _go;
    GameObject _ground;
    GridManager _gridManager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("GridManager");
        _gridManager = _go.AddComponent<GridManager>();
        _ground = new GameObject("Ground");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_ground);
    }

    [Test]
    public void Ground_ReturnsTheSerializedGround()
    {
        TestHelpers.SetPrivateField(_gridManager, "_ground", _ground);

        Assert.AreSame(_ground, _gridManager.ground);
    }
}

}
