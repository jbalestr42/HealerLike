using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

    public class GroupImpactLinksTests : ImpactPoolFixture
    {
        [Test]
        public void Flush_OneCharacterHealingTwoRecipients_LinksEachOnceInLime()
        {
            AddCharacter(_caster);
            _pool.ShowImpact(_caster, _target, ResourceKind.Health, 3f, false);
            _pool.ShowImpact(_caster, _target, ResourceKind.Health, 4f, false);
            _pool.ShowImpact(_caster, _other, ResourceKind.Health, 2f, false);
            _pool.ShowImpact(_other, _target, ResourceKind.Health, 2f, false);
            _pool.ShowImpact(_caster, _target, ResourceKind.Mana, 2f, false);
            Assert.AreEqual(0, CountBeams());

            _pool.Flush(true);
            _pool.Flush(true);

            Assert.AreEqual(2, CountBeams()); // one per recipient, the second flush has nothing left
            SpellEffect beam = null;
            foreach (SpellEffect effect in _host.GetComponentsInChildren<SpellEffect>())
            {
                if (effect.element == EffectElement.Beam)
                {
                    beam = effect;
                }
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            beam.stalks[0].GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.Less(Vector4.Distance(RenderTestAssets.LoadPalette().heal, block.GetColor("_BaseColor")), 0.0001f);
        }

        [Test]
        public void Flush_SingleRecipient_DrawsNoBeam()
        {
            AddCharacter(_caster);
            _pool.ShowImpact(_caster, _target, ResourceKind.Health, 3f, false);

            _pool.Flush(true);

            Assert.AreEqual(0, CountBeams());
        }

        [Test]
        public void Flush_OneHealAndOneHit_DrawsNoBeam()
        {
            AddCharacter(_caster);
            _pool.ShowImpact(_caster, _target, ResourceKind.Health, 3f, false);
            _pool.ShowImpact(_caster, _other, ResourceKind.Health, -3f, false);

            _pool.Flush(true);

            Assert.AreEqual(0, CountBeams());
        }

        [Test]
        public void Flush_Hidden_ForgetsTheCastWithoutABeam()
        {
            AddCharacter(_caster);
            _pool.ShowImpact(_caster, _target, ResourceKind.Health, 3f, false);
            _pool.ShowImpact(_caster, _other, ResourceKind.Health, 3f, false);

            _pool.Flush(false);
            _pool.Flush(true);

            Assert.AreEqual(0, CountBeams());
        }

    }
}
