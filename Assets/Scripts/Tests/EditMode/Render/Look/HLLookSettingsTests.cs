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
            var defaults = HLLookSettings.Default;
            Assert.That(defaults.Validated(), Is.EqualTo(defaults));
            foreach (var field in typeof(HLLookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                string macro = "HL_DEF_" + field.Name.ToUpperInvariant();
                if (field.FieldType == typeof(Color))
                {
                    var match = Regex.Match(core, macro + @"\s+float4\(HLWorkingColor\(float3\((\d+),(\d+),(\d+)\)/255.0\),1\)");
                    Assert.That(match.Success, Is.True, macro);
                    Color color = (Color)field.GetValue(defaults);
                    Assert.That(color.r, Is.EqualTo(int.Parse(match.Groups[1].Value) / 255f));
                    Assert.That(color.g, Is.EqualTo(int.Parse(match.Groups[2].Value) / 255f));
                    Assert.That(color.b, Is.EqualTo(int.Parse(match.Groups[3].Value) / 255f));
                    Assert.That(color.a, Is.EqualTo(1f));
                }
                else
                {
                    var match = Regex.Match(core, macro + @"\s+([0-9.]+)");
                    Assert.That(match.Success, Is.True, macro);
                    Assert.That(Convert.ToSingle(field.GetValue(defaults)), Is.EqualTo(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)), macro);
                }
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonfiniteScalarsAndColorChannelsUseTheirDefaults(float invalid)
        {
            foreach (var field in typeof(HLLookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object value = HLLookSettings.Default;
                if (field.FieldType == typeof(float)) field.SetValue(value, invalid);
                else if (field.FieldType == typeof(Color)) field.SetValue(value, new Color(invalid, invalid, invalid, invalid));
                else continue;
                Assert.That(((HLLookSettings)value).Validated(), Is.EqualTo(HLLookSettings.Default), field.Name);
            }
        }

        [Test]
        public void DegenerateRangesClampWithoutMutatingInput()
        {
            var input = new HLLookSettings
            {
                FogStart = 30f, FogEnd = -1f, FogBands = -2, OutlineWidthPixels = -1f,
                ShadowStrength = -1f, ToonThreshold = 2f, InkStrength = 2f,
                InkScale = -1f, InkWidth = -1f, InkStart = 2f, InkRange = -1f,
                DensityMul = 2f, InkWarp = -1f, InkWarpFreq = -1f,
                DashAmount = 2f, DashScale = -1f, InkDistStart = -1f, InkFarSpacing = -1f
            };
            var value = input.Validated();
            Assert.That(input.FogEnd, Is.EqualTo(-1f));
            Assert.That(value.FogStart, Is.EqualTo(30f));
            Assert.That((double)value.FogEnd - value.FogStart, Is.GreaterThanOrEqualTo(.001));
            Assert.That(value.FogBands, Is.EqualTo(1));
            Assert.That(value.OutlineWidthPixels, Is.Zero);
            Assert.That(value.ShadowStrength, Is.EqualTo(.01f));
            Assert.That(value.ToonThreshold, Is.EqualTo(.999f));
            Assert.That(value.InkStrength, Is.EqualTo(1f));
            Assert.That(value.InkScale, Is.EqualTo(.0001f));
            Assert.That(value.InkWidth, Is.Zero);
            Assert.That(value.InkStart, Is.EqualTo(1f));
            Assert.That(value.InkRange, Is.EqualTo(.001f));
            Assert.That(value.DensityMul, Is.EqualTo(1f));
            Assert.That(value.InkWarp, Is.Zero);
            Assert.That(value.InkWarpFreq, Is.Zero);
            Assert.That(value.DashAmount, Is.EqualTo(.92f));
            Assert.That(value.DashScale, Is.EqualTo(.001f));
            Assert.That(value.InkDistStart, Is.EqualTo(.001f));
            Assert.That(value.InkFarSpacing, Is.Zero);
            Assert.That(value.Validated(), Is.EqualTo(value));
        }

        [Test]
        public void PixelDensitySupportsLegacyZeroAndClampsUnsafeValues()
        {
            var settings = HLLookSettings.Default;
            Assert.That(settings.InkSpacingPixels, Is.EqualTo(3.5f));
            settings.InkSpacingPixels = -1;
            Assert.That(settings.Validated().InkSpacingPixels, Is.Zero);
            settings.InkSpacingPixels = 100;
            Assert.That(settings.Validated().InkSpacingPixels, Is.EqualTo(16));
            settings.InkSpacingPixels = 0;
            Assert.That(settings.Validated().InkSpacingPixels, Is.Zero);
        }

        [TestCase(0f)]
        [TestCase(1000000f)]
        [TestCase(float.MaxValue)]
        public void ExtremeFogRangesRemainFiniteAndSeparated(float start)
        {
            var settings = HLLookSettings.Default;
            settings.FogStart = start;
            settings.FogEnd = start;
            var value = settings.Validated();
            Assert.That(float.IsInfinity(value.FogEnd), Is.False);
            Assert.That((double)value.FogEnd - value.FogStart, Is.GreaterThanOrEqualTo(.001));
        }

        [Test]
        public void ColorValidationKeepsInkAndTintNonblackButAllowsBlackFog()
        {
            var settings = HLLookSettings.Default;
            settings.ShadowTint = new Color(-1, -2, -3, 0);
            settings.OutlineColor = Color.clear;
            settings.FogColor = new Color(-1, 2, .4f, float.NaN);
            var value = settings.Validated();
            Assert.That(value.ShadowTint, Is.EqualTo(HLLookSettings.Default.ShadowTint));
            Assert.That(value.OutlineColor, Is.EqualTo(HLLookSettings.Default.OutlineColor));
            Assert.That(value.FogColor, Is.EqualTo(new Color(0, 1, .4f, 1)));
            settings.FogColor = Color.clear;
            Assert.That(settings.Validated().FogColor, Is.EqualTo(Color.black));
        }
    }
}
