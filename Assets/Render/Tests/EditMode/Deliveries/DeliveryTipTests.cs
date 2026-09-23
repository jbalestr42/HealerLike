using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{

public class DeliveryTipTests
{
    static DeliveryTip CreateTip(DeliveryStyle style, bool hasVocabulary = true)
    {
        DeliveryTip tip = new DeliveryTip();
        DeliveryVocabulary vocabulary = hasVocabulary ? DeliveryVocabularyTests.Vocabulary() : null;
        tip.SetStyle(style, vocabulary, PrimitiveMeshesTests.Meshes());
        return tip;
    }

    [Test]
    public void SetStyle_NoVocabulary_KeepsOneBead()
    {
        DeliveryTip tip = CreateTip(DeliveryStyle.Swarm, hasVocabulary: false);

        Assert.AreEqual(1, tip.partCount);
        Assert.AreEqual(Primitive.Sphere, tip.Part(0).primitive);
        Assert.AreEqual(DeliveryStyle.Swarm, tip.style);
    }

    [Test]
    public void SetStyle_NoMeshes_DrawsNothing()
    {
        DeliveryTip tip = new DeliveryTip();

        tip.SetStyle(DeliveryStyle.Direct, DeliveryVocabularyTests.Vocabulary(), null);

        Assert.AreEqual(0, tip.partCount);
    }

    [Test]
    public void PartColour_Roles_AccentTakesTipAndStemTakesArm()
    {
        DeliveryTip tip = CreateTip(DeliveryStyle.Arc);
        Color accent = Color.red;
        Color stem = Color.green;

        Color stalk = tip.PartColour(0, accent, stem);
        Color pod = tip.PartColour(1, accent, stem);

        Assert.AreEqual(stem, stalk);
        Assert.AreEqual(accent, pod);
    }

    [Test]
    public void PartColour_Glow_BrightensTheAccent()
    {
        DeliveryTip tip = CreateTip(DeliveryStyle.ChainSync);
        Color accent = new Color(0.5f, 0.25f, 0.1f, 1f);

        Color spark = tip.PartColour(0, accent, Color.green);

        Assert.Greater(spark.r, accent.r);
        Assert.AreEqual(accent.r / accent.g, spark.r / spark.g, 0.0001f); // same hue
    }

    [Test]
    public void Frame_Travel_LooksAlongIt()
    {
        Matrix4x4 frame = DeliveryTip.Frame(Vector3.one, Vector3.right * 3f, 0.5f);

        Vector3 forward = frame.MultiplyVector(Vector3.forward).normalized;

        Assert.That(Vector3.Dot(forward, Vector3.right), Is.GreaterThan(0.999f));
        Assert.AreEqual(Vector3.one, (Vector3)frame.GetColumn(3));
        Assert.AreEqual(0.5f, frame.lossyScale.x, 0.0001f);
    }

    [Test]
    public void PartMatrix_RigidSpike_ApexLeadsTheTravel()
    {
        DeliveryTip tip = CreateTip(DeliveryStyle.Rigid);
        Matrix4x4 frame = DeliveryTip.Frame(Vector3.zero, Vector3.right, 1f);

        Vector3 apex = tip.PartMatrix(frame, 0).MultiplyPoint3x4(Vector3.up * 0.5f);

        Assert.Greater(apex.x, 0.9f);
    }
}

}
