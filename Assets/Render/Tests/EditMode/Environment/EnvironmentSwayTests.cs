using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{

public class EnvironmentSwayTests
{
    GameObject _go;
    GameObject _cameraGo;
    EnvironmentSway _sway;
    Camera _camera;
    Transform _plant;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("SwayTest");
        _sway = _go.AddComponent<EnvironmentSway>();
        _cameraGo = new GameObject("SwayCamera");
        _camera = _cameraGo.AddComponent<Camera>();
        _plant = new GameObject("Plant").transform;
        _plant.SetParent(_go.transform, false);
        _plant.localPosition = new Vector3(3f, 0f, 2f);
        _plant.localRotation = Quaternion.Euler(0f, 30f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_cameraGo);
    }

    [Test]
    public void Animate_BeyondFarDistance_FreezesAndResumesWithoutAccumulating()
    {
        _sway.Init(_camera, null, 10f, 0d);
        _sway.Add(_plant, _plant, 5, 1.2f, 0f);
        Quaternion rest = _plant.localRotation;
        _cameraGo.transform.position = Vector3.one * 1000f;

        _sway.Animate(10);

        Assert.AreEqual(rest, _plant.localRotation);

        _cameraGo.transform.position = _plant.position;
        _sway.Animate(10);
        Quaternion pose = _plant.localRotation;

        Assert.That(Quaternion.Angle(rest, pose), Is.GreaterThan(0.001f));

        _cameraGo.transform.position = Vector3.one * 1000f;
        _sway.Animate(11);

        Assert.AreEqual(pose, _plant.localRotation);

        _cameraGo.transform.position = _plant.position;
        _sway.Animate(10);

        Assert.AreEqual(pose, _plant.localRotation);
    }

    [Test]
    public void Animate_NoCamera_MovesEveryPlant()
    {
        _sway.Init(null, null, 1f, 0d);
        _sway.Add(_plant, _plant, 5, 1.2f, 0f);
        Quaternion rest = _plant.localRotation;
        _plant.position = Vector3.one * 1000f;

        _sway.Animate(10);

        Assert.That(Quaternion.Angle(rest, _plant.localRotation), Is.GreaterThan(0.001f));
    }

    [Test]
    public void Animate_Gust_OpensAnUncurlingJoint()
    {
        EnvironmentGust gust = _go.AddComponent<EnvironmentGust>();
        _sway.Init(null, gust, 100f, 0d);
        _plant.localRotation = Quaternion.Euler(0f, 0f, -30f);
        _sway.Add(_plant, _plant, 5, 0.12f, -3f);
        _sway.Animate(10);
        float resting = _plant.localEulerAngles.z;

        gust.GustAt(Vector3.forward, 1f, 2f, 9);
        _sway.Animate(10);

        Assert.That(Mathf.DeltaAngle(resting, _plant.localEulerAngles.z), Is.GreaterThan(2f));
    }

    [Test]
    public void Animate_AfterBuild_SettleDecaysAndSamplingIsStable()
    {
        _sway.Add(_plant, _plant, 5, 1.2f, 0f);
        _sway.Init(null, null, 100f, 10d);
        _sway.Animate(10.1);
        Quaternion settling = _plant.localRotation;

        _sway.Init(null, null, 100f, 0d);
        _sway.Animate(10.1);

        Assert.That(Quaternion.Angle(settling, _plant.localRotation), Is.GreaterThan(1f));

        Quaternion settled = _plant.localRotation;
        _sway.Animate(10.1);

        Assert.AreEqual(settled, _plant.localRotation);
    }

    [Test]
    public void Animate_AfterWarmup_AllocatesNothing()
    {
        _sway.Init(_camera, _go.AddComponent<EnvironmentGust>(), 100f, 0d);
        _sway.Add(_plant, _plant, 5, 1.2f, 0f);
        for (int i = 0; i < 10; i++)
        {
            _sway.Animate(i);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _sway.Animate(i);
        }
        long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, bytes);
    }
}

}
