using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

namespace Skills
{

// What the player sees of the marked strike: a marker with the time left, an arc, an impact effect
public class MarkedStrikeViewTests
{
    class TestMarkedStrikeSkill : MarkedStrikeSkill
    {
        public GameObject nextTarget;

        protected override GameObject FindTarget()
        {
            return nextTarget;
        }
    }

    const float Interval = 8f;
    const float Delay = 3f;

    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    TestMarkedStrikeSkill _skill;
    MarkedStrikeView _view;
    Entity _target;
    GameObject _impactPrefab;

    [SetUp]
    public void SetUp()
    {
        GameObject markerGo = Track(new GameObject("Marker Prefab"));
        StrikeMarker markerPrefab = markerGo.AddComponent<StrikeMarker>();
        TestHelpers.SetPrivateField(markerPrefab, "_countdown", markerGo.AddComponent<TextMeshPro>());

        LineRenderer arcPrefab = Track(new GameObject("Arc Prefab")).AddComponent<LineRenderer>();
        _impactPrefab = Track(new GameObject("Impact"));

        ConsumerFactory strike = ScriptableObject.CreateInstance<ConsumerFactory>();
        strike.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 10f } } };
        _objects.Add(strike);

        Entity boss = _units.Create(2000f, 2000f, "Boss");
        _skill = boss.gameObject.AddComponent<TestMarkedStrikeSkill>();
        _skill.data = new MarkedStrikeSkillData
        {
            interval = Interval,
            delay = Delay,
            strikeConsumer = strike,
            markerPrefab = markerPrefab,
            arcPrefab = arcPrefab,
            impactPrefab = _impactPrefab,
        };
        _view = boss.gameObject.AddComponent<MarkedStrikeView>();
        _view.Init(_skill);

        _target = _units.Create(100f, 100f, "Target");
        _skill.nextTarget = _target.gameObject;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject clone in FindImpacts())
        {
            Object.DestroyImmediate(clone);
        }
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }
        _objects.Clear();
    }

    GameObject Track(GameObject go)
    {
        _objects.Add(go);
        return go;
    }

    static List<GameObject> FindImpacts()
    {
        List<GameObject> impacts = new List<GameObject>();
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "Impact(Clone)")
            {
                impacts.Add(go);
            }
        }
        return impacts;
    }

    string GetCountdown()
    {
        return _view.marker.GetComponent<TMP_Text>().text;
    }

    [Test]
    public void NothingMarked_ShowsNothing()
    {
        _view.Refresh(0f);

        Assert.IsNull(_view.marker);
        Assert.IsFalse(_view.isArcShown);
    }

    [Test]
    public void Marked_ShowsTheMarkerOnTheTargetWithTheTimeLeft_AndTheArc()
    {
        _skill.Tick(Interval);

        _view.Refresh(0f);

        Assert.IsNotNull(_view.marker);
        Assert.AreSame(_target.transform, _view.marker.transform.parent);
        Assert.AreEqual("3.0", GetCountdown());
        Assert.IsTrue(_view.isArcShown);
    }

    [Test]
    public void WhileMarked_TheCountdownGoesDown()
    {
        _skill.Tick(Interval);
        _view.Refresh(0f);

        _skill.Tick(1.2f);
        _view.Refresh(1.2f);

        Assert.AreEqual("1.8", GetCountdown());
    }

    [Test]
    public void AfterTheStrike_HidesTheMarkAndPlaysTheImpact()
    {
        _skill.Tick(Interval);
        _view.Refresh(0f);

        _skill.Tick(Delay);
        _view.Refresh(Delay);

        Assert.IsNull(_view.marker);
        Assert.IsFalse(_view.isArcShown);
        Assert.AreEqual(1, FindImpacts().Count);
    }

    [Test]
    public void StrikeLost_HidesTheMarkWithoutImpact()
    {
        _skill.Tick(Interval);
        _view.Refresh(0f);
        _target.health.SetValue(0f);

        _skill.Tick(Delay);
        _view.Refresh(Delay);

        Assert.IsNull(_view.marker);
        Assert.IsEmpty(FindImpacts());
    }

    [Test]
    public void MarkedTargetDestroyedBeforeTheStrike_HidesTheArc()
    {
        _skill.Tick(Interval);
        _view.Refresh(0f);

        // Killed by something else while marked: removed from the game before the strike
        Object.DestroyImmediate(_target.gameObject);
        _skill.Tick(0.1f);
        _view.Refresh(0.1f);

        Assert.IsNull(_view.marker);
        Assert.IsFalse(_view.isArcShown);
    }

    [Test]
    public void MarkedTargetDestroyed_TheNextMarkStillComes()
    {
        _skill.Tick(Interval);
        Object.DestroyImmediate(_target.gameObject);
        Entity other = _units.Create(100f, 100f, "Other");
        _skill.nextTarget = other.gameObject;

        _skill.Tick(Interval);
        _view.Refresh(Interval);

        Assert.AreSame(other.gameObject, _skill.markedTarget);
        Assert.IsTrue(_view.isArcShown);
        Assert.AreSame(other.transform, _view.marker.transform.parent);
    }

    [Test]
    public void WithAShakeForce_ShakesTheCameraWithABumpOfTheChosenDuration()
    {
        _skill.data.impactShakeForce = 0.5f;
        _skill.data.impactShakeDuration = 0.3f;
        MarkedStrikeView view = _skill.gameObject.AddComponent<MarkedStrikeView>();

        view.Init(_skill);

        Assert.IsNotNull(view.shakeSource);
        Assert.AreEqual(CinemachineImpulseDefinition.ImpulseShapes.Bump, view.shakeSource.ImpulseDefinition.ImpulseShape);
        Assert.AreEqual(0.3f, view.shakeSource.ImpulseDefinition.ImpulseDuration, 1e-5f);
    }

    [Test]
    public void WithoutShakeForce_DoesNotShakeTheCamera()
    {
        _skill.data.impactShakeForce = 0f;
        MarkedStrikeView view = _skill.gameObject.AddComponent<MarkedStrikeView>();

        view.Init(_skill);

        Assert.IsNull(view.shakeSource);
    }

    [Test]
    public void ShakeAlone_CountsAsVisuals()
    {
        MarkedStrikeSkillData data = new MarkedStrikeSkillData { impactShakeForce = 0.4f };

        Assert.IsTrue(data.hasVisuals);
    }

    [TestCase(2.44f, "2.4")]
    [TestCase(0f, "0.0")]
    [TestCase(-1f, "0.0")]
    public void FormatCountdown_ShowsTenthsOfSeconds(float seconds, string expected)
    {
        Assert.AreEqual(expected, StrikeMarker.FormatCountdown(seconds));
    }
}

}
