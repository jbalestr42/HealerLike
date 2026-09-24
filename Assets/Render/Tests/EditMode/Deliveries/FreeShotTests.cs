using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{

public class FreeShotTests
{
    GameObject _projectileGo;
    GameObject _target;
    Projectile _projectile;
    FreeShot _shot;

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _target.transform.position = Vector3.right * 3f;
        _projectileGo = new GameObject("Projectile");
        _projectile = _projectileGo.AddComponent<Projectile>();
        _projectile.targetPoint = _target;
        _shot = _projectileGo.AddComponent<FreeShot>();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_shot, "OnDestroy");
        Object.DestroyImmediate(_projectileGo);
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void Init_ThrownStyle_HasNoTipToShow()
    {
        bool isShown = _shot.Init(_projectile, DeliveryStyle.Thrown, RenderTestAssets.LoadDeliveryVocabulary(),
            RenderTestAssets.LoadMeshes());

        Assert.IsFalse(isShown);
    }

    [Test]
    public void Init_BeforeTheFirstMove_LooksAtItsTargetAtTheBulletSize()
    {
        DeliveryVocabulary vocabulary = RenderTestAssets.LoadDeliveryVocabulary();

        bool isShown = _shot.Init(_projectile, DeliveryStyle.Direct, vocabulary, RenderTestAssets.LoadMeshes());

        Assert.IsTrue(isShown);
        Vector3 forward = _shot.frame.MultiplyVector(Vector3.forward);
        Assert.That(Vector3.Dot(forward.normalized, Vector3.right), Is.GreaterThan(0.99f));
        Assert.AreEqual(vocabulary.bulletSize, forward.magnitude, 0.0001f);
    }
}

}
