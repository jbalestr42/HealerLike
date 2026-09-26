using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // The fixed-clock actions and ground effects played by the lab's four creatures.
    public class GrassLabScenario
    {
        public static readonly float Step = 1f / 60f;
        public static readonly int Frames = 270;
        readonly GrassLabScene _scene;
        readonly GrassLabMeasurements _measurements;
        readonly GroundHandle _stoneAura;
        readonly GroundHandle _walkerAura;
        readonly GroundHandle _launch;
        readonly GroundHandle _poison;
        readonly GroundHandle _frost;
        readonly GroundHandle _warning;
        readonly PointerBrush _finger;

        public GrassLabScenario(GrassLabScene scene, GrassLabMeasurements measurements)
        {
            _scene = scene;
            _measurements = measurements;
            Ground ground = scene.ground;
            GroundVocabulary said = ground.vocabulary;
            _stoneAura = ground.Hold(said.ash);
            _walkerAura = ground.Hold(said.wilt);
            _launch = ground.Hold(said.launch);
            _poison = ground.Hold(said.blight);
            _frost = ground.Hold(said.frost);
            _warning = ground.Hold(said.warning);
            _finger = scene.Fixture("GrassLabFinger").AddComponent<PointerBrush>();
            _finger.Init(ground, scene.camera, 0f, 1f);
        }

        public void Tick(int frame)
        {
            Ground ground = _scene.ground;
            GroundVocabulary said = ground.vocabulary;
            Transform walker = _scene.creatures[0].anchor;
            Transform stone = _scene.creatures[1].anchor;
            Transform healed = _scene.creatures[2].anchor;
            Transform dropped = _scene.creatures[3].anchor;
            _measurements.BeginFrame();
            float time = frame * Step;
            // The walker crosses in three seconds, then stands
            float walk = Mathf.Clamp01(time / 3f);
            walker.position = new Vector3(Mathf.Lerp(-3.2f, 3.2f, walk), 0f, 0.5f + 0.4f * Mathf.Sin(walk * 5f));
            if (frame == 72)
            {
                dropped.gameObject.SetActive(true);
                GrassLabScene.CreatureBody landed = _scene.creatures[3];
                TrampleZone.Landing(ground, landed.preview.rig, dropped.position,
                                    TrampleZone.TrampleRadius(TrampleZone.CreatureFootprint(dropped,
                                                                                            landed.preview.rig)),
                                    new List<Vector3>());
            }

            // Both lose health across the run
            float stoneHealth = Mathf.Lerp(1f, 0.1f, time / (Frames * Step));
            float walkerHealth = Mathf.Lerp(1f, 0.2f, time / (Frames * Step));

            Aura(_stoneAura, GroundAura.Mark.Ash, stone.position, stoneHealth);
            Aura(_walkerAura, GroundAura.Mark.Wilt, walker.position, walkerHealth);
            foreach (GrassLabScene.CreatureBody creature in _scene.creatures)
            {
                creature.preview.Tick(time, Step, new FootFrame(creature.anchor.position, Vector3.up, 1f),
                                      Vector3.back);
            }
            if (frame == 36)
            {
                _scene.registry.AddHealPulse(healed, 1.6f);
            }

            // A low shot flies at the stone from the far corner, its trail following it as LaunchWave's does
            Shot(_launch, new Vector3(-3.5f, 0f, -3f), stone.position, (frame - 108) * Step, 0.6f);

            // The launch lands on the stone, then a critical hit lands on it
            if (frame == 132)
            {
                ground.Play(said.hit, stone.position, ImpactPool.ShockRadius(0.3f, false), ImpactPool.HitShock(0.3f));
            }

            if (frame == 170)
            {
                ground.Play(said.hit, stone.position, ImpactPool.ShockRadius(0.5f, true), ImpactPool.HitShock(0.5f));
            }

            // Hits, landings and the heal blast out of these; the lab measures motion against them
            _measurements.Source(stone.position, ImpactPool.ShockRadius(1f, true));
            _measurements.Source(healed.position, 1.6f);
            _measurements.Source(dropped.position, 2.5f);

            // The healed ally is poisoned, the stone slowed; a chain bolt scorches walker to stone to ally
            Status(_poison, healed.position, frame >= 120 && frame < 230);
            Status(_frost, stone.position, frame >= 60 && frame < 200);
            if (frame == 190)
            {
                ground.Play(said.scorch, walker.position, stone.position);
                ground.Play(said.scorch, stone.position, healed.position);
            }

            // The stone's area skill comes off cooldown from frame 200 to 240 and lands
            float cooldown = Mathf.Clamp01((240 - frame) / 40f) * GroundWarning.WarningShare;
            float shiver = frame >= 200 && frame < 240 ? GroundWarning.Strength(cooldown) : 0f;
            if (shiver > 0f)
            {
                _warning.Show(stone.position, 1.5f, shiver);
            }
            else
            {
                _warning.Hide();
            }

            // A finger strokes through the open grass on the left in the first second
            Vector2 stroke = _scene.camera.WorldToScreenPoint(new Vector3(-3f + 4f * (frame / 60f), 0f, -2.8f));
            _finger.Point(frame < 60, frame == 0, stroke);
            _measurements.Source(new Vector3(-3f, 0f, -2.8f), new Vector3(1f, 0f, -2.8f));

        }

        static void Status(GroundHandle handle, Vector3 position, bool isOn)
        {
            if (isOn)
            {
                handle.Show(position, GroundStatus.PatchRadius, 1f);
            }
            else
            {
                handle.Hide();
            }
        }

        // A shot from source to target taking flight seconds, at a quarter of a unit over the ground
        void Shot(GroundHandle handle, Vector3 source, Vector3 target, float age, float flight)
        {
            if (age <= 0f || age >= flight)
            {
                handle.Hide();
                return;
            }

            Vector3 head = Vector3.Lerp(source, target, age / flight);
            Vector3 travelled = head - source;
            float length = Mathf.Min(travelled.magnitude, LaunchWave.TrailLength);
            handle.ShowLine(head - travelled.normalized * length, head, LaunchWave.Strength(0.25f));
            _measurements.Source(source, head);
        }

        static void Aura(GroundHandle handle, GroundAura.Mark mark, Vector3 position, float health)
        {
            float strength = GroundAura.Strength(mark, health);
            if (strength <= 0f)
            {
                handle.Hide();
                return;
            }

            handle.Show(position, GroundAura.Radius(mark, health), strength);
        }

    }
}
