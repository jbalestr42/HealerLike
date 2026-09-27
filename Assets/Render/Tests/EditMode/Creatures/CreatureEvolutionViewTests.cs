using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureEvolutionViewTests
    {
        GameObject _owner;
        GameObject _viewObject;
        GameObject _managerObject;
        EntityData _data;
        CreatureLooks _looks;
        CreatureBuilder _view;
        HealerLike.Render.Stage.RenderManager _manager;
        AttributeManager _attributes;
        Material _material;
        Entity _entity;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Live creature");
            _attributes = TestHelpers.CreateAttributeManager(_owner, AttributeType.HealthMax, 100f);
            TestHelpers.WithLoggingDisabled(() => _entity = _owner.AddComponent<Entity>());
            _entity.attributeManager = _attributes;
            _data = ScriptableObject.CreateInstance<EntityData>();
            _entity.data = _data;
            _managerObject = new GameObject("Presentation manager");
            _manager = _managerObject.AddComponent<HealerLike.Render.Stage.RenderManager>();
            _looks = ScriptableObject.CreateInstance<CreatureLooks>();
            _looks.vocabulary = RenderTestAssets.LoadLookVocabulary();
            TestHelpers.SetPrivateField(_manager, "_creatureLooks", _looks);
            TestHelpers.SetPrivateField(_manager, "_meshes", RenderTestAssets.LoadMeshes());
            _viewObject = new GameObject("Derived view");
            _viewObject.transform.SetParent(_owner.transform, false);
            _view = _viewObject.AddComponent<CreatureBuilder>();
            _material = new Material(RenderTestAssets.LoadLookMaterial());
            RenderTestAssets.SetRecipe(_view, null, _material, RenderTestAssets.LoadMeshes());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_managerObject);
            Object.DestroyImmediate(_data);
            Object.DestroyImmediate(_looks);
            Object.DestroyImmediate(_material);
        }

        void Tick() => TestHelpers.InvokePrivate(_view, "LateUpdate");

        [Test]
        public void LiveUpgrade_ReusesRigAndLease_ThenRestoresWithoutFrameRebuilds()
        {
            _view.Init(_entity, _manager);
            Tick();
            CreatureRig rig = _view.rig;
            Transform root = rig.root;
            int initial = rig.revision;
            using (var lease = new CastSourceLease(rig))
            {
                Assert.IsTrue(lease.TryGet(out _));
                string id = lease.sourceId;
                Attribute health = _attributes.Get(AttributeType.HealthMax);
                health.BaseValue = 400f;
                health.Update();
                Tick();
                Assert.AreSame(rig, _view.rig);
                Assert.AreSame(root, rig.root);
                Assert.AreEqual(initial + 1, rig.revision);
                Assert.IsTrue(lease.TryGet(out _));
                Assert.AreEqual(id, lease.sourceId);
                for (int i = 0; i < 5; i++) Tick();
                Assert.AreEqual(initial + 1, rig.revision);
                health.BaseValue = 100f;
                health.Update();
                Tick();
                Assert.AreEqual(initial + 2, rig.revision);
                Assert.IsTrue(lease.TryGet(out _));
            }
        }

        [Test]
        public void HealthDamage_DoesNotChangeTheMaximumHealthSilhouette()
        {
            ResourceAttribute health = _owner.AddComponent<ResourceAttribute>();
            health.Init(AttributeType.HealthMax);
            TestHelpers.SetPrivateField(_entity, "_health", health);
            _view.Init(_entity, _manager);
            Tick();
            int revision = _view.rig.revision;
            TestHelpers.SetPrivateField(health, "_value", 20f);
            health.OnValueChanged.Invoke(health);
            Tick();
            Assert.AreEqual(revision, _view.rig.revision);
            Assert.AreEqual(100f, health.Max);
            Assert.AreEqual(20f, health.Value);
        }

        [Test]
        public void PreviewOwnerWithUninitializedAttributeComponent_UsesDataUntilLiveInit()
        {
            _entity.attributeManager = null;
            TestHelpers.SetPrivateField(_attributes, "_attributes", null);
            Assert.DoesNotThrow(() => _view.Init(_entity, _manager));
            Assert.DoesNotThrow(Tick);
            Assert.IsNotNull(_view.rig);
            Assert.IsTrue(_view.Rebuild(_manager));
            // A later proper owner initialization opts back into live evolution.
            TestHelpers.InvokePrivate(_attributes, "Awake");
            _attributes.Add(AttributeType.HealthMax, new Attribute(100f));
            _entity.attributeManager = _attributes;
            _view.Init(_entity, _manager);
            Tick();
            int revision = _view.rig.revision;
            Attribute maximum = _attributes.Get(AttributeType.HealthMax);
            maximum.BaseValue = 400f;
            maximum.Update();
            Tick();
            Assert.AreEqual(revision + 1, _view.rig.revision);
        }

        [Test]
        public void AuthoredRecipe_RemainsAuthoritativeUnderBuffs()
        {
            CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
            try
            {
                RenderTestAssets.SetRecipe(_view, recipe, _material, RenderTestAssets.LoadMeshes());
                _view.Init(_entity, _manager);
                int revision = _view.rig.revision;
                Attribute health = _attributes.Get(AttributeType.HealthMax);
                health.BaseValue = 400f;
                health.Update();
                Tick();
                Assert.AreSame(recipe, _view.recipe);
                Assert.AreEqual(revision, _view.rig.revision);
            }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void DisableAndReenable_ReconcilesChangesWhileHidden()
        {
            _view.Init(_entity, _manager);
            Tick();
            int revision = _view.rig.revision;
            _view.enabled = false;
            TestHelpers.InvokePrivate(_view, "OnDisable");
            Attribute health = _attributes.Get(AttributeType.HealthMax);
            health.BaseValue = 400f;
            health.Update();
            _view.enabled = true;
            TestHelpers.InvokePrivate(_view, "OnEnable");
            Tick();
            Assert.AreEqual(revision + 1, _view.rig.revision);
            Assert.IsTrue(_view.rig.root.gameObject.activeSelf);
        }
    }
}
