using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Real Main with Toolkit HUD. Only the proof shot is harness spawned, through the gameplay spawn contract.
    public class SpellSourceRun : AStageRun
    {
        [Serializable]
        public class Sample
        {
            public float gameTime;
            public int frame;
            public string sourceId;
            public Vector3 outlet;
            public Vector3 root;
            public Vector3 logicalShot;
            public float attachmentError;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
        }
        [Serializable]
        public class Proof
        {
            public string revision = StagePlay.ReadRevision();
            public string baseline = "ea20bdb945ab5190fbd5ca7a22be1aecc5642ff9";
            public string unityVersion = Application.unityVersion;
            public string condition = "Real Main battle and HUD. Proof projectile spawned through EntityManager; "
                + "ordinary projectile behaviour, source entity and hit contract. Simulation paused for PNG readback. "
                + "Grammar pages are labelled Render-only fixtures, not roster or gameplay attribution.";
            public bool passed;
            public int supportHealLinks;
            public int supportBoonLinks;
            public float supportMaxAttachmentError;
            public bool recomposedWhileHeld;
            public bool projectileStartUnchanged;
            public bool stableLease;
            public List<Sample> samples = new List<Sample>();
        }
        readonly Proof _proof = new Proof();
        StageInterfaceOutput _output;
        StageCaptureSession _session;
        string _folder;

        public static void Capture() => SpellSourcePreparation.Enter("spell-sources", 360f);

        protected override void OnFailed(Exception error)
        {
            _output?.Fail(error.ToString());
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            _folder = Path.Combine(StagePlay.CaptureFolder, "spell-sources");
            _output = new StageInterfaceOutput(_folder);
            _session = new StageCaptureSession(_manager, _output);
            GameObject shotObject = null;
            CreatureRecipe edited = null;
            CreatureRecipe original = null;
            CreatureBuilder heldHost = null;
            BattleFocus focus = null;
            bool focusEnabled = false;
            try
            {
                _session.AttachInput();
                _manager.SetLandscape(false);
                yield return _session.Resize(1080, 1920);
                _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
                yield return Wait(0.5f);
                _hud.nextWaveButton.onClick.Invoke();
                yield return Wait(1f);
                focus = Object.FindAnyObjectByType<BattleFocus>();
                focusEnabled = focus && focus.enabled;
                if (focus) focus.enabled = false;
                Time.timeScale = 0f;
                yield return _session.Capture("00-real-battle", "Real battle before proof projectile");
                Entity source = _manager.entityManager.GetEntities(Entity.EntityType.Player)
                    .Select(g => g.GetComponent<Entity>()).First(e => e.GetComponentInChildren<CreatureBuilder>());
                Entity target = _manager.entityManager.GetEntities(Entity.EntityType.Computer)
                    .Select(g => g.GetComponent<Entity>()).OrderByDescending(e =>
                        Vector3.Distance(e.transform.position, source.transform.position)).First();
                CreatureBuilder host = source.GetComponentInChildren<CreatureBuilder>();
                heldHost = host;
                original = host.rig.recipe;
                _output.Check(host.rig != null && CreatureSources.HasExplicit(host.rig), "Real source has anatomical metadata");
                GameObject prefab = RenderAssets.Load<GameObject>("Assets/Prefabs/Projectiles/BulletSpeed.prefab");
                Vector3 logicalStart = source.transform.position + Vector3.up * 1.5f;
                shotObject = _manager.entityManager.SpawnProjectile(prefab, logicalStart, Quaternion.identity);
                Projectile shot = shotObject.GetComponent<Projectile>();
                shot.Init(source.gameObject, target.gameObject, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
                _proof.projectileStartUnchanged = shot.transform.position == logicalStart;
                ProjectileVisualObserver observer = shotObject.GetComponent<ProjectileVisualObserver>();
                _output.Check(observer && observer.gestureToken != 0, "Spawn dressing claimed proof delivery");
                string selected = null;
                for (int sample = 0; sample < 4; sample++)
                {
                    if (sample == 2)
                    {
                        edited = Object.Instantiate(original);
                        int part = Array.FindIndex(edited.parts, p => p.sourceId == selected);
                        edited.parts[part].dimensions *= 1.08f;
                        Material material = host.rig.partTransforms[0].GetComponent<Renderer>().sharedMaterial;
                        _proof.recomposedWhileHeld = host.rig.Recompose(edited, material, material, _manager.meshes);
                        _output.Check(_proof.recomposedWhileHeld, "Held delivery survives render-only size edit");
                    }
                    Time.timeScale = 0.02f;
                    yield return Wait(0.25f);
                    Time.timeScale = 0f;
                    yield return Wait(0.05f);
                    _output.Check(shot && host && host.rig != null, "Proof shot and source remain alive");
                    LianaArm arm = host.GetDeliveryArm(observer.gestureToken);
                    _output.Check(arm != null && !arm.isAvailable, "Exact projectile lease remains held");
                    if (selected == null)
                        selected = host.rig.parts.Where(p => p.isSource).OrderBy(p =>
                        {
                            CreatureSources.Resolve(host.rig, p.sourceId, out Vector3 point);
                            return Vector3.Distance(point, arm.Joint(0));
                        }).First().sourceId;
                    _output.Check(CreatureSources.Resolve(host.rig, selected, out Vector3 outlet), "Held outlet resolves");
                    Sample measurement = new Sample
                    {
                        gameTime = Time.time, frame = Time.frameCount, sourceId = selected, outlet = outlet,
                        root = arm.Joint(0), logicalShot = shot.transform.position,
                        attachmentError = Vector3.Distance(outlet, arm.Joint(0)),
                        cameraPosition = _manager.gameCamera.transform.position,
                        cameraRotation = _manager.gameCamera.transform.rotation
                    };
                    _proof.samples.Add(measurement);
                    _output.Check(measurement.attachmentError < 0.0001f, "Delivery root attached at sample " + sample);
                    yield return _session.Capture("01-cast-" + sample.ToString("00"), "Timed gameplay projectile, paused at sample");
                }
                _proof.stableLease = _proof.samples.Select(s => s.sourceId).Distinct().Count() == 1;
                Object.Destroy(shotObject);
                shotObject = null;
                yield return SpellSourceSupport.Capture(_manager, _session, _proof);
                using (var fixture = new SpellSourceFixture(_manager, _session))
                    yield return fixture.Capture();
                _proof.passed = _proof.projectileStartUnchanged && _proof.stableLease && _proof.samples.Count == 4
                    && _proof.recomposedWhileHeld && _proof.supportMaxAttachmentError < 0.0001f;
            }
            finally
            {
                if (shotObject) Object.Destroy(shotObject);
                if (heldHost && original)
                {
                    Material material = heldHost.rig.partTransforms[0].GetComponent<Renderer>().sharedMaterial;
                    heldHost.rig.Recompose(original, material, material, _manager.meshes);
                }
                if (edited) Object.Destroy(edited);
                if (focus) focus.enabled = focusEnabled;
                _session.Dispose();
                _output.Write(_proof.passed);
                Directory.CreateDirectory(_folder);
                File.WriteAllText(Path.Combine(_folder, "source-proof.json"), JsonUtility.ToJson(_proof, true));
                StagePlay.Finish(this, _proof.passed);
            }
        }
    }
}
