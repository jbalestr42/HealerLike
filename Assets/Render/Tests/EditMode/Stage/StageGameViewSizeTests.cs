using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public class StageGameViewSizeTests
    {
        [Test]
        public void Dispose_TemporaryPreset_RestoresSelectionAndRemovesOnlyItsOwnSize()
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            MethodInfo count = group.GetType().GetMethod("GetTotalCount");
            Type windowType = editor.GetType("UnityEditor.GameView");
            EditorWindow window = EditorWindow.GetWindow(windowType);
            PropertyInfo selection = windowType.GetProperty("selectedSizeIndex");
            int originalCount = (int)count.Invoke(group, null);
            int originalSelection = (int)selection.GetValue(window);
            StageGameViewSize capture = new StageGameViewSize(1080, 1920);
            try
            {
                Assert.AreEqual(originalCount + 1, count.Invoke(group, null));
                Assert.AreEqual(originalCount, selection.GetValue(window));
                object preset = group.GetType().GetMethod("GetGameViewSize").Invoke(group,
                    new object[] { originalCount });
                Assert.AreEqual(1080, preset.GetType().GetProperty("width").GetValue(preset));
                Assert.AreEqual(1920, preset.GetType().GetProperty("height").GetValue(preset));
            }
            finally
            {
                capture.Dispose();
                capture.Dispose();
            }
            Assert.AreEqual(originalCount, count.Invoke(group, null));
            Assert.AreEqual(originalSelection, selection.GetValue(window));
        }
    }
}
