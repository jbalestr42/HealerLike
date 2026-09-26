using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StagePortraitLifecycleTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Refresh_ReplacesTheCaptureSourceWhenManagerAssetReferenceChanges(bool replaceLooks)
        {
            GameObject host = new GameObject("portrait source lifecycle");
            host.SetActive(false);
            CreatureLooks firstLooks = ScriptableObject.CreateInstance<CreatureLooks>();
            CreatureLooks nextLooks = ScriptableObject.CreateInstance<CreatureLooks>();
            PrimitiveMeshes firstMeshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
            PrimitiveMeshes nextMeshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
            StageInterface stage = null;
            try
            {
                // Inactive hosts avoid Awake, singleton discovery and any live camera capture.
                RenderManager manager = host.AddComponent<RenderManager>();
                ToolkitGameUI ui = host.AddComponent<ToolkitGameUI>();
                stage = host.AddComponent<StageInterface>();
                stage.Init(manager, null);
                TestHelpers.SetPrivateField(manager, "_creatureLooks", firstLooks);
                TestHelpers.SetPrivateField(manager, "_meshes", firstMeshes);
                TestHelpers.SetPrivateField(stage, "_ui", ui);
                TestHelpers.InvokePrivate(stage, "CreatePortraits");
                CreaturePortraits initial = stage.portraits;
                stage.RefreshCreatureIcons();
                Assert.AreSame(initial, stage.portraits, "An edit within the same source invalidates its existing cache.");

                if (replaceLooks) TestHelpers.SetPrivateField(manager, "_creatureLooks", nextLooks);
                else TestHelpers.SetPrivateField(manager, "_meshes", nextMeshes);
                stage.RefreshCreatureIcons();
                Assert.IsTrue(initial.isDisposed);
                Assert.AreNotSame(initial, stage.portraits);
                Assert.AreSame(stage.portraits, ui.iconProvider);
                Assert.AreEqual(0, stage.portraits.captureCount);

                CreaturePortraits replacement = stage.portraits;
                TestHelpers.InvokePrivate(stage, "ReleasePortraits");
                Assert.IsTrue(replacement.isDisposed);
                Assert.IsNull(stage.portraits);
                Assert.IsNull(ui.iconProvider);
            }
            finally
            {
                if (stage != null) TestHelpers.InvokePrivate(stage, "ReleasePortraits");
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(firstLooks);
                Object.DestroyImmediate(nextLooks);
                Object.DestroyImmediate(firstMeshes);
                Object.DestroyImmediate(nextMeshes);
            }
        }
    }
}
