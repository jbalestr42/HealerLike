using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class BoostCellGroundTests
{
    GameObject _root;
    GameObject _owner;
    Ground _groundService;
    BoostCellGround _ground;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _groundService = new Ground();
        _owner = new GameObject("entity");
        _ground = _root.AddComponent<BoostCellGround>();
        _ground.Init(_owner.transform, _groundService, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_root);
        _groundService.Dispose();
    }

    // A cell as the buff lays it: a child of the entity carrying its own flat tile
    GameObject AddCell(Vector3 position)
    {
        GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cell.transform.SetParent(_owner.transform, false);
        cell.transform.position = position;
        cell.AddComponent<BoostCell>();
        return cell;
    }

    [Test]
    public void Refresh_NewCells_HidesTheirTilesAndLightsTheGrassUnderEach()
    {
        GameObject first = AddCell(new Vector3(1f, 0f, 1f));
        AddCell(new Vector3(-1f, 0f, 1f));
        new GameObject("not a cell").transform.SetParent(_owner.transform, false);

        _ground.Refresh();

        Assert.AreEqual(2, _ground.patchCount);
        Assert.IsFalse(first.GetComponent<Renderer>().enabled, "The flat tile is hidden.");
        Assert.AreEqual(2, GroundProbe.Count(_groundService, GroundStampKind.Aura));
        Vector4 lit = GroundProbe.State(_groundService, new Vector3(1f, 0f, 1f));
        Assert.AreEqual(_groundService.vocabulary.boost.vitality, lit.y, 1e-5f, "Lush under the cell.");
        Assert.AreEqual(Vector4.zero, GroundProbe.State(_groundService, new Vector3(1f, 0f, 2f)),
            "A soft patch inside its cell.");
    }

    [Test]
    public void Refresh_CellRemoved_DropsItsPatch()
    {
        GameObject cell = AddCell(Vector3.one);
        _ground.Refresh();

        Object.DestroyImmediate(cell);
        _ground.Refresh();

        Assert.AreEqual(0, _ground.patchCount);
        Assert.AreEqual(0, _groundService.heldCount);
    }

    [Test]
    public void Refresh_ChildLeavesAsACellArrives_StillFindsTheCell()
    {
        GameObject effect = new GameObject("buff effect");
        effect.transform.SetParent(_owner.transform, false);
        _ground.Refresh();

        Object.DestroyImmediate(effect);
        GameObject cell = AddCell(Vector3.one);
        _ground.Refresh();

        Assert.AreEqual(1, _ground.patchCount, "Same number of children, not the same children.");
        Assert.IsFalse(cell.GetComponent<Renderer>().enabled);
    }

    [Test]
    public void Refresh_Steady_KeepsOnePatchPerCell()
    {
        AddCell(Vector3.one);

        for (int i = 0; i < 5; i++)
        {
            _ground.Refresh();
        }

        Assert.AreEqual(1, _ground.patchCount);
        Assert.AreEqual(1, _groundService.heldCount);
    }

    [Test]
    public void DisableAndReenable_RestoresOriginalRendererStatesAndReacquiresCells()
    {
        Renderer visible = AddCell(Vector3.one).GetComponent<Renderer>();
        Renderer hidden = AddCell(Vector3.left).GetComponent<Renderer>();
        hidden.enabled = false;
        _ground.Refresh();
        Assert.IsFalse(visible.enabled);
        _ground.enabled = false;
        TestHelpers.InvokePrivate(_ground, "OnDisable");
        _ground.Refresh();
        Assert.IsTrue(visible.enabled);
        Assert.IsFalse(hidden.enabled);
        Assert.AreEqual(0, _ground.patchCount);
        _ground.enabled = true;
        _ground.Refresh();
        Assert.AreEqual(2, _ground.patchCount);
        Assert.IsFalse(visible.enabled);
        TestHelpers.InvokePrivate(_ground, "OnDestroy");
        Object.DestroyImmediate(_ground);
        Assert.IsTrue(visible.enabled);
        Assert.IsFalse(hidden.enabled);
    }

    [Test]
    public void InactiveCellResumesAndReparentedCellRestoresItsBorrowedRenderer()
    {
        GameObject cell = AddCell(Vector3.one);
        _ground.Refresh();
        cell.SetActive(false);
        _ground.Refresh();
        Assert.AreEqual(0, GroundProbe.Stamps(_groundService).Count);
        cell.SetActive(true);
        _ground.Refresh();
        Assert.AreEqual(1, GroundProbe.Stamps(_groundService).Count);
        cell.transform.SetParent(_root.transform, false);
        _ground.Refresh();
        Assert.AreEqual(0, _ground.patchCount);
        Assert.AreEqual(0, _groundService.heldCount);
        Assert.IsTrue(cell.GetComponent<Renderer>().enabled);
    }

    [Test]
    public void OnDisable_ClearsEveryPatch()
    {
        AddCell(Vector3.one);
        _ground.Refresh();

        TestHelpers.InvokePrivate(_ground, "OnDisable");

        Assert.AreEqual(0, _groundService.heldCount);
    }
}

}
