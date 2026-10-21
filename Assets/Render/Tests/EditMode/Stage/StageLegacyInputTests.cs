using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StageLegacyInputTests
    {
        [Test]
        public void Configure_ReplacesCompetingModule_AndReusesLegacyModule()
        {
            GameObject host = new GameObject("Input backend fixture");
            try
            {
                EventSystem system = host.AddComponent<EventSystem>();
                FixtureInputModule previous = host.AddComponent<FixtureInputModule>();
                StandaloneInputModule legacy = StageLegacyInput.Configure(system);
                Assert.That(previous.enabled, Is.False);
                Assert.That(legacy.enabled, Is.True);
                Assert.That(StageLegacyInput.Configure(system), Is.SameAs(legacy));
                Assert.That(host.GetComponents<StandaloneInputModule>().Length, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Configure_ExistingDisabledSystem_KeepsItsEnabledState()
        {
            GameObject host = new GameObject("Disabled input backend fixture");
            try
            {
                EventSystem system = host.AddComponent<EventSystem>();
                system.enabled = false;
                StandaloneInputModule legacy = host.AddComponent<StandaloneInputModule>();
                BaseInput source = host.AddComponent<BaseInput>();
                legacy.inputOverride = source;
                legacy.enabled = false;
                Assert.That(StageLegacyInput.Configure(system), Is.SameAs(legacy));
                Assert.That(system.enabled, Is.False);
                Assert.That(legacy.inputOverride, Is.SameAs(source));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ConfigureScene_LeavesOtherScenesInputModulesUntouched()
        {
            Scene original = SceneManager.GetActiveScene();
            Scene scene = default;
            Scene otherScene = default;
            GameObject host = null;
            GameObject otherHost = null;
            try
            {
                scene = EditorSceneManager.NewPreviewScene();
                otherScene = EditorSceneManager.NewPreviewScene();
                otherHost = new GameObject("Other scene input");
                SceneManager.MoveGameObjectToScene(otherHost, otherScene);
                otherHost.AddComponent<EventSystem>();
                FixtureInputModule other = otherHost.AddComponent<FixtureInputModule>();
                host = new GameObject("Scene scoped input");
                SceneManager.MoveGameObjectToScene(host, scene);
                host.AddComponent<EventSystem>();
                FixtureInputModule previous = host.AddComponent<FixtureInputModule>();
                Assert.That(StageLegacyInput.Configure(scene), Is.Not.Null);
                Assert.That(previous.enabled, Is.False);
                Assert.That(other.enabled, Is.True);
                Assert.That(otherHost.GetComponent<StandaloneInputModule>(), Is.Null);
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(original));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(otherHost);
                if (otherScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(otherScene);
                }
                if (scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        public class FixtureInputModule : BaseInputModule
        {
            public override void Process() { }
        }
    }
}
