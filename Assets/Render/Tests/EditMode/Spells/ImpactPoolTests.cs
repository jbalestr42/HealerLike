using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class ImpactPoolTests
{
    GameObject _host;
    GameObject _target;
    GameObject _other;
    GameObject _caster;
    ZoneRegistry _zones;
    Ground _ground;
    ImpactPool _pool;

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
    }

    int CountBeams()
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

    Character AddCharacter(GameObject characterGo)
    {
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = characterGo.AddComponent<Character>());
        return character;
    }

    [Test]
    public void PulseArea_Hostile_SpawnsTheLitterInTheBaneAccentForItsEntryCycle()
    {
        _pool.PulseArea(Vector3.one, 2f, ZoneKind.Hostile, 1f, true);

        SpellEffect effect = _host.GetComponentInChildren<SpellEffect>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.shapes[0].GetComponent<Renderer>().GetPropertyBlock(block);
        float cycle = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Litter).cycleSeconds;
        Assert.AreEqual(EffectElement.Litter, effect.element);
        Assert.AreEqual(cycle, effect.lifetime);
        Assert.AreEqual(Vector3.one, effect.transform.position);
        Assert.Less(Vector4.Distance(RenderTestAssets.LoadPalette().bane, block.GetColor("_BaseColor")), 0.0001f);
    }

    [Test]
    public void PulseArea_Heal_KeepsTheExactRadius()
    {
        _pool.PulseArea(Vector3.right, 2f, ZoneKind.Heal, 0.3f, true);

        SpellEffect ring = _host.GetComponentInChildren<SpellEffect>();
        Assert.AreEqual(EffectElement.Ring, ring.element);
        Assert.AreEqual(Vector3.right, ring.transform.position);
        Assert.AreEqual(Vector3.one * 2f, ring.transform.localScale);
    }

    [Test]
    public void PulseArea_Heal_PulsesTheZonesForTheRingsCycle()
    {
        float cycle = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Ring).cycleSeconds;

        _pool.PulseArea(Vector3.zero, 2f, ZoneKind.Heal, 0.5f, true);
        _zones.PublishFrame(0f);

        Assert.AreEqual((int)ZoneKind.Heal, _zones.snapshot[0].kind);
        Assert.AreEqual(2f, _zones.snapshot[0].radius);
        _zones.PublishFrame(cycle);
        Assert.AreEqual(0, _zones.liveCount);
    }

    [Test]
    public void PulseArea_Hidden_PulsesTheZonesOnly()
    {
        _pool.PulseArea(Vector3.zero, 2f, ZoneKind.Hostile, 1f, false);

        Assert.AreEqual(0, _pool.count);
        Assert.AreEqual(1, _zones.liveCount);
    }

    [Test]
    public void PulseArea_NegativeRadius_PulsesNothing()
    {
        _pool.PulseArea(Vector3.zero, -1f, ZoneKind.Heal, 0.5f, true);

        Assert.AreEqual(0, _pool.count);
        Assert.AreEqual(0, _zones.liveCount);
    }

    [Test]
    public void ShowLink_ContactThread_DrawsAThreadWithoutBeads()
    {
        SpellEffect thread = _pool.ShowLink(Vector3.zero, Vector3.right, EffectFamily.Damage, true);

        Assert.IsTrue(thread.isContactThread);
        Assert.AreEqual(EffectElement.Beam, thread.element);
        Assert.IsFalse(thread.shapes[0].gameObject.activeSelf);
        Assert.IsTrue(thread.stalks[0].gameObject.activeSelf);
    }

    [Test]
    public void ShowImpact_Hit_ThrowsAShockThroughTheGrass()
    {
        _target.transform.position = new Vector3(2f, 0f, 1f);

        _pool.ShowImpact(_caster, _target, ResourceKind.Health, -30f, false);

        Assert.AreEqual(1, _ground.Playing(_ground.vocabulary.hit));
        Assert.IsTrue(_ground.Find(_ground.vocabulary.hit, out Vector2 at, out _, out float radius, out float strength));
        Assert.AreEqual(new Vector2(2f, 1f), at);
        Assert.AreEqual(ImpactPool.ShockRadius(0.3f, false), radius, 1e-5f);
        Assert.AreEqual(ImpactPool.HitShock(0.3f), strength, 1e-5f);
    }

    [Test]
    public void ShowImpact_ManaSpent_ThrowsNoShock()
    {
        _pool.ShowImpact(_caster, _target, ResourceKind.Mana, -10f, false);

        Assert.AreEqual(0, _ground.oneShotCount, "A spell's cost is not a blow.");
    }

    [Test]
    public void ShowImpact_Heal_ThrowsNoShock()
    {
        _pool.ShowImpact(_caster, _target, ResourceKind.Health, 30f, false);

        Assert.AreEqual(0, _ground.oneShotCount);
    }

    [Test]
    public void HitShock_LargerShare_ThrowsHarderButLessThanALanding()
    {
        Assert.Less(ImpactPool.HitShock(0.1f), ImpactPool.HitShock(0.8f));
        Assert.Less(ImpactPool.HitShock(1f), 1f, "A landing throws at full strength.");
        Assert.AreEqual(ImpactPool.HitShock(1f), ImpactPool.HitShock(3f));
    }

    [Test]
    public void ShockRadius_LargerShareOrCritical_BlowsWider()
    {
        Assert.Less(ImpactPool.ShockRadius(0.1f, false), ImpactPool.ShockRadius(0.6f, false));
        Assert.Less(ImpactPool.ShockRadius(0.6f, false), ImpactPool.ShockRadius(0.6f, true));
        Assert.AreEqual(ImpactPool.ShockRadius(1f, false), ImpactPool.ShockRadius(5f, false));
    }

    [Test]
    public void ShowImpact_LargerAmount_DrawsALargerBurst()
    {
        _pool.ShowImpact(null, _target, ResourceKind.Health, -1f, false);
        float small = _host.GetComponentInChildren<SpellEffect>().transform.localScale.x;
        _pool.Clear();

        _pool.ShowImpact(null, _target, ResourceKind.Health, -100f, false);

        Assert.Greater(_host.GetComponentInChildren<SpellEffect>().transform.localScale.x, small);
    }

    [Test]
    public void ShowImpact_LargerHeal_RaisesMoreSpheres()
    {
        _pool.ShowImpact(null, _target, ResourceKind.Health, 1f, false);
        int few = _host.GetComponentInChildren<SpellEffect>().count;
        _pool.Clear();

        _pool.ShowImpact(null, _target, ResourceKind.Health, 100f, false);

        Assert.AreEqual(3, few);
        Assert.AreEqual(8, _host.GetComponentInChildren<SpellEffect>().count);
    }

    [Test]
    public void ShowImpact_SignedAmounts_PickTheElement()
    {
        _pool.ShowImpact(null, _target, ResourceKind.Health, 5f, false);
        _pool.ShowImpact(null, _target, ResourceKind.Health, -5f, false);
        _pool.ShowImpact(null, _target, ResourceKind.Mana, 5f, false);
        _pool.ShowImpact(null, _target, ResourceKind.Mana, -5f, false);

        SpellEffect[] effects = _host.GetComponentsInChildren<SpellEffect>();
        Assert.AreEqual(4, _pool.count);
        Assert.AreEqual(EffectElement.Rise, effects[0].element);
        Assert.AreEqual(EffectElement.Burst, effects[1].element);
        Assert.AreEqual(EffectElement.ManaUp, effects[2].element);
        Assert.AreEqual(EffectElement.ManaDown, effects[3].element);
    }

    [Test]
    public void ShowImpact_Critical_ShowsTheRings()
    {
        _pool.ShowImpact(null, _target, ResourceKind.Health, -3f, true);

        SpellEffect effect = _host.GetComponentInChildren<SpellEffect>();
        Assert.Greater(effect.rings.Count, 0);
        foreach (Transform ring in effect.rings)
        {
            Assert.IsTrue(ring.gameObject.activeSelf, ring.name);
        }
    }

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

    [Test]
    public void Clear_Impacts_ReleasesThem()
    {
        _pool.ShowImpact(null, _other, ResourceKind.Health, 1f, false);

        _pool.Clear();

        Assert.AreEqual(0, _pool.count);
    }
}

}
