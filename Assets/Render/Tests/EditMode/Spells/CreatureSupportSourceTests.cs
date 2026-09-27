using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Spells
{
    public class CreatureSupportSourceTests
    {
        GameObject _source, _target, _effects;
        GameObject _foreignSource;
        CreatureRecipe _recipe;
        CreatureRecipe _foreignRecipe;
        SourcePathHost _host;
        SourcePathHost _foreignHost;
        SpellVisualSink _sink;
        readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _source = new GameObject("mineral healer fixture");
            _target = new GameObject("recipient");
            _target.transform.position = Vector3.right * 4f;
            _effects = new GameObject("effects");
            _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.GiftHeal),
                RenderTestAssets.LoadLookVocabulary());
            _host = _source.AddComponent<SourcePathHost>();
            _host.Build(_recipe);
            _foreignSource = new GameObject("foreign source");
            _foreignRecipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.GiftHeal),
                RenderTestAssets.LoadLookVocabulary());
            _foreignHost = _foreignSource.AddComponent<SourcePathHost>();
            _foreignHost.Build(_foreignRecipe);
            _sink = SpellSinkFixture.Add(_effects);
        }

        [TearDown]
        public void TearDown()
        {
            _sink.Clear();
            _host.Release();
            _foreignHost.Release();
            Object.DestroyImmediate(_effects);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_foreignSource);
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_foreignRecipe);
            foreach (Object value in _created) Object.DestroyImmediate(value);
            _created.Clear();
        }

        int Links() => _effects.GetComponentsInChildren<SpellEffect>().Count(e => e.recipe.socket == EffectSocket.Link);

        [TestCase(false)]
        [TestCase(true)]
        public void PositiveHealth_ArmlessExplicitSourceConnectsIncludingSelfTarget(bool self)
        {
            _sink.ShowImpact(_source, self ? _source : _target, ResourceKind.Health, 3f, false);
            Assert.AreEqual(1, Links());
            Assert.AreEqual(0, _host.rig.armCount);
            SpellEffect link = _effects.GetComponentsInChildren<SpellEffect>().First(e => e.recipe.socket == EffectSocket.Link);
            link.Advance(0f);
            CreatureSources.Resolve(_host.rig, CreatureSources.Select(_host.rig, 0), out Vector3 source);
            Assert.Less(Vector3.Distance(source, link.castOrigin), 0.00001f);
        }

        [Test]
        public void BoonApplication_ConnectsOnce_ElapsedRefreshDoesNotReplay_StackIncreaseDoes()
        {
            BuffHandlerFactory factory = SpellSinkFixture.Modifier(AttributeType.AttackRate, 0.1f, _created);
            _sink.SetStatus(_source, _target, factory, 1, 0f, 4f);
            Assert.AreEqual(1, Links());
            _sink.SetStatus(_source, _target, factory, 1, 0.3f, 4f);
            Assert.AreEqual(1, Links());
            _sink.SetStatus(_source, _target, factory, 2, 0.3f, 4f);
            Assert.AreEqual(2, Links());
        }

        [Test]
        public void CountLinksFrom_AttributesOnlyLinksCastFromTheRequestedRig()
        {
            SpellEffect own = SpellEffect.Create(EffectComposer.Link(RenderTestAssets.LoadEffectVocabulary(),
                EffectFamily.Renew), null, RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadLookMaterial(), null);
            SpellEffect foreign = SpellEffect.Create(EffectComposer.Link(RenderTestAssets.LoadEffectVocabulary(),
                EffectFamily.Renew), null, RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadLookMaterial(), null);
            _created.Add(own.gameObject);
            _created.Add(foreign.gameObject);
            own.SetCastSource(_source);
            foreign.SetCastSource(_foreignSource);
            own.transform.SetParent(_effects.transform, false);
            foreign.transform.SetParent(_effects.transform, false);

            Assert.AreEqual(1, SpellSourceSupport.CountLinksFrom(_sink, _host.rig));
            Assert.AreEqual(1, SpellSourceSupport.CountLinksFrom(_sink, _foreignHost.rig));
        }
    }
}
