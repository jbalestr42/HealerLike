using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class EffectVocabularyTests
{
    [Test]
    public void GetEntry_ShippedVocabulary_HasAnEntryWithShapesForEveryElement()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        Assert.IsNotNull(vocabulary.palette);
        foreach (EffectElement element in System.Enum.GetValues(typeof(EffectElement)))
        {
            ElementEntry entry = vocabulary.GetEntry(element);

            Assert.IsNotNull(entry, element.ToString());
            Assert.Greater(EffectComposer.Shapes(entry), 0, element.ToString());
            Assert.Greater(entry.cycleSeconds, 0f, element.ToString());
        }
    }

    [Test]
    public void GetEntry_ShippedPress_FourOrFiveConesPointingDown()
    {
        ElementEntry press = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Press);

        Assert.AreEqual(EffectSocket.AboveHead, press.socket);
        Assert.AreEqual(5, press.parts.Length);
        Assert.AreEqual(4, press.minCount);
        foreach (LookPart cone in press.parts)
        {
            Assert.AreEqual(Primitive.Cone, cone.primitive);
            Assert.Less((Quaternion.Euler(cone.euler) * Vector3.up).y, -0.99f);
        }
    }

    [Test]
    public void GetEntry_ShippedBoonAndBane_DrawDifferentPrimitives()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        Assert.AreEqual(Primitive.Torus, vocabulary.GetEntry(EffectElement.Orbit).parts[0].primitive);
        Assert.AreEqual(Primitive.Cone, vocabulary.GetEntry(EffectElement.Press).parts[0].primitive);
        Assert.AreNotEqual(vocabulary.GetEntry(EffectElement.Plates).parts[0].primitive,
                           vocabulary.GetEntry(EffectElement.Crack).parts[0].primitive);
    }

    [Test]
    public void GetEntry_ShippedOrbit_ToriAtTheOrbitRadiusTiltedTenToTwenty()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(SpellSinkFixture.MeshesPath);
        Mesh torus = meshes.GetMesh(Primitive.Torus);

        foreach (LookPart ring in vocabulary.GetEntry(EffectElement.Orbit).parts)
        {
            float radius = ring.size.x * torus.bounds.extents.x;
            float tilt = Vector3.Angle(Vector3.up, Quaternion.Euler(ring.euler) * Vector3.up);

            Assert.GreaterOrEqual(radius, 1.35f - 0.001f);
            Assert.That(tilt, Is.InRange(9.9f, 20.1f));
        }
    }

    [Test]
    public void GetEntry_ShippedRise_EightSpheresFromThree()
    {
        ElementEntry rise = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Rise);

        Assert.AreEqual(8, EffectComposer.Shapes(rise));
        Assert.AreEqual(3, rise.minCount);
        Assert.AreEqual(EffectCount.Amount, rise.count);
    }

    [Test]
    public void Elements_ShippedParts_AreBakedPrimitivesOnly()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(SpellSinkFixture.MeshesPath);

        foreach (ElementEntry entry in RenderTestAssets.LoadEffectVocabulary().elements.Values)
        {
            foreach (LookPart part in entry.parts)
            {
                Assert.IsTrue(EditorUtility.IsPersistent(meshes.GetMesh(part.primitive, 0)));
                Assert.Greater(part.size.sqrMagnitude, 0f);
            }
        }
    }
}

}
