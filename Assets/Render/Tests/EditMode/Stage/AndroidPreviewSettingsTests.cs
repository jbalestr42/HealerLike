using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class AndroidPreviewSettingsTests
{
    [Test]
    public void Dispose_RepeatedAndNestedScopes_LeaveUnityCachedSettingsUsable()
    {
        MethodInfo getter = typeof(PlayerSettings).GetMethod("GetSerializedObject",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(getter, Is.Not.Null);
        SerializedObject cached = (SerializedObject)getter.Invoke(null, null);
        UnityEngine.Object target = cached.targetObject;
        cached.Update();
        int input = cached.FindProperty("activeInputHandler").intValue;

        using (AndroidPreviewSettings outer = new AndroidPreviewSettings())
        {
            using (AndroidPreviewSettings inner = new AndroidPreviewSettings())
            {
                Assert.AreEqual(input, inner.inputHandler);
            }

            cached.Update();
            Assert.AreSame(target, cached.targetObject);
            Assert.AreEqual(input, outer.inputHandler);
        }

        using (AndroidPreviewSettings repeated = new AndroidPreviewSettings())
        {
            Assert.AreEqual(input, repeated.inputHandler);
        }

        Assert.AreSame(cached, getter.Invoke(null, null));
        cached.Update();
        Assert.AreSame(target, cached.targetObject);
        Assert.AreEqual(input, cached.FindProperty("activeInputHandler").intValue);
    }

    [Test]
    public void Dispose_FailedBuild_RestoresIdentityAndInputConfiguration()
    {
        string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string product = PlayerSettings.productName;
        string version = PlayerSettings.bundleVersion;
        int versionCode = PlayerSettings.Android.bundleVersionCode;
        UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
        int input;
        using (AndroidPreviewSettings original = new AndroidPreviewSettings())
        {
            input = original.inputHandler;
        }

        Assert.Throws<InvalidOperationException>(() =>
        {
            using (AndroidPreviewSettings settings = new AndroidPreviewSettings())
            {
                settings.Apply();
                Assert.AreEqual(0, settings.inputHandler);
                Assert.AreEqual(StagePreviewBuild.AndroidVersion, PlayerSettings.bundleVersion);
                throw new InvalidOperationException("Simulated build failure");
            }
        });

        Assert.AreEqual(identifier, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        Assert.AreEqual(product, PlayerSettings.productName);
        Assert.AreEqual(version, PlayerSettings.bundleVersion);
        Assert.AreEqual(versionCode, PlayerSettings.Android.bundleVersionCode);
        Assert.AreEqual(orientation, PlayerSettings.defaultInterfaceOrientation);
        using (AndroidPreviewSettings restored = new AndroidPreviewSettings())
        {
            Assert.AreEqual(input, restored.inputHandler);
        }
    }
}

}
