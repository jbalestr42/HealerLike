using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public class StageGameViewSizeTests
    {
        [Test]
        public void SelectPersistent_ReusesPortraitSizeAndTemporaryCaptureRestoresIt()
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
            try
            {
                StageGameViewSize.SelectPersistent(1080, 1920);
                int portrait = (int)selection.GetValue(window);
                int withPortrait = (int)count.Invoke(group, null);
                StageGameViewSize.SelectPersistent(1080, 1920);
                Assert.AreEqual(withPortrait, count.Invoke(group, null));
                Assert.AreEqual(portrait, selection.GetValue(window));
                object size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { portrait });
                Assert.AreEqual(1080, size.GetType().GetProperty("width").GetValue(size));
                Assert.AreEqual(1920, size.GetType().GetProperty("height").GetValue(size));
                using (new StageGameViewSize(640, 360)) { }
                Assert.AreEqual(portrait, selection.GetValue(window));
                Assert.AreEqual(withPortrait, count.Invoke(group, null));
            }
            finally
            {
                selection.SetValue(window, originalSelection);
                if ((int)count.Invoke(group, null) > originalCount)
                    group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { originalCount });
                sizesType.GetMethod("SaveToHDD", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(sizes, null);
            }
        }

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
