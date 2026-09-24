using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class ChainLightningProjectileTests
{
    GameObject _source;
    GameObject _target;
    GameObject _healthGo;
    EntityModel _model;
    readonly List<SkillSource> _sourcePoints = new List<SkillSource>();
    ChainLightningProjectile _projectile;

    [SetUp]
    public void SetUp()
    {
        Entity sourceEntity = null;
        Entity targetEntity = null;
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            sourceEntity = _source.AddComponent<Entity>();
            targetEntity = _target.AddComponent<Entity>();
        });

        // Four source points so one advance and one advance per frame land on different points
        GameObject modelGo = new GameObject("Model");
        modelGo.transform.SetParent(_source.transform);
        for (int i = 0; i < 4; i++)
        {
            GameObject sourcePointGo = new GameObject($"SourcePoint {i}");
            sourcePointGo.transform.SetParent(modelGo.transform);
            sourcePointGo.transform.position = new Vector3(i, 0f, 0f);
            _sourcePoints.Add(sourcePointGo.AddComponent<SkillSource>());
        }
        _model = modelGo.AddComponent<EntityModel>();
        _model.Init(sourceEntity);
        TestHelpers.SetPrivateField(sourceEntity, "_model", _model);

        _healthGo = new GameObject("Health");
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_healthGo, AttributeType.HealthMax, 10f);
        TestHelpers.SetPrivateField(targetEntity, "_health", health);
        TestHelpers.SetPrivateField(targetEntity, "_targetPoint", _target);

        GameObject projectileGo = new GameObject("ChainLightning");
        projectileGo.AddComponent<LineRenderer>();
        _projectile = projectileGo.AddComponent<ChainLightningProjectile>();
        TestHelpers.SetPrivateField(_projectile, "_effectDuration", 100f);
        _projectile.source = _source;
        _projectile.SetTarget(_target);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_healthGo);
        Object.DestroyImmediate(_projectile.gameObject);
        _sourcePoints.Clear();
    }

    static IEnumerator StartEffect(ChainLightningProjectile projectile)
    {
        MethodInfo start = typeof(ChainLightningProjectile).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IEnumerator)start.Invoke(projectile, null);
    }

    [Test]
    public void Start_OverSeveralFrames_AdvancesTheSourcePointOnce()
    {
        IEnumerator effect = StartEffect(_projectile);

        for (int i = 0; i < 3; i++)
        {
            effect.MoveNext();
        }

        Assert.AreSame(_sourcePoints[1], _model.GetSourcePoint()); // 3 reads would give point 3
    }

    [Test]
    public void Start_OverSeveralFrames_DrawsFromTheSameSourcePoint()
    {
        IEnumerator effect = StartEffect(_projectile);
        LineRenderer lineRenderer = _projectile.GetComponent<LineRenderer>();

        for (int i = 0; i < 3; i++)
        {
            effect.MoveNext();

            Assert.AreEqual(_sourcePoints[0].transform.position, lineRenderer.GetPosition(0));
        }
    }
}

}
