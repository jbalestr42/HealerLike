using System.Collections;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // A labelled support fixture on real entities: render-only head swap, real health/buff processing.
    public static class SpellSourceSupport
    {
        public static IEnumerator Capture(RenderManager manager, StageCaptureSession session, SpellSourceRun.Proof proof)
        {
            Entity source = manager.entityManager.GetEntities(Entity.EntityType.Computer)
                .Select(g => g.GetComponent<Entity>()).First();
            Entity target = manager.entityManager.GetEntities(Entity.EntityType.Player)
                .Select(g => g.GetComponent<Entity>()).First();
            CreatureBuilder host = source.GetComponentInChildren<CreatureBuilder>();
            CreatureRecipe original = host.rig.recipe;
            Material material = host.rig.partTransforms[0].GetComponent<Renderer>().sharedMaterial;
            CreatureRecipe fixture = LookComposer.Compose(new UnitChannels
            {
                side = LookSide.Stone, head = HeadKind.GiftHeal, count = CountBand.One,
                stem = StemBand.Steady, mass = MassBand.Light, reach = ReachBand.Short,
                accessory = AccessoryKind.None, accent = EffectFamily.Renew
            }, manager.creatureLooks.vocabulary);
            VisualElement ui = session.actions.ui.GetComponent<UIDocument>().rootVisualElement;
            Label label = new Label("SUPPORT FIXTURE: mineral GiftHeal\nReal creature owner and recipient; harness applies existing heal/boon consumers.");
            label.style.position = Position.Absolute;
            label.style.top = Length.Percent(16f);
            label.style.left = Length.Percent(4f);
            label.style.fontSize = 26;
            label.style.color = Color.white;
            label.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            ui.Add(label);
            try
            {
                session.output.Check(host.rig.Recompose(fixture, material, material, manager.meshes), "Support fixture accepted");
                manager.spellSink.Clear();
                // RestHealConsumer reads HealthMax, which creatures actually possess. The Character heal
                // instead requires the player-only HealPower attribute and is not a valid creature fixture.
                var healFactory = RenderAssets.Load<ConsumerFactory>("Assets/Data/Run/RestHealConsumer.asset");
                target.health.AddResourceModifier(ResourceModifier.Create(healFactory, source.gameObject, target.gameObject));
                yield return AStageRun.Wait(0.2f);
                proof.supportHealLinks = Measure(manager, host, proof);
                session.output.Check(proof.supportHealLinks > 0, "Creature health outcome produced an anatomical link");
                yield return session.Capture("02-mineral-heal-fixture", "Real resource outcome; labelled mineral healer fixture");
                manager.spellSink.Clear();
                var boonFactory = RenderAssets.Load<BuffCharacterSkillFactory>(
                    "Assets/Data/CharacterSkills/SingleTargetBuffAttackRate/SingleTargetBuffAttackRate.asset");
                ((BuffCharacterSkill)boonFactory.Create()).ApplySkillOnTarget(source.gameObject, target.gameObject);
                target.buffManager.ForceUpdate();
                yield return AStageRun.Wait(0.2f);
                proof.supportBoonLinks = Measure(manager, host, proof);
                session.output.Check(proof.supportBoonLinks > 0, "Creature boon application produced an anatomical link");
                yield return session.Capture("03-mineral-boon-fixture", "Real buff application; labelled support fixture");
            }
            finally
            {
                host.rig.Recompose(original, material, material, manager.meshes);
                manager.spellSink.Clear();
                label.RemoveFromHierarchy();
                Object.Destroy(fixture);
            }
        }

        static int Measure(RenderManager manager, CreatureBuilder host, SpellSourceRun.Proof proof)
        {
            CreatureSources.Resolve(host.rig, CreatureSources.Select(host.rig, 0), out Vector3 outlet);
            int count = 0;
            foreach (SpellEffect link in manager.spellSink.GetComponentsInChildren<SpellEffect>())
            {
                if (link.recipe.socket != EffectSocket.Link) continue;
                count++;
                proof.supportMaxAttachmentError = Mathf.Max(proof.supportMaxAttachmentError,
                    Vector3.Distance(outlet, link.castOrigin));
            }
            return count;
        }
    }
}
