using System.Collections.Generic;
using System.Reflection;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class VisibilityTestSpawner : AEnvironmentSpawner
    {
        public Transform Rebuild() => CreateRoot("Owned scenery");
    }

    public class EnvironmentVisibilityTests
    {
        [Test]
        public void SpawnerRebuildWhileDisabled_KeepsNewRootHiddenUntilEnabled()
        {
            GameObject owner = new GameObject("Spawner owner");
            try
            {
                VisibilityTestSpawner spawner = owner.AddComponent<VisibilityTestSpawner>();
                spawner.enabled = false;
                Transform root = spawner.Rebuild();
                Assert.IsFalse(root.gameObject.activeSelf);
                spawner.enabled = true;
                // The callback is private on the base; EditMode does not dispatch runtime lifecycle messages.
                typeof(AEnvironmentSpawner).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(spawner, null);
                Assert.IsTrue(root.gameObject.activeSelf);
                spawner.Clear();
                Assert.IsFalse(root);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void GrassComponentDisableAndRemoval_StopsAndOwnsAllInstantiatedStrips()
        {
            GameObject owner = new GameObject("Strip owner");
            try
            {
                EnvironmentGrass grass = owner.AddComponent<EnvironmentGrass>();
                GameObject stripObject = new GameObject("Owned strip");
                stripObject.transform.SetParent(owner.transform, false);
                GrassField strip = stripObject.AddComponent<GrassField>();
                TestHelpers.SetPrivateField(grass, "_strips", new List<GrassField> { strip });
                grass.enabled = false;
                TestHelpers.InvokePrivate(grass, "OnDisable");
                Assert.IsFalse(strip.enabled);
                grass.UpdateStrips(null);
                Assert.IsFalse(strip.enabled);
                grass.enabled = true;
                TestHelpers.InvokePrivate(grass, "OnEnable");
                Assert.IsTrue(strip.enabled);
                TestHelpers.InvokePrivate(grass, "OnDestroy");
                Object.DestroyImmediate(grass);
                Assert.IsFalse(stripObject);
                Assert.IsTrue(owner);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
