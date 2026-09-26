using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class BoostCellGroundTests
{
    GameObject _root;
    GameObject _owner;
    ZoneRegistry _zones;
    BoostCellGround _ground;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _zones = _root.AddComponent<ZoneRegistry>();
        _zones.Init(new ZoneFakeUpload());
        _owner = new GameObject("entity");
        _ground = _root.AddComponent<BoostCellGround>();
        _ground.Init(_owner.transform, _zones, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_root);
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
        _zones.PublishFrame(0f);

        Assert.AreEqual(2, _ground.patchCount);
        Assert.IsFalse(first.GetComponent<Renderer>().enabled, "The flat tile is hidden.");
        Assert.AreEqual(2, _zones.count);
        bool isUnderFirst = false;
        foreach (Zone zone in _zones.snapshot)
        {
            Assert.AreEqual((int)ZoneKind.Boost, zone.kind);
            Assert.AreEqual(BoostCellGround.PatchRadius, zone.radius, 1e-6f);
            isUnderFirst |= zone.position == new Vector3(1f, 0f, 1f);
        }

        Assert.IsTrue(isUnderFirst, "A patch sits under each cell.");
    }

    [Test]
    public void Refresh_CellRemoved_DropsItsPatch()
    {
        GameObject cell = AddCell(Vector3.one);
        _ground.Refresh();

        Object.DestroyImmediate(cell);
        _ground.Refresh();

        Assert.AreEqual(0, _ground.patchCount);
        Assert.AreEqual(0, _zones.liveCount);
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
        Assert.AreEqual(1, _zones.liveCount);
    }

    [Test]
    public void OnDisable_ClearsEveryPatch()
    {
        AddCell(Vector3.one);
        _ground.Refresh();

        TestHelpers.InvokePrivate(_ground, "OnDisable");

        Assert.AreEqual(0, _zones.liveCount);
    }
}

}
