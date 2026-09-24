using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{

public class TipDropTests
{
    GameObject _dropGo;
    TipDrop _drop;

    [SetUp]
    public void SetUp()
    {
        _dropGo = new GameObject("TipDrop");
        _drop = _dropGo.AddComponent<TipDrop>();
        LookPart pod = new LookPart { size = Vector3.one };
        PrimitiveMeshes meshes = RenderTestAssets.LoadMeshes();
        _drop.Init(pod, meshes.sphere, null, Color.white, Vector3.up, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (TipDrop drop in Object.FindObjectsByType<TipDrop>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(drop.gameObject);
        }
    }

    [Test]
    public void Tick_HalfItsLifetime_FallsAndShrinks()
    {
        Vector3 start = _dropGo.transform.position;

        _drop.Tick(TipDrop.Lifetime * 0.5f);

        Assert.Less(_dropGo.transform.position.y, start.y);
        Assert.Less(_dropGo.transform.localScale.x, 1f);
    }

    [Test]
    public void Tick_PastItsLifetime_DestroysItself()
    {
        _drop.Tick(TipDrop.Lifetime * 0.5f);

        _drop.Tick(TipDrop.Lifetime);

        Assert.IsFalse(_dropGo);
    }

    [Test]
    public void Splash_AreaShot_DropsOnePodAtTheContact()
    {
        int before = Object.FindObjectsByType<TipDrop>(FindObjectsSortMode.None).Length;

        TipDrop.Splash(RenderTestAssets.LoadDeliveryVocabulary(), RenderTestAssets.LoadMeshes(), null, Vector3.one * 2f);

        Assert.AreEqual(before + 1, Object.FindObjectsByType<TipDrop>(FindObjectsSortMode.None).Length);
    }

    [Test]
    public void Splash_NoVocabulary_DropsNothing()
    {
        int before = Object.FindObjectsByType<TipDrop>(FindObjectsSortMode.None).Length;

        TipDrop.Splash(null, RenderTestAssets.LoadMeshes(), null, Vector3.one);

        Assert.AreEqual(before, Object.FindObjectsByType<TipDrop>(FindObjectsSortMode.None).Length);
    }
}

}
