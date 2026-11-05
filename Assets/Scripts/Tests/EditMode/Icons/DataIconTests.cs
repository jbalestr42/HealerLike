using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Icons
{
    // The game's data each have their own drawn icon (see IconPainter), never one shared with another
    public class DataIconTests
    {
        static List<StatusData> Statuses()
        {
            return AssetDatabase.FindAssets("t:StatusData", new[] { "Assets/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<StatusData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        [Test]
        public void EveryStatus_HasItsOwnDrawnIcon()
        {
            List<StatusData> statuses = Statuses();
            Assert.IsNotEmpty(statuses);

            var icons = new List<Sprite>();
            foreach (StatusData status in statuses)
            {
                Sprite icon = status.buffHandlerFactory != null ? status.buffHandlerFactory.icon : null;
                Assert.IsNotNull(icon, $"{status.name} has no icon");
                StringAssert.StartsWith("Assets/Icons/Statuses/", AssetDatabase.GetAssetPath(icon), $"{status.name}'s icon");
                icons.Add(icon);
            }
            Assert.AreEqual(icons.Count, icons.Distinct().Count(), "Two statuses share an icon");
        }

        [TestCase("Assets/Data/EntityItems")]
        [TestCase("Assets/Data/PlayerItems")]
        [TestCase("Assets/Data/EventItems")]
        public void EveryItem_HasItsOwnDrawnIcon(string folder)
        {
            List<AItemFactory> items = AssetDatabase.FindAssets("t:AItemFactory", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AItemFactory>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
            Assert.IsNotEmpty(items);

            var icons = new List<Sprite>();
            foreach (AItemFactory item in items)
            {
                var icon = new SerializedObject(item).FindProperty("data.icon").objectReferenceValue as Sprite;
                Assert.IsNotNull(icon, $"{item.name} has no icon");
                StringAssert.StartsWith("Assets/Icons/Items/", AssetDatabase.GetAssetPath(icon), $"{item.name}'s icon");
                icons.Add(icon);
            }
            Assert.AreEqual(icons.Count, icons.Distinct().Count(), "Two items share an icon");
        }
    }
}
