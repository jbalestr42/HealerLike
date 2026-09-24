using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

// A view that publishes fixed anchors, standing in for the creature builder
public class FakeEffectAnchors : MonoBehaviour, IEffectAnchors
{
    public EffectAnchors anchors;

    public bool TryGetAnchors(out EffectAnchors published)
    {
        published = anchors;
        return true;
    }
}

public class EffectPlacementTests
{
    GameObject _host;
    GameObject _target;
    SpellVisualSink _sink;
    readonly List<Object> _created = new List<Object>();
    readonly List<GameObject> _units = new List<GameObject>();
    readonly List<CreatureRig> _rigs = new List<CreatureRig>();

    // A long stem with a small head high over the body
    static EffectAnchors Tall()
    {
        EffectAnchors anchors = new EffectAnchors();
        anchors.foot = Vector3.zero;
        anchors.bodyCentre = new Vector3(0f, 0.35f, 0f);
        anchors.bodyRadius = 0.28f;
        anchors.neck = new Vector3(0f, 0.65f, 0f);
        anchors.headCentre = new Vector3(0f, 1.05f, 0f);
        anchors.headRadius = 0.2f;
        return anchors;
    }

    // No stem: the head sinks into the top of the body, a little off centre
    static EffectAnchors Squat()
    {
        EffectAnchors anchors = new EffectAnchors();
        anchors.foot = Vector3.zero;
        anchors.bodyCentre = new Vector3(0f, 0.3f, 0f);
        anchors.bodyRadius = 0.3f;
        anchors.neck = new Vector3(0f, 0.55f, 0f);
        anchors.headCentre = new Vector3(0.05f, 0.62f, 0f);
        anchors.headRadius = 0.18f;
        return anchors;
    }

    BuffHandlerFactory Handler(EffectElement element)
    {
        switch (element)
        {
            case EffectElement.Orbit:
                return SpellSinkFixture.Modifier(AttributeType.Damage, 10f, _created);
            case EffectElement.Plates:
                return SpellSinkFixture.Modifier(AttributeType.HitArmor, 2f, _created);
            case EffectElement.Bud:
                return SpellSinkFixture.Invincible(_created);
            case EffectElement.Press:
                return SpellSinkFixture.Modifier(AttributeType.Damage, -5f, _created);
            case EffectElement.Crack:
                return SpellSinkFixture.Modifier(AttributeType.FlatArmor, -5f, _created);
            case EffectElement.Drips:
                return SpellSinkFixture.Consumer(10f, 2f, _created);
            default:
                return SpellSinkFixture.Consumer(-4f, 1f, _created);
        }
    }

