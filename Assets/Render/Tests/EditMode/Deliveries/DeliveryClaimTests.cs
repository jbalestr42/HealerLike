using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{

// A view that claims shots and records what the observer asks of it, through the two contracts alone
public class DeliveryProbe : MonoBehaviour, IDeliverySource, IDeliveryAccent
{
    public int begins;
    public int contacts;
    public int ends;
    public int accents;
    public bool accepts = true;
    public DeliveryStyle style;
    public int token;
    public Color accent;

    public bool BeginDelivery(int value, DeliveryStyle deliveryStyle, Transform projectile, Vector3 end)
    {
        begins++;
        style = deliveryStyle;
        if (accepts)
        {
            token = value;
        }
        return accepts;
    }

    public void ContactDelivery(int value, Vector3 position, GameObject target)
    {
        contacts++;
    }

    public void EndDelivery(int value)
    {
        ends++;
    }

    public void SetDeliveryAccent(int value, Color colour)
    {
        accents++;
        if (value == token)
        {
            accent = colour;
        }
    }
}

public class DeliveryClaimTests
{
    GameObject _model;
    GameObject _projectile;
    DeliveryProbe _probe;
    DeliveryClaim _claim;

    [SetUp]
    public void SetUp()
    {
        _model = new GameObject("Model");
        _projectile = new GameObject("Projectile");
        _probe = _model.AddComponent<DeliveryProbe>();
        _claim = new DeliveryClaim();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_model);
        Object.DestroyImmediate(_projectile);
    }

    [Test]
    public void TryClaim_OneSourceDeclines_AnotherClaims()
    {
        _probe.accepts = false;
        DeliveryProbe accepted = _model.AddComponent<DeliveryProbe>();

        bool isClaimed = _claim.TryClaim(_model, 7, DeliveryStyle.Arc, _projectile.transform, Vector3.one);

        Assert.IsTrue(isClaimed);
        Assert.AreEqual(1, accepted.begins);
        Assert.AreEqual(7, _claim.token);
    }

    [Test]
    public void TryClaim_NoToken_ClaimsNothing()
    {
        bool isClaimed = _claim.TryClaim(_model, 0, DeliveryStyle.Arc, _projectile.transform, Vector3.one);

        Assert.IsFalse(isClaimed);
        Assert.AreEqual(0, _probe.begins);
    }

    [Test]
    public void Tint_ClaimedShot_ReachesTheSourceByItsToken()
    {
        _claim.TryClaim(_model, 7, DeliveryStyle.Arc, _projectile.transform, Vector3.one);

        _claim.Tint(Color.cyan);

        Assert.AreEqual(Color.cyan, _probe.accent);
    }

    [Test]
    public void Release_Twice_EndsTheDeliveryOnce()
    {
        _claim.TryClaim(_model, 7, DeliveryStyle.Arc, _projectile.transform, Vector3.one);

        _claim.Release();
        _claim.Release();

        Assert.AreEqual(1, _probe.ends);
        Assert.IsFalse(_claim.isClaimed);
    }

    [Test]
    public void isLost_SourceSwitchedOff_IsTrue()
    {
        _claim.TryClaim(_model, 7, DeliveryStyle.Arc, _projectile.transform, Vector3.one);

        _probe.enabled = false;

        Assert.IsTrue(_claim.isLost);
    }
}

}
