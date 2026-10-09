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
        // Picked after nextTarget, in this order
        public List<GameObject> nextOthers = new List<GameObject>();

        protected override List<GameObject> FindTargets(int count)
        {
            List<GameObject> targets = new List<GameObject>();
            if (nextTarget != null)
            {
                targets.Add(nextTarget);
                targets.AddRange(nextOthers);
            }
            return targets.GetRange(0, Mathf.Min(count, targets.Count));
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

    [Test]
    public void SeveralMarkedUnits_ShowsAMarkerAndAnArcOnEach_SmallerWhenTheyTakeLess()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        Entity third = _units.Create(100f, 100f, "Third");
        _skill.nextOthers.Add(second.gameObject);
        _skill.nextOthers.Add(third.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f, 0.25f };
        _skill.Tick(Interval);

        _view.Refresh(0f);

        List<StrikeMarker> markers = _view.markers;
        Assert.AreEqual(3, markers.Count);
        Assert.AreSame(second.transform, markers[1].transform.parent);
        Assert.AreEqual(1f, markers[0].transform.localScale.x, 0.0001f);
        Assert.AreEqual(Mathf.Sqrt(0.5f), markers[1].transform.localScale.x, 0.0001f);
        Assert.AreEqual(0.5f, markers[2].transform.localScale.x, 0.0001f);
        Assert.AreEqual(3, _view.shownArcCount);
    }

    [Test]
    public void SeveralMarkedUnits_OnlyTheMainOneShowsTheCountdown()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        _skill.nextOthers.Add(second.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f };
        _skill.Tick(Interval);

        _view.Refresh(0f);

        Assert.IsTrue(_view.markers[0].isCountdownShown);
        Assert.IsFalse(_view.markers[1].isCountdownShown);
    }

    [Test]
    public void SeveralMarkedUnits_TheArcsGoFromTheEntityToTheMainUnit_ThenFromUnitToUnit()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        Entity third = _units.Create(100f, 100f, "Third");
        _skill.transform.position = Vector3.zero;
        _target.transform.position = new Vector3(2f, 0f, 0f);
        second.transform.position = new Vector3(4f, 0f, 0f);
        third.transform.position = new Vector3(6f, 0f, 0f);
        _skill.nextOthers.Add(second.gameObject);
        _skill.nextOthers.Add(third.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f, 0.25f };
        _skill.Tick(Interval);

        _view.Refresh(0f);

        List<LineRenderer> arcs = _view.shownArcs;
        Vector3[] ends = { Vector3.zero, _target.transform.position, second.transform.position, third.transform.position };
        for (int i = 0; i < arcs.Count; i++)
        {
            Assert.That(Vector3.Distance(ends[i], arcs[i].GetPosition(0)), Is.LessThan(0.001f), $"arc {i} start");
            Assert.That(Vector3.Distance(ends[i + 1], arcs[i].GetPosition(arcs[i].positionCount - 1)), Is.LessThan(0.001f), $"arc {i} end");
        }
        Assert.AreEqual(3, arcs.Count);
    }

    [Test]
    public void MainMarkedUnitDestroyed_TheNextOneShowsTheCountdown()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        _skill.nextOthers.Add(second.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f };
        _skill.Tick(Interval);
        _view.Refresh(0f);

        Object.DestroyImmediate(_target.gameObject);
        _skill.Tick(0.1f);
        _view.Refresh(0.1f);

        Assert.IsTrue(_view.marker.isCountdownShown);
    }

    [Test]
    public void SeveralMarkedUnits_AfterTheStrike_HidesEveryMarkAndPlaysAnImpactOnEach()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        _skill.nextOthers.Add(second.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f };
        _skill.Tick(Interval);
        _view.Refresh(0f);

        _skill.Tick(Delay);
        _view.Refresh(Delay);

        Assert.IsEmpty(_view.markers);
        Assert.IsFalse(_view.isArcShown);
        Assert.AreEqual(2, FindImpacts().Count);
    }

    [Test]
    public void OneOfTheMarkedUnitsDestroyed_ItsMarkGoes_TheOthersStay()
    {
        Entity second = _units.Create(100f, 100f, "Second");
        _skill.nextOthers.Add(second.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f };
        _skill.Tick(Interval);
        _view.Refresh(0f);

        Object.DestroyImmediate(_target.gameObject);
        _skill.Tick(0.1f);
        _view.Refresh(0.1f);

        Assert.AreEqual(1, _view.markers.Count);
        Assert.AreSame(second.transform, _view.marker.transform.parent);
        Assert.AreEqual(1, _view.shownArcCount);
    }

    // The area of the marker follows the damage taken
    [TestCase(1f, 1f)]
    [TestCase(0.25f, 0.5f)]
    [TestCase(0f, 0f)]
    [TestCase(2f, 1f)]
    public void MarkerScale_IsTheSquareRootOfThePartOfTheStrike(float damageMultiplier, float expected)
    {
        Assert.AreEqual(expected, MarkedStrikeView.GetMarkerScale(damageMultiplier), 0.0001f);
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
