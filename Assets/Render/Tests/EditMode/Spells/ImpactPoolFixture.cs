using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

    public abstract class ImpactPoolFixture
    {
        protected GameObject _host;
        protected GameObject _target;
        protected GameObject _other;
        protected GameObject _caster;
        protected ZoneRegistry _zones;
        protected Ground _ground;
        protected ImpactPool _pool;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Host");
            _target = new GameObject("Target");
            _other = new GameObject("Other");
            _caster = new GameObject("Caster");
            _zones = _host.AddComponent<ZoneRegistry>();
            _zones.Init(new ZoneFakeUpload());
            _ground = new Ground();
            _pool = new ImpactPool();
            _pool.Init(_host.transform, RenderTestAssets.LoadEffectVocabulary(), RenderTestAssets.LoadMeshes(),
                       RenderTestAssets.LoadLookMaterial(), _zones, _ground, null);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Clear();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_other);
            Object.DestroyImmediate(_caster);
            _ground.Dispose();
        }

        protected int CountBeams()
        {
            int beams = 0;
            foreach (SpellEffect effect in _host.GetComponentsInChildren<SpellEffect>())
            {
                if (effect.element == EffectElement.Beam)
                {
                    beams++;
                }
            }

            return beams;
        }

        protected Character AddCharacter(GameObject characterGo)
        {
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = characterGo.AddComponent<Character>());
            return character;
        }

    }
}
