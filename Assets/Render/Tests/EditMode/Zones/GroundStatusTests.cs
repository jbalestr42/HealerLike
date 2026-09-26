using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class GroundStatusTests
{
    static readonly string poisonPath = "Assets/Data/CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory.asset";
    static readonly string slowPath = "Assets/Data/EntityItems/SlowItem/BuffHandlerFactory.asset";

    GameObject _root;
    ZoneRegistry _zones;
    GroundStatus _status;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("status");
        _zones = _root.AddComponent<ZoneRegistry>();
        _zones.Init(new ZoneFakeUpload());
        _status = _root.AddComponent<GroundStatus>();
        _status.Init(null, _zones, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    static ABuffHandlerFactory Load(string path)
    {
        ABuffHandlerFactory factory = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
        Assert.IsNotNull(factory, path);
        return factory;
    }

    int Count(ZoneKind kind)
    {
        _zones.PublishFrame(0f);
        int count = 0;
        foreach (Zone zone in _zones.snapshot)
        {
            count += zone.kind == (int)kind ? 1 : 0;
        }

        return count;
    }

    [Test]
    public void IsPoisonAndIsSlow_GameData_ClassifyThePoisonAndTheSlow()
    {
        ABuffHandlerFactory poison = Load(poisonPath);
        ABuffHandlerFactory slow = Load(slowPath);

        Assert.IsTrue(GroundStatus.IsPoison(poison, false));
        Assert.IsFalse(GroundStatus.IsSlow(poison));
        Assert.IsTrue(GroundStatus.IsSlow(slow));
        Assert.IsFalse(GroundStatus.IsPoison(slow, false));
        Assert.IsFalse(GroundStatus.IsPoison(null, false));
        Assert.IsFalse(GroundStatus.IsSlow(null));
    }

    [Test]
    public void Refresh_PoisonedThenCured_BlightsTheGrassOnlyMeanwhile()
    {
        BuffManager.BuffHandlerData poison = new BuffManager.BuffHandlerData { buffHandlerFactory = Load(poisonPath) };

        _status.OnStarted(poison);
        _status.Refresh();

        Assert.IsTrue(_status.isPoisoned);
        Assert.AreEqual(1, Count(ZoneKind.Blight));
        Assert.AreEqual(0, Count(ZoneKind.Frost));

        _status.OnStopped(poison);
        _status.Refresh();

        Assert.IsFalse(_status.isPoisoned);
        Assert.AreEqual(0, Count(ZoneKind.Blight));
    }

    [Test]
    public void Refresh_Slowed_FrostsTheGrass()
    {
        _status.OnStarted(new BuffManager.BuffHandlerData { buffHandlerFactory = Load(slowPath) });
        _status.Refresh();

        Assert.IsTrue(_status.isSlowed);
        Assert.AreEqual(1, Count(ZoneKind.Frost));
    }

    [Test]
    public void OnStarted_NoFactory_IsIgnored()
    {
        _status.OnStarted(null);
        _status.OnStarted(new BuffManager.BuffHandlerData());

        Assert.IsFalse(_status.isPoisoned);
        Assert.IsFalse(_status.isSlowed);
    }
}

}
