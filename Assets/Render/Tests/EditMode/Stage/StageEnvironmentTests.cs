using System;
using HealerLike.Render.Environment;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    [ExecuteAlways]
    public class EnvironmentRetirementProbe : MonoBehaviour
    {
        public Action onDisable;
        void OnDisable() => onDisable?.Invoke();
    }

    public class StageEnvironmentTests
    {
        [Test]
        public void Clear_HidesThePublishedRootBeforeRetiringItsSceneObjects()
        {
            GameObject owner = new GameObject("Stage environment");
            StageEnvironment environment = new StageEnvironment();
            try
            {
                EnvironmentRoot root = owner.AddComponent<EnvironmentRoot>();
                EnvironmentRetirementProbe probe = owner.AddComponent<EnvironmentRetirementProbe>();
                bool hiddenBeforeRelease = false;
                probe.onDisable = () => hiddenBeforeRelease = !owner.activeSelf;
                TestHelpers.SetPrivateField(environment, "_root", root);

                environment.Clear();
                environment.Clear();

                Assert.IsTrue(hiddenBeforeRelease);
                Assert.IsNull(environment.root);
                Assert.IsFalse(owner);
            }
            finally
            {
                environment.Clear();
                Object.DestroyImmediate(owner);
            }
        }
    }
}
