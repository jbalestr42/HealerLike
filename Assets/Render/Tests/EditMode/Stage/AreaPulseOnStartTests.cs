using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class AreaPulseOnStartTests
{
    GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Area");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Start_RunsThePulseOnce()
    {
        int pulses = 0;
        AreaPulseOnStart pulse = _go.AddComponent<AreaPulseOnStart>();
        pulse.Init(() => pulses++);

        Assert.AreEqual(0, pulses);
        TestHelpers.InvokePrivate(pulse, "Start");

        Assert.AreEqual(1, pulses);
    }

    [Test]
    public void Start_WithoutInit_DoesNothing()
    {
        AreaPulseOnStart pulse = _go.AddComponent<AreaPulseOnStart>();

        Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(pulse, "Start"));
    }
}

}
