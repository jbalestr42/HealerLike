using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{

public class LianaPoseTests
{
    CreatureRecipe _recipe;
    LianaPose _pose;

    static LianaPose CreatePose(ArmDefinition definition)
    {
        LianaPose pose = new LianaPose();
        pose.Init(definition, 1f);
        pose.Tick(0f, Vector3.zero, Quaternion.identity, DeliveryVocabulary.BendingArm);
        return pose;
    }

    [SetUp]
    public void SetUp()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _pose = CreatePose(_recipe.arms[0]);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_recipe);
    }

    void Tick(float deltaTime)
    {
        _pose.Tick(deltaTime, Vector3.zero, Quaternion.identity, DeliveryVocabulary.BendingArm);
    }

    // Gestures change goals, never link lengths
    void AssertLengths(LianaPose pose, float length)
    {
        for (int i = 0; i < pose.segmentCount; i++)
        {
            float link = Vector3.Distance(pose.Joint(i), pose.Joint(i + 1));
            Assert.That(link, Is.EqualTo(length).Within(0.00001));
        }
    }

    [Test]
    public void Contact_Heal_SkipsAnticipationAndReturnsToExactRestPose()
    {
        _pose.Begin(1, GestureKind.Heal, Vector3.one);
        _pose.Contact(1, Vector3.one);
        Tick(0.001f);
        Assert.Less(Vector3.Distance(_pose.tip, Vector3.one), 0.001f);
        AssertLengths(_pose, _recipe.arms[0].segmentLength);
        for (int i = 0; i < 30; i++)
        {
            Tick(0.01f);
            AssertLengths(_pose, _recipe.arms[0].segmentLength);
        }

        Assert.AreEqual(GesturePhase.Rest, _pose.phase);
        for (int i = 0; i <= _pose.segmentCount; i++)
        {
            Assert.AreEqual(_recipe.arms[0].restJoints[i], _pose.Joint(i));
        }
    }

    [Test]
    public void End_StaleOrRepeatedToken_DoesNotRetractNewGesture()
    {
        _pose.Begin(1, GestureKind.Attack, Vector3.one);
        _pose.Begin(2, GestureKind.Attack, Vector3.up);
        _pose.End(1);
        _pose.End(1);
        Assert.AreEqual(GesturePhase.Extend, _pose.phase);
        Tick(0.02f);
        _pose.End(2);
        _pose.End(2);
        Assert.AreEqual(GesturePhase.Retract, _pose.phase);
        Tick(0.1f);
        AssertLengths(_pose, _recipe.arms[0].segmentLength);
        _pose.End(2);
        Tick(0.11f);
        Assert.AreEqual(GesturePhase.Rest, _pose.phase);
    }

    [Test]
    public void SetTipGoal_ProjectileGoal_TipFollowsIt()
    {
        _pose.Begin(1, GestureKind.Attack, Vector3.one);
        _pose.SetTipGoal(1, Vector3.up * 2f);

        Tick(0.001f);

        Assert.Less(Vector3.Distance(_pose.tip, Vector3.up * 2f), 0.001f);
        AssertLengths(_pose, _recipe.arms[0].segmentLength);
    }

    [Test]
    public void End_RightAfterContact_StillShowsContact()
    {
        _pose.Begin(1, GestureKind.Attack, Vector3.one);
        _pose.Contact(1, Vector3.one);
        _pose.End(1);

        Tick(0.016f);

        Assert.AreEqual(GesturePhase.Contact, _pose.phase);
        Assert.Less(Vector3.Distance(_pose.tip, Vector3.one), 0.001f);
    }

    [TestCase(DeliveryStyle.Arc)]
    [TestCase(DeliveryStyle.Rigid)]
    [TestCase(DeliveryStyle.Bounce)]
    public void Tick_DeliveryProfile_FollowsLiveEndpoint(DeliveryStyle style)
    {
        // Which styles draw as a rod is the vocabulary's arm entry
        ArmStyle arm = RenderTestAssets.LoadDeliveryVocabulary().GetArm(style);
        _pose.style = style;
        _pose.isDeliveryProfile = true;
        _pose.Begin(1, GestureKind.Attack, Vector3.right * 2f);
        _pose.SetTipGoal(1, Vector3.right * 2f);
        _pose.Tick(0.016f, Vector3.zero, Quaternion.identity, arm);
        Assert.That(Vector3.Distance(_pose.tip, Vector3.right * 2f), Is.LessThan(0.001f));
        if (style == DeliveryStyle.Arc)
        {
            Assert.Greater(_pose.Joint(_pose.segmentCount / 2).y, 0.1f);
        }
        else
        {
            Assert.AreEqual(0, _pose.Joint(_pose.segmentCount / 2).y);
        }

        _pose.Contact(1, Vector3.right * 2f);
        _pose.SetTipGoal(1, Vector3.forward);
        _pose.Tick(0.016f, Vector3.zero, Quaternion.identity, arm);

        Assert.That(Vector3.Distance(_pose.tip, Vector3.forward), Is.LessThan(0.001f));
    }

    [Test]
    public void Contact_HealerArm_ReachesAcrossBoardAndStopsShortOutsideIt()
    {
        CreatureRecipe healer = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");
        LianaPose pose = CreatePose(healer.arms[0]);
        Vector3 target = new Vector3(15f, 4f, 15f);
        pose.Begin(1, GestureKind.Attack, target);
        pose.Contact(1, target);
        pose.Tick(0.016f, Vector3.zero, Quaternion.identity, DeliveryVocabulary.BendingArm);
        Assert.Less(Vector3.Distance(pose.tip, target), 0.001f);

        Vector3 far = Vector3.right * 100f;
        pose.Contact(1, far);
        pose.Tick(0.016f, Vector3.zero, Quaternion.identity, DeliveryVocabulary.BendingArm);

        Assert.Greater(Vector3.Distance(pose.tip, far), 1f); // the chain stretches toward it and stops
        AssertLengths(pose, healer.arms[0].segmentLength);
    }
}

}
