using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Look
{

public class LookShaderPropertiesTests
{
    readonly Dictionary<int, float> _floats = new Dictionary<int, float>();
    readonly Dictionary<int, Vector4> _vectors = new Dictionary<int, Vector4>();
    static readonly int appliedId = Shader.PropertyToID("_HLLookApplied");

    static FieldInfo[] Fields()
    {
        return typeof(LookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance);
    }

    static string Name(FieldInfo field)
    {
        return "_HL" + char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1);
    }

    [SetUp]
    public void SetUp()
    {
        foreach (FieldInfo field in Fields())
        {
            int id = Shader.PropertyToID(Name(field));
            if (field.FieldType == typeof(Color))
            {
                _vectors[id] = Shader.GetGlobalVector(id);
            }
            else
            {
                _floats[id] = Shader.GetGlobalFloat(id);
            }
        }
        _floats[appliedId] = Shader.GetGlobalFloat(appliedId);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (KeyValuePair<int, float> pair in _floats)
        {
            Shader.SetGlobalFloat(pair.Key, pair.Value);
        }
        foreach (KeyValuePair<int, Vector4> pair in _vectors)
        {
            Shader.SetGlobalVector(pair.Key, pair.Value);
        }
        _floats.Clear();
        _vectors.Clear();
    }

    [Test]
    public void SettingsFields_MatchEveryLookGlobalInTheShaderContract()
    {
        List<string> expected = new List<string> { "_HLLookApplied" };
        foreach (FieldInfo field in Fields())
        {
            expected.Add(Name(field));
        }
        string source = File.ReadAllText(Path.Combine(Application.dataPath, "Render/Shaders/LookCore.hlsl"));
        List<string> actual = new List<string>();
        foreach (Match match in Regex.Matches(source, @"(?m)^float[4]?\s+(_HL\w+)\s*;"))
        {
            string name = match.Groups[1].Value;
            // Board geometry is owned by the Stage grid contract, independently of the surface look.
            if (!name.StartsWith("_HLGrid", StringComparison.Ordinal))
            {
                actual.Add(name);
            }
        }

        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestCase(ColorSpace.Gamma)]
    [TestCase(ColorSpace.Linear)]
    public void Publish_EverySetting_UsesItsValueAndExplicitWorkingColor(ColorSpace space)
    {
        SetRaw(900f);
        object boxed = LookSettings.Default;
        int index = 0;
        foreach (FieldInfo field in Fields())
        {
            if (field.FieldType == typeof(Color))
            {
                field.SetValue(boxed, new Color(0.2f, 0.5f, 0.8f, 0.3f));
            }
            else if (field.FieldType == typeof(int))
            {
                field.SetValue(boxed, 37);
            }
            else
            {
                field.SetValue(boxed, ++index + 0.25f);
            }
        }
        LookSettings settings = (LookSettings)boxed;

        LookShaderProperties.Publish(settings, space);

        foreach (FieldInfo field in Fields())
        {
            string name = Name(field);
            if (field.FieldType == typeof(Color))
            {
                Color expected = (Color)field.GetValue(settings);
                if (space == ColorSpace.Linear)
                {
                    expected = expected.linear;
                }
                Vector4 color = new Vector4(expected.r, expected.g, expected.b, 1f);
                Assert.AreEqual(color, Shader.GetGlobalVector(name), name);
            }
            else
            {
                Assert.AreEqual(Convert.ToSingle(field.GetValue(settings)), Shader.GetGlobalFloat(name), name);
            }
        }
        Assert.AreEqual(1f, Shader.GetGlobalFloat(appliedId));
    }

    [Test]
    public void Capture_ThenRestore_RestoresEveryRawValueIncludingAlphaAndAppliedFlag()
    {
        SetRaw(100f);
        LookShaderProperties.Snapshot previous = LookShaderProperties.Capture();
        SetRaw(900f);

        previous.Restore();

        int index = 0;
        foreach (FieldInfo field in Fields())
        {
            float value = 100f + index++;
            string name = Name(field);
            if (field.FieldType == typeof(Color))
            {
                Assert.AreEqual(new Vector4(value, value + 1f, value + 2f, 0.37f), Shader.GetGlobalVector(name), name);
            }
            else
            {
                Assert.AreEqual(value, Shader.GetGlobalFloat(name), name);
            }
        }
        Assert.AreEqual(0.25f, Shader.GetGlobalFloat(appliedId));
    }

    static void SetRaw(float start)
    {
        int index = 0;
        foreach (FieldInfo field in Fields())
        {
            float value = start + index++;
            string name = Name(field);
            if (field.FieldType == typeof(Color))
            {
                Shader.SetGlobalVector(name, new Vector4(value, value + 1f, value + 2f, 0.37f));
            }
            else
            {
                Shader.SetGlobalFloat(name, value);
            }
        }
        Shader.SetGlobalFloat(appliedId, 0.25f);
    }
}

}
