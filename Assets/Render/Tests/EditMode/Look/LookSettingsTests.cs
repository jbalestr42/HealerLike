using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Look
{

public class LookSettingsTests
{
    [Test]
    public void Default_EveryField_MatchesShaderFallback()
    {
        string core = File.ReadAllText(Path.Combine(Application.dataPath, "Render/Shaders/LookCore.hlsl"));
        LookSettings defaults = LookSettings.Default;

        Assert.That(defaults.Validated(), Is.EqualTo(defaults));
        foreach (FieldInfo field in typeof(LookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
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

        // And every define names a field, so a define left behind by a removed setting fails too
        foreach (Match define in Regex.Matches(core, @"#define\s+HL_DEF_(\w+)"))
        {
            FieldInfo field = typeof(LookSettings).GetField(define.Groups[1].Value,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            Assert.That(field, Is.Not.Null, define.Value);
        }
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void Validated_NonfiniteField_UsesItsDefault(float invalid)
    {
        foreach (FieldInfo field in typeof(LookSettings).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            object value = LookSettings.Default;
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

            Assert.That(((LookSettings)value).Validated(), Is.EqualTo(LookSettings.Default), field.Name);
        }
    }

    [Test]
    public void Default_ShadowTintAndFog_AreTheReferenceColours()
    {
        LookSettings defaults = LookSettings.Default;

        Assert.That((Color32)defaults.shadowTint, Is.EqualTo(new Color32(30, 87, 125, 255))); // #1e577d, plant shade
        Assert.That((Color32)defaults.fogColor, Is.EqualTo(new Color32(154, 188, 211, 255))); // #9abcd3, far ridge
    }

    [Test]
    public void Validated_DegenerateRanges_ClampWithoutMutatingInput()
    {
        LookSettings input = new LookSettings
        {
            fogStart = 30f,
            fogEnd = -1f,
            fogBands = -2,
            outlineWidthPixels = -1f,
            shadowStrength = -1f,
            toonThreshold = 2f,
            toonSoftness = -1f,
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
            inkFarSpacing = -1f,
            contrast = -1f
        };

        LookSettings value = input.Validated();

        Assert.That(input.fogEnd, Is.EqualTo(-1f));
        Assert.That(value.fogStart, Is.EqualTo(30f));
        Assert.That(value.fogEnd, Is.GreaterThan(value.fogStart));
        Assert.That(value.fogBands, Is.EqualTo(1));
        Assert.That(value.outlineWidthPixels, Is.Zero);
        Assert.That(value.shadowStrength, Is.EqualTo(0.01f));
        Assert.That(value.toonThreshold, Is.EqualTo(0.999f));
        Assert.That(value.toonSoftness, Is.Zero);
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
        Assert.That(value.contrast, Is.EqualTo(1f));
        Assert.That(value.Validated(), Is.EqualTo(value));
    }

    [Test]
    public void Validated_LargeSoftnessAndContrast_ClampToTheirMaximum()
    {
        LookSettings settings = LookSettings.Default;
        settings.toonSoftness = 2f;
        settings.contrast = 5f;

        LookSettings value = settings.Validated();

        Assert.That(value.toonSoftness, Is.EqualTo(0.5f));
        Assert.That(value.contrast, Is.EqualTo(1.6f));
    }

    [Test]
    public void Validated_EqualFogEnds_MovesTheEndPastTheStart()
    {
        LookSettings settings = LookSettings.Default;
        settings.fogStart = 20f;
        settings.fogEnd = 20f;

        LookSettings value = settings.Validated();

        Assert.That(value.fogEnd, Is.GreaterThan(value.fogStart));
    }

    [Test]
    public void Validated_BlackColours_KeepInkAndTintButAllowBlackFog()
    {
        LookSettings settings = LookSettings.Default;
        settings.shadowTint = new Color(-1f, -2f, -3f, 0f);
        settings.outlineColor = Color.clear;
        settings.fogColor = new Color(-1f, 2f, 0.4f, float.NaN);

        LookSettings value = settings.Validated();

        Assert.That(value.shadowTint, Is.EqualTo(LookSettings.Default.shadowTint));
        Assert.That(value.outlineColor, Is.EqualTo(LookSettings.Default.outlineColor));
        Assert.That(value.fogColor, Is.EqualTo(new Color(0f, 1f, 0.4f, 1f)));

        settings.fogColor = Color.clear;
        Assert.That(settings.Validated().fogColor, Is.EqualTo(Color.black));
    }
}

}
