using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public class RenderAssetRefreshTests
    {
        [Test]
        public void CreatureRevision_DetectsAnEditedSharedMaterialAndAnEditedOverrideRecipe()
        {
            CreatureLooks looks = ScriptableObject.CreateInstance<CreatureLooks>();
            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            Material material = new Material(Shader.Find("HL/Look/Primitive"));
            GameObject view = new GameObject("portrait source");
            try
            {
                CreatureBuilder builder = view.AddComponent<CreatureBuilder>();
                TestHelpers.SetPrivateField(builder, "_material", material);
                TestHelpers.SetPrivateField(builder, "_recipe", recipe);
                looks.plant = view;
                string initial = RenderAssetRefresh.CreatureRevision(looks);
                material.SetColor("_BaseColor", Color.magenta);
                EditorUtility.SetDirty(material);
                string painted = RenderAssetRefresh.CreatureRevision(looks);
                Assert.AreNotEqual(initial, painted);
                EditorUtility.SetDirty(recipe);
                Assert.AreNotEqual(painted, RenderAssetRefresh.CreatureRevision(looks));
                Assert.AreEqual(RenderAssetRefresh.CreatureRevision(looks), RenderAssetRefresh.CreatureRevision(looks));
            }
            finally
            {
                Object.DestroyImmediate(view);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(looks);
            }
        }
    }
}
