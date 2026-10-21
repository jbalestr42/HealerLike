using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class FieldTreatmentTests
    {
        static readonly Color committed = new Color32(55, 191, 104, 255);
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_material);
        }

        static Vector3 Hsv(Color colour)
        {
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            return new Vector3(h, s, v);
        }

        [Test]
        public void Control_KeepsTheCommittedColour()
        {
            Assert.That(Vector4.Distance(FieldTreatment.Control().Treat(committed), committed), Is.LessThan(1e-5f));
        }

        [Test]
        public void Sheet_IsTheSixTreatmentsInOrder()
        {
            string[] names = System.Array.ConvertAll(FieldTreatment.Sheet(1f), t => t.name);

            CollectionAssert.AreEqual(new[] { "control", "value-only", "saturation-and-value", "hatch-contrast",
                "hue-separation", "combination" }, names);
        }

        [Test]
        public void ValueOnly_DarkensByAThirdAndKeepsHueAndSaturation()
        {
            Vector3 before = Hsv(committed), after = Hsv(FieldTreatment.Sheet(1f)[1].Treat(committed));

            Assert.AreEqual(before.x, after.x, 1e-4f);
            Assert.AreEqual(before.y, after.y, 1e-4f);
            Assert.AreEqual(before.z * 0.65f, after.z, 1e-4f);
        }

        [Test]
        public void SaturationAndValue_LowersBoth()
        {
            Vector3 before = Hsv(committed), after = Hsv(FieldTreatment.Sheet(1f)[2].Treat(committed));

            Assert.AreEqual(before.x, after.x, 1e-4f);
            Assert.AreEqual(before.y * 0.6f, after.y, 1e-4f);
            Assert.AreEqual(before.z * 0.7f, after.z, 1e-4f);
        }

        [Test]
        public void HueSeparation_MovesTowardTealAndDarkens()
        {
            Vector3 before = Hsv(committed), after = Hsv(FieldTreatment.Sheet(1f)[4].Treat(committed));

            Assert.Greater(after.x, before.x, "Teal sits past the committed green on the hue wheel");
            Assert.Less(after.z, before.z);
        }

        [Test]
        public void Combination_AtZero_IsTheControl_AtOne_IsTreatmentsOneTwoThree()
        {
            FieldTreatment none = FieldTreatment.Combination(0f), full = FieldTreatment.Combination(1f);

            Assert.AreEqual(1f, none.valueScale);
            Assert.AreEqual(1f, none.saturationScale);
            Assert.AreEqual(1f, none.contrastScale);
            Assert.AreEqual(0.65f, full.valueScale, 1e-6f);
            Assert.AreEqual(0.6f, full.saturationScale, 1e-6f);
            Assert.AreEqual(0.5f, full.contrastScale, 1e-6f);
        }

        [Test]
        public void HatchContrast_HalvesShadeAndInkThenRestoresExactly()
        {
            float hatch = _material.GetFloat("_HLHatchMultiplier");
            Color shade = _material.GetColor("_HLShadeTint"), turn = _material.GetColor("_HLShadeTurnTint");
            Color baseColor = _material.GetColor("_BaseColor");

            FieldTreatment.Snapshot saved = FieldTreatment.Sheet(1f)[3].Apply(_material);

            Assert.AreEqual(hatch * 0.5f, _material.GetFloat("_HLHatchMultiplier"), 1e-6f);
            Assert.AreEqual(shade.a * 0.5f, _material.GetColor("_HLShadeTint").a, 1e-6f);
            Assert.AreEqual(turn.a * 0.5f, _material.GetColor("_HLShadeTurnTint").a, 1e-6f);
            Assert.That(Vector4.Distance(_material.GetColor("_BaseColor"), baseColor), Is.LessThan(1e-5f),
                "The hatch treatment keeps the committed colour");

            saved.Restore(_material);

            Assert.AreEqual(hatch, _material.GetFloat("_HLHatchMultiplier"));
            Assert.AreEqual(shade, _material.GetColor("_HLShadeTint"));
            Assert.AreEqual(turn, _material.GetColor("_HLShadeTurnTint"));
            Assert.AreEqual(baseColor, _material.GetColor("_BaseColor"));
        }
    }
}
