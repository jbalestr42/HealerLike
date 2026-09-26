using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitCreatureIconTests
    {
        sealed class Provider : IToolkitIconProvider
        {
            public event Action Changed;
            public Texture2D texture;
            public Entity.EntityType side;
            public int calls;
            public int listeners { get { return Changed?.GetInvocationList().Length ?? 0; } }
            public Texture2D GetCreatureIcon(EntityData data, Entity.EntityType side)
            {
                calls++;
                this.side = side;
                return texture;
            }
            public void Invalidate() { Changed?.Invoke(); }
        }

        Provider _provider;
        ToolkitGameView _view;
        VisualElement _root;
        EntityData _data;
        Texture2D _first;
        Texture2D _second;

        [SetUp]
        public void SetUp()
        {
            _first = new Texture2D(2, 2);
            _second = new Texture2D(2, 2);
            _data = ScriptableObject.CreateInstance<EntityData>();
            _provider = new Provider { texture = _first };
            _root = new VisualElement();
            _root.Add(new VisualElement { name = "cards" });
            _root.Add(new VisualElement { name = "detail-icon" });
            _view = new ToolkitGameView(_root, _provider);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            UnityEngine.Object.DestroyImmediate(_data);
            UnityEngine.Object.DestroyImmediate(_first);
            UnityEngine.Object.DestroyImmediate(_second);
        }

        ToolkitCardModel Model() { return new ToolkitCardModel { iconSource = _data, title = "Plant" }; }

        [Test]
        public void Invalidation_UpdatesAnUnchangedCardAndItsOpenDetailImmediately()
        {
            ToolkitCardModel model = Model();
            _view.SetCards("cards", new[] { model });
            _view.ShowDetail(model);
            _provider.texture = _second;
            _provider.Invalidate();
            VisualElement card = _root.Q(className: "data-card__icon");
            Assert.AreSame(_second, card.style.backgroundImage.value.texture);
            Assert.AreSame(_second, _root.Q("detail-icon").style.backgroundImage.value.texture);
            Assert.IsTrue(card.ClassListContains("creature-portrait"));
            Assert.AreEqual(Entity.EntityType.Player, _provider.side);
        }

        [Test]
        public void EntitySource_PreservesEnemySideWithoutInitializingGameplay()
        {
            GameObject go = new GameObject("icon source");
            try
            {
                Entity entity = null;
                TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
                entity.data = _data;
                entity.entityType = Entity.EntityType.Computer;
                Assert.AreSame(_first, _view.GetIcon(entity, out bool portrait));
                Assert.IsTrue(portrait);
                Assert.AreEqual(Entity.EntityType.Computer, _provider.side);
                _view.SetIconProvider(null);
                Assert.AreSame(_view.icons.GetIcon(_data), _view.GetIcon(entity, out portrait));
                Assert.IsFalse(portrait);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void NonCreatureSource_RetainsTheExistingFallbackAndNeverCallsPortraitProvider()
        {
            Texture2D result = _view.GetIcon(null, out bool portrait);
            Assert.AreSame(_view.icons.GetIcon(null), result);
            Assert.IsFalse(portrait);
            Assert.AreEqual(0, _provider.calls);
        }

        [Test]
        public void ProviderReplacement_RefreshesExistingCards_AndDetachesTheOldEvent()
        {
            _view.SetCards("cards", new[] { Model() });
            Provider next = new Provider { texture = _second };
            _view.SetIconProvider(next);
            int calls = next.calls;
            _provider.Invalidate();
            Assert.AreEqual(calls, next.calls);
            Assert.AreSame(_second, _root.Q(className: "data-card__icon").style.backgroundImage.value.texture);
        }

        [Test]
        public void ProviderRemoval_ReplacesBorrowedTextureWithFallback()
        {
            _view.SetCards("cards", new[] { Model() });
            _view.SetIconProvider(null);
            VisualElement icon = _root.Q(className: "data-card__icon");
            Assert.IsNotNull(icon.style.backgroundImage.value.texture);
            Assert.AreNotSame(_first, icon.style.backgroundImage.value.texture);
            Assert.IsFalse(icon.ClassListContains("creature-portrait"));
            Assert.IsTrue(_first);
        }

        [Test]
        public void Release_UnsubscribesAndDoesNotDestroyTheHostsBorrowedTextures()
        {
            _view.SetCards("cards", new[] { Model() });
            int calls = _provider.calls;
            _view.Release();
            _provider.Invalidate();
            Assert.AreEqual(calls, _provider.calls);
            Assert.IsTrue(_first);
        }

        [Test]
        public void DisabledHost_DetachesViewAndRetainsNewProviderWithoutSubscribing()
        {
            GameObject host = new GameObject("inactive icon host");
            host.SetActive(false);
            try
            {
                ToolkitGameUI ui = host.AddComponent<ToolkitGameUI>();
                ToolkitTimeControls controls = new ToolkitTimeControls();
                controls.Init(ui, new ToolkitGameContext(), _view);
                TestHelpers.SetPrivateField(ui, "_view", _view);
                TestHelpers.SetPrivateField(ui, "_timeControls", controls);
                Assert.AreEqual(1, _provider.listeners);
                TestHelpers.InvokePrivate(ui, "OnDisable");
                Assert.AreEqual(0, _provider.listeners);
                Provider replacement = new Provider { texture = _second };
                ui.SetIconProvider(replacement);
                Assert.AreSame(replacement, ui.iconProvider);
                Assert.AreEqual(0, replacement.listeners);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
