using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{

public class DeliveryVocabularyTests
{
    static readonly string vocabularyPath = "Assets/Render/Deliveries/Data/Resources/DeliveryVocabulary.asset";

    readonly List<Object> _objects = new List<Object>();

    public static DeliveryVocabulary Vocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<DeliveryVocabulary>(vocabularyPath);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    [Test]
    public void Load_Resources_ReturnsTheAuthoredAsset()
    {
        DeliveryVocabulary vocabulary = DeliveryVocabulary.Load();

        Assert.AreSame(Vocabulary(), vocabulary);
        Assert.IsNotNull(vocabulary.palette);
        Assert.IsNotNull(vocabulary.meshes);
        Assert.IsNotNull(vocabulary.material);
    }

    [TestCase(DeliveryStyle.Direct, new[] { Primitive.Sphere })]
    [TestCase(DeliveryStyle.Bounce, new[] { Primitive.Sphere })]
    [TestCase(DeliveryStyle.ChainSync, new[] { Primitive.Sphere })]
    [TestCase(DeliveryStyle.Rigid, new[] { Primitive.Cone })]
    [TestCase(DeliveryStyle.Arc, new[] { Primitive.CylinderSegment, Primitive.Sphere })]
    [TestCase(DeliveryStyle.Swarm, new[] { Primitive.Sphere, Primitive.Sphere, Primitive.Sphere })]
    [TestCase(DeliveryStyle.Thrown, new Primitive[0])]
    public void GetTip_Style_FollowsTheHeadOfTheSameDelivery(DeliveryStyle style, Primitive[] expected)
    {
        LookPart[] tip = Vocabulary().GetTip(style);

        Assert.AreEqual(expected.Length, tip.Length);
        for (int i = 0; i < tip.Length; i++)
        {
            Assert.AreEqual(expected[i], tip[i].primitive);
        }
    }

    [Test]
    public void GetTip_RigidSpike_PointsAlongTheTravel()
    {
        LookPart spike = Vocabulary().GetTip(DeliveryStyle.Rigid)[0];

        Vector3 axis = Quaternion.Euler(spike.euler) * Vector3.up;

        Assert.That(Vector3.Dot(axis, Vector3.forward), Is.GreaterThan(0.99f));
        Assert.Greater(spike.size.y, spike.size.x); // narrow
    }

    [Test]
    public void GetTip_ChainSync_IsBrighterThanTheBead()
    {
        DeliveryVocabulary vocabulary = Vocabulary();

        LookPart spark = vocabulary.GetTip(DeliveryStyle.ChainSync)[0];
        LookPart bead = vocabulary.GetTip(DeliveryStyle.Direct)[0];

        Assert.Greater(spark.glow, bead.glow);
    }

    [Test]
    public void GetTip_EveryAccentPart_IsATip()
    {
        DeliveryVocabulary vocabulary = Vocabulary();

        foreach (LookPart[] tip in vocabulary.tips.Values)
        {
            foreach (LookPart part in tip)
            {
                if (part.colour == ColourRole.Accent)
                {
                    Assert.AreEqual(PartRole.Tip, part.role, part.id);
                }
            }
        }
    }

    [Test]
    public void GetTip_MissingStyle_ReturnsEmpty()
    {
        DeliveryVocabulary vocabulary = ScriptableObject.CreateInstance<DeliveryVocabulary>();
        _objects.Add(vocabulary);

        LookPart[] tip = vocabulary.GetTip(DeliveryStyle.Direct);

        Assert.AreEqual(0, tip.Length);
    }
}

}
