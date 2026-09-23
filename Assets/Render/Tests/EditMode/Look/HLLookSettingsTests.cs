using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Look
{
    public class HLLookSettingsTests
    {
        [Test]
        public void DefaultsMatchEveryShaderFallback()
        {
            string core = File.ReadAllText(Path.Combine(Application.dataPath, "Render/Shaders/HLLookCore.hlsl"));
            HLLookSettings defaults = HLLookSettings.Default;

            Assert.That(defaults.Validated(), Is.EqualTo(defaults));
            foreach (FieldInfo field in typeof(HLLookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                string macro = "HL_DEF_" + field.Name.ToUpperInvariant();
                if (field.FieldType == typeof(Color))
                {
                    string pattern = macro + @"\s+float4\(HLWorkingColor\(float3\((\d+),(\d+),(\d+)\)/255.0\),1\)";
                    Match match = Regex.Match(core, pattern);
                    Assert.That(match.Success, Is.True, macro);
                    Color color = (Color)field.GetValue(defaults);
                    Assert.That(color.r, Is.EqualTo(int.Parse(match.Groups[1].Value) / 255f));
                    Assert.That(color.g, Is.EqualTo(int.Parse(match.Groups[2].Value) / 255f));
                    Assert.That(color.b, Is.EqualTo(int.Parse(match.Groups[3].Value) / 255f));
                    Assert.That(color.a, Is.EqualTo(1f));
                }
                else
                {
                    Match match = Regex.Match(core, macro + @"\s+([0-9.]+)");
                    Assert.That(match.Success, Is.True, macro);
                    float expected = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    Assert.That(Convert.ToSingle(field.GetValue(defaults)), Is.EqualTo(expected), macro);
                }
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonfiniteScalarsAndColorChannelsUseTheirDefaults(float invalid)
        {
            foreach (FieldInfo field in typeof(HLLookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object value = HLLookSettings.Default;
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(value, invalid);
                }
                else if (field.FieldType == typeof(Color))
                {
                    field.SetValue(value, new Color(invalid, invalid, invalid, invalid));
                }
                else
                {
                    continue;
                }

                Assert.That(((HLLookSettings)value).Validated(), Is.EqualTo(HLLookSettings.Default), field.Name);
            }
        }

        [Test]
        public void DegenerateRangesClampWithoutMutatingInput()
        {
            HLLookSettings input = new HLLookSettings
            {
                fogStart = 30f,
                fogEnd = -1f,
                fogBands = -2,
                outlineWidthPixels = -1f,
                shadowStrength = -1f,
                toonThreshold = 2f,
                inkStrength = 2f,
                inkScale = -1f,
                inkWidth = -1f,
                inkStart = 2f,
                inkRange = -1f,
                densityMul = 2f,
                inkWarp = -1f,
                inkWarpFreq = -1f,
                dashAmount = 2f,
                dashScale = -1f,
                inkDistStart = -1f,
                inkFarSpacing = -1f
            };

            HLLookSettings value = input.Validated();

            Assert.That(input.fogEnd, Is.EqualTo(-1f));
            Assert.That(value.fogStart, Is.EqualTo(30f));
            Assert.That((double)value.fogEnd - value.fogStart, Is.GreaterThanOrEqualTo(0.001));
            Assert.That(value.fogBands, Is.EqualTo(1));
            Assert.That(value.outlineWidthPixels, Is.Zero);
            Assert.That(value.shadowStrength, Is.EqualTo(0.01f));
            Assert.That(value.toonThreshold, Is.EqualTo(0.999f));
            Assert.That(value.inkStrength, Is.EqualTo(1f));
            Assert.That(value.inkScale, Is.EqualTo(0.0001f));
            Assert.That(value.inkWidth, Is.Zero);
            Assert.That(value.inkStart, Is.EqualTo(1f));
            Assert.That(value.inkRange, Is.EqualTo(0.001f));
            Assert.That(value.densityMul, Is.EqualTo(1f));
            Assert.That(value.inkWarp, Is.Zero);
            Assert.That(value.inkWarpFreq, Is.Zero);
            Assert.That(value.dashAmount, Is.EqualTo(0.92f));
            Assert.That(value.dashScale, Is.EqualTo(0.001f));
            Assert.That(value.inkDistStart, Is.EqualTo(0.001f));
            Assert.That(value.inkFarSpacing, Is.Zero);
            Assert.That(value.Validated(), Is.EqualTo(value));
        }

        [Test]
        public void PixelDensitySupportsLegacyZeroAndClampsUnsafeValues()
        {
            HLLookSettings settings = HLLookSettings.Default;
            Assert.That(settings.inkSpacingPixels, Is.EqualTo(3.5f));

            settings.inkSpacingPixels = -1f;
            Assert.That(settings.Validated().inkSpacingPixels, Is.Zero);

            settings.inkSpacingPixels = 100f;
            Assert.That(settings.Validated().inkSpacingPixels, Is.EqualTo(16));

            settings.inkSpacingPixels = 0f;
            Assert.That(settings.Validated().inkSpacingPixels, Is.Zero);
        }

        [TestCase(0f)]
        [TestCase(1000000f)]
        [TestCase(float.MaxValue)]
        public void ExtremeFogRangesRemainFiniteAndSeparated(float start)
        {
            HLLookSettings settings = HLLookSettings.Default;
            settings.fogStart = start;
            settings.fogEnd = start;

            HLLookSettings value = settings.Validated();

            Assert.That(float.IsInfinity(value.fogEnd), Is.False);
            Assert.That((double)value.fogEnd - value.fogStart, Is.GreaterThanOrEqualTo(0.001));
        }

        [Test]
        public void ColorValidationKeepsInkAndTintNonblackButAllowsBlackFog()
        {
            HLLookSettings settings = HLLookSettings.Default;
            settings.shadowTint = new Color(-1f, -2f, -3f, 0f);
            settings.outlineColor = Color.clear;
            settings.fogColor = new Color(-1f, 2f, 0.4f, float.NaN);

            HLLookSettings value = settings.Validated();

            Assert.That(value.shadowTint, Is.EqualTo(HLLookSettings.Default.shadowTint));
            Assert.That(value.outlineColor, Is.EqualTo(HLLookSettings.Default.outlineColor));
            Assert.That(value.fogColor, Is.EqualTo(new Color(0f, 1f, 0.4f, 1f)));

            settings.fogColor = Color.clear;
            Assert.That(settings.Validated().fogColor, Is.EqualTo(Color.black));
        }
    }
}