    // The anchors the rig publishes for a unit the composer builds from these channels
    EffectAnchors ComposeAnchors(LookSide side, HeadKind head)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(side, head);
        CreatureRecipe recipe = LookComposer.Compose(channels, RenderTestAssets.LoadLookVocabulary());
        _created.Add(recipe);
        GameObject unitGo = new GameObject(side + " " + head);
        _units.Add(unitGo);
        CreatureRig rig = RenderTestAssets.CreateRig(recipe, unitGo.transform, RenderTestAssets.LoadLookMaterial());
        _rigs.Add(rig);
        Assert.IsTrue(rig.TryGetAnchors(out EffectAnchors anchors), side + " " + head);
        return anchors;
    }

    void AssertOffTheHead(GameObject target, BuffHandlerFactory handler, EffectElement element, EffectAnchors anchors,
                          string unit)
    {
        float grown = anchors.headRadius + EffectPlacement.HeadMargin * anchors.bodyRadius;
        _sink.SetStatus(null, target, handler, 3, 0f, 6f);
        SpellEffect effect = _sink.GetElement(target, element);
        Assert.IsNotNull(effect, unit);
        Assert.IsTrue(effect.isLasting, unit);
        for (float time = 0f; time < 6f; time += 0.1f)
        {
            _sink.SetStatus(null, target, handler, 3, time, 6f);
            foreach (Renderer part in effect.GetComponentsInChildren<Renderer>())
            {
                if (part.bounds.size.sqrMagnitude < 0.00000001f)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(part.bounds.SqrDistance(anchors.headCentre));
                Assert.GreaterOrEqual(distance, grown - 0.001f, $"{unit}: {part.name} at {time:0.0} s");
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Host");
        _target = new GameObject("Target");
        _sink = SpellSinkFixture.Add(_host);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_sink, "OnDestroy");
        foreach (CreatureRig rig in _rigs)
        {
            rig.Dispose();
        }

        _rigs.Clear();
        foreach (GameObject unitGo in _units)
        {
            Object.DestroyImmediate(unitGo);
        }

        _units.Clear();
        Object.DestroyImmediate(_host);
        Object.DestroyImmediate(_target);
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }

        _created.Clear();
    }

    // Every head the composer draws, on each side: an element alive longer than the lasting limit keeps its
    // bounds out of the head sphere grown by a tenth of a body unit, over the whole of a status
    [Test]
    public void SetStatus_LastingElementOnEveryComposedHead_NeverReachesTheHead(
        [Values(EffectElement.Orbit, EffectElement.Plates, EffectElement.Bud, EffectElement.Press, EffectElement.Crack,
                EffectElement.Drips, EffectElement.Stalks)] EffectElement element,
        [Values(LookSide.Plant, LookSide.Stone)] LookSide side)
    {
        BuffHandlerFactory handler = Handler(element);

        foreach (HeadKind head in System.Enum.GetValues(typeof(HeadKind)))
        {
            EffectAnchors anchors = ComposeAnchors(side, head);
            GameObject target = new GameObject(head.ToString());
            _units.Add(target);
            target.AddComponent<FakeEffectAnchors>().anchors = anchors;
            AssertOffTheHead(target, handler, element, anchors, head.ToString());
        }
    }

    [Test]
    public void SetStatus_BoonOffenceAndDefence_OrbitAndPlatesAtOnce()
    {
        _target.AddComponent<FakeEffectAnchors>().anchors = Tall();
        BuffHandlerFactory offence = Handler(EffectElement.Orbit);
        BuffHandlerFactory defence = Handler(EffectElement.Plates);

        _sink.SetStatus(null, _target, offence, 1, 0f, 6f);
        _sink.SetStatus(null, _target, defence, 1, 0f, 6f);

        Assert.AreEqual(2, _sink.statusCount);
        Assert.IsNotNull(_sink.GetElement(_target, EffectElement.Orbit));
        Assert.IsNotNull(_sink.GetElement(_target, EffectElement.Plates));
    }

    [Test]
    public void SetStatus_Rot_DripsStartUnderTheHeadOnTheRightAndFallToTheGround()
    {
        EffectAnchors anchors = Tall();
        _target.AddComponent<FakeEffectAnchors>().anchors = anchors;
        BuffHandlerFactory rot = Handler(EffectElement.Drips);

        _sink.SetStatus(null, _target, rot, 1, 0f, 6f);

        Transform root = _sink.GetStatus(_target, rot).transform;
        Assert.Greater(root.position.x, anchors.bodyCentre.x + anchors.bodyRadius);
        Assert.Less(root.position.y, anchors.headCentre.y - anchors.headRadius);
        SpellEffect effect = root.GetComponent<SpellEffect>();
        effect.Pose(1f / EffectMotion.FallPace, 0f);
        Assert.AreEqual(anchors.foot.y, effect.shapes[0].position.y, 0.01f);
    }

    [Test]
    public void SetStatus_RenewUnderALargeHead_StalksWidenAndKeepTheirHeight()
    {
        EffectAnchors anchors = Tall();
        anchors.headCentre = new Vector3(0f, 0.75f, 0f);
        anchors.headRadius = 0.4f;
        _target.AddComponent<FakeEffectAnchors>().anchors = anchors;
        BuffHandlerFactory renew = Handler(EffectElement.Stalks);

        _sink.SetStatus(null, _target, renew, 1, 1.5f, 6f);

        float top = 0f;
        foreach (Transform sphere in _sink.GetElement(_target, EffectElement.Stalks).shapes)
        {
            if (sphere.gameObject.activeSelf)
            {
                top = Mathf.Max(top, sphere.position.y - anchors.foot.y);
            }
        }
        Assert.Greater(top, 2f * anchors.bodyRadius);
    }

    [Test]
    public void SetStatus_Weaken_ConesSitAQuarterUnitOverTheHead()
    {
        EffectAnchors anchors = Squat();
        _target.AddComponent<FakeEffectAnchors>().anchors = anchors;
        BuffHandlerFactory weaken = Handler(EffectElement.Press);

        _sink.SetStatus(null, _target, weaken, 1, 0f, 6f);

        Transform root = _sink.GetStatus(_target, weaken).transform;
        float top = anchors.headCentre.y + anchors.headRadius;
        Assert.AreEqual(top + EffectPlacement.AboveHeadGap * anchors.bodyRadius, root.position.y, 0.001f);
    }

    [Test]
    public void SetStatus_Sanctuary_BudClosesToTheNeck()
    {
        EffectAnchors anchors = Tall();
        _target.AddComponent<FakeEffectAnchors>().anchors = anchors;
        BuffHandlerFactory sanctuary = Handler(EffectElement.Bud);

        _sink.SetStatus(null, _target, sanctuary, 1, 1f, 6f);

        float top = float.NegativeInfinity;
        foreach (Transform plate in _sink.GetElement(_target, EffectElement.Bud).shapes)
        {
            top = Mathf.Max(top, EffectPlacement.WorldBounds(plate).max.y);
        }
        Assert.AreEqual(anchors.neck.y, top, 0.02f);
    }

    [Test]
    public void Anchors_NoView_FallsBackOnTheTargetPoint()
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _target.AddComponent<Entity>());
        GameObject point = new GameObject("TargetPoint");
        _created.Add(point);
        point.transform.SetParent(_target.transform);
        point.transform.position = new Vector3(1f, 0.4f, 2f);
        TestHelpers.SetPrivateField(entity, "_targetPoint", point);
        BuffHandlerFactory boon = Handler(EffectElement.Orbit);

        _sink.SetStatus(null, _target, boon, 1, 0f, 4f);

        EffectAnchors anchors = EffectPlacement.Anchors(_target);
        Transform root = _sink.GetStatus(_target, boon).transform;
        Assert.AreEqual(point.transform.position, anchors.bodyCentre);
        Assert.AreEqual(EffectPlacement.FallbackBodyRadius, anchors.bodyRadius);
        Assert.AreEqual(point.transform, root.parent);
    }

    [TestCase(1.5f, 0f, false, 0f)] // beside the sphere
    [TestCase(0f, -1.5f, false, 0f)] // well under it
    [TestCase(0f, 0f, false, 1.25f)] // centred: the top must drop under the sphere's bottom
    [TestCase(0f, 0f, true, 1.25f)]
    public void Overlap_BoxAndSphere_VerticalMoveToClear(float x, float y, bool isUp, float expected)
    {
        Bounds box = new Bounds(new Vector3(x, y, 0f), new Vector3(0.5f, 0.5f, 0.5f));

        Assert.AreEqual(expected, EffectPlacement.Overlap(box, Vector3.zero, 1f, isUp), 0.0001f);
    }
}

}
