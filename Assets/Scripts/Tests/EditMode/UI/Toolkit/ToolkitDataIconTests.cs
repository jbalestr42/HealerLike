using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitDataIconTests
    {
        class Provider : IToolkitIconProvider, IToolkitDataIconProvider
        {
            public event Action Changed;
            public Texture2D image;
            public object source;
            public Texture2D GetCreatureIcon(EntityData data, Entity.EntityType side) { return null; }
            public Texture2D GetDataIcon(object value) { source = value; return image; }
            public void Invalidate() { Changed?.Invoke(); }
        }

        [Test]
        public void OptionalDataProvider_RefreshesCardsAndDetailWithoutOwningItsTextures()
        {
            Texture2D first = new Texture2D(4, 4);
            Texture2D second = new Texture2D(4, 4);
            Provider provider = new Provider { image = first };
            VisualElement root = new VisualElement();
            root.Add(new VisualElement { name = "cards" });
            root.Add(new VisualElement { name = "detail-icon" });
            ToolkitGameView view = new ToolkitGameView(root, provider);
            object source = new object();
            try
            {
                ToolkitCardModel model = new ToolkitCardModel { iconSource = source, title = "Spell" };
                view.SetCards("cards", new[] { model });
                view.ShowDetail(model);
                Assert.AreSame(first, view.GetIcon(source, out bool portrait));
                Assert.IsFalse(portrait);
                Assert.AreSame(source, provider.source);
                provider.image = second;
                provider.Invalidate();
                Assert.AreSame(second, root.Q<VisualElement>("card-icon").style.backgroundImage.value.texture);
                Assert.AreSame(second, root.Q<VisualElement>("detail-icon").style.backgroundImage.value.texture);
                provider.image = null;
                Assert.AreSame(view.icons.GetIcon(source), view.GetIcon(source, out portrait));
                view.Release();
                Assert.IsTrue(first && second, "The provider owns borrowed artwork.");
            }
            finally
            {
                view.Release();
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }
    }
}
