using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // A real Main session. Existing Character skills publish through health and buff observers; a spawned
    // gameplay projectile exercises its ordinary dressing and Homing behaviour with a Character source.
    public class OffscreenPlayerRun : AStageRun
    {
        [Serializable]
        class Proof
        {
            public bool passed;
            public string unityVersion;
            public int characterRenderers;
            public bool characterHasRig;
            public bool characterTrampleEnabled;
            public Vector3 characterPosition;
            public Vector3 castViewport;
            public int boardTufts;
            public float tuftHeight;
            public float tuftWidth;
            public int resolvedCharacterHeals;
            public int healLinks;
            public int statusLinks;
            public int statusCount;
            public Vector3 projectileLogicalStart;
            public Vector3 projectileVisualStart;
            public Vector3 projectileEntryViewport;
            public bool projectileDressedBySpawn;
            public bool logicalCharacterUnchanged;
            public List<Vector3> projectileLogicalPositions = new List<Vector3>();
            public List<Vector3> projectileVisualPositions = new List<Vector3>();
            public string limitation = "Captures freeze simulation at each sample. The projectile is spawned by the harness with the real Character as source; no Character projectile skill exists in the shipped roster. This verifies presentation and native rendering, not device performance.";
        }

        protected override IEnumerator Run()
        {
            StageGameViewSize size = new StageGameViewSize(1080, 1920);
            string folder = Path.Combine(StagePlay.CaptureFolder, "offscreen-player");
            StageMotionOutput output = new StageMotionOutput(folder);
            Proof proof = new Proof { unityVersion = Application.unityVersion };
            output.manifest.condition = "Real Main with a hidden Character; six paused samples: playfield, heal, status cast and three projectile positions. Gameplay runs between projectile samples; this is not an ambient-motion comparison.";
            output.manifest.region = "No pixel-difference region or ambient-motion metrics measured; zero-valued motion fields are unused.";
            output.manifest.unityVersion = Application.unityVersion;
            output.manifest.gpu = SystemInfo.graphicsDeviceName;
            output.manifest.revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION") ?? "unspecified";
            float timeScale = Time.timeScale;
            BattleFocus focus = null;
            bool focusEnabled = false;
            GameObject shotGo = null;
            try
            {
                _manager.SetLandscape(false);
                yield return Wait(0.5f);
                _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
                yield return Wait(0.5f);
                _hud.nextWaveButton.onClick.Invoke();
                yield return Wait(2f);
                Character character = _manager.player.character;
                CharacterView view = character.GetComponentInChildren<CharacterView>();
                List<GameObject> allies = _manager.entityManager.GetEntities(Entity.EntityType.Player);
                if (!view || allies.Count == 0)
                {
                    Debug.LogError("[OffscreenPlayerRun] Missing the Character view or a live ally.");
                    yield break;
                }
                Entity target = allies[0].GetComponent<Entity>();
                Camera camera = _manager.gameCamera;
                focus = Object.FindAnyObjectByType<BattleFocus>();
                focusEnabled = focus && focus.enabled;
                if (focus)
                {
                    focus.enabled = false;
                }
                Time.timeScale = 0f;
                proof.characterPosition = character.transform.position;
                proof.characterRenderers = character.GetComponentsInChildren<Renderer>().Length;
                proof.characterHasRig = view.rig != null;
                TrampleZone trample = view.GetComponent<TrampleZone>();
                proof.characterTrampleEnabled = trample && trample.enabled;
                view.TryGetCastPoint(out Vector3 origin);
                proof.castViewport = camera.WorldToViewportPoint(origin);
                proof.boardTufts = _manager.grass.tuftCount;
                proof.tuftHeight = GrassLayout.TuftHeight;
                proof.tuftWidth = GrassLayout.TuftWidth;
                output.manifest.inkScale = _manager.look.settings.inkScale;
                output.manifest.inkStart = _manager.look.settings.inkStart;
                output.manifest.grassNormalEdges = _manager.grass.lookMaterial.GetFloat("_HLNormalEdges") > 0f;
                output.manifest.grassCastsShadows = _manager.grass.tuftDraw.shadowCastingMode
                    != UnityEngine.Rendering.ShadowCastingMode.Off;
                yield return Wait(0.2f);
                yield return output.Capture(camera, "playfield", "offscreen-player");
                if (!output.isCaptured)
                {
                    yield break;
                }

                _manager.spellSink.Clear();
                target.health.OnAllConsumerProcessed.AddListener((owner, modifier, amount, critical) =>
                {
                    if (modifier.source == character.gameObject && amount > 0f)
                    {
                        proof.resolvedCharacterHeals++;
                    }
                });
                ApplyConsumerCharacterSkillFactory healFactory = RenderAssets.Load<ApplyConsumerCharacterSkillFactory>(
                    "Assets/Data/CharacterSkills/HealSingleTarget/HealSingleTarget.asset");
                ApplyConsumerCharacterSkill heal = (ApplyConsumerCharacterSkill)healFactory.Create();
                heal.ApplySkillOnTarget(character.gameObject, target.gameObject);
                yield return Wait(0.2f);
                proof.healLinks = Links(_manager.spellSink);
                yield return output.Capture(camera, "single-heal", "offscreen-player");
                if (!output.isCaptured)
                {
                    yield break;
                }

                _manager.spellSink.Clear();
                yield return Wait(0.1f);
                BuffCharacterSkillFactory buffFactory = RenderAssets.Load<BuffCharacterSkillFactory>(
                    "Assets/Data/CharacterSkills/SingleTargetBuffAttackRate/SingleTargetBuffAttackRate.asset");
                BuffCharacterSkill buff = (BuffCharacterSkill)buffFactory.Create();
                buff.ApplySkillOnTarget(character.gameObject, target.gameObject);
                target.buffManager.ForceUpdate();
                yield return Wait(0.2f);
                proof.statusLinks = Links(_manager.spellSink);
                proof.statusCount = _manager.spellSink.statusCount;
                yield return output.Capture(camera, "status-cast", "offscreen-player");
                if (!output.isCaptured)
                {
                    yield break;
                }

                _manager.spellSink.Clear();
                GameObject prefab = RenderAssets.Load<GameObject>("Assets/Prefabs/Projectiles/BulletSpeed.prefab");
                Vector3 logicalStart = character.transform.position + Vector3.up * 1.5f;
                shotGo = _manager.entityManager.SpawnProjectile(prefab, logicalStart, Quaternion.identity);
                Projectile projectile = shotGo.GetComponent<Projectile>();
                HomingProjectileBehaviour homing = shotGo.GetComponent<HomingProjectileBehaviour>();
                if (homing)
                {
                    homing.data.speed = 1.5f;
                }
                projectile.Init(character.gameObject, target.gameObject, new List<ABuffHandlerFactory>(),
                    new List<AConsumerFactory> { healFactory.data.consumer });
                FreeShot shot = shotGo.GetComponent<FreeShot>();
                proof.projectileDressedBySpawn = shot && shot.enabled
                    && shotGo.GetComponent<ProjectileVisualObserver>() && shot.fromScreen;
                if (!proof.projectileDressedBySpawn)
                {
                    Debug.LogError("[OffscreenPlayerRun] Player projectile was not dressed by SpawnDressing.");
                    yield break;
                }
                proof.projectileLogicalStart = projectile.transform.position;
                proof.projectileVisualStart = shot.visualPosition;
                proof.projectileEntryViewport = camera.WorldToViewportPoint(shot.visualPosition);
                // Let the actual homing behaviour advance between frozen native screen captures.
                for (int i = 0; i < 3; i++)
                {
                    Time.timeScale = 1f;
                    yield return Wait(0.45f);
                    Time.timeScale = 0f;
                    yield return Wait(0.15f);
                    if (!projectile || !shot)
                    {
                        Debug.LogError("[OffscreenPlayerRun] The proof projectile ended before its flight samples.");
                        yield break;
                    }
                    proof.projectileLogicalPositions.Add(projectile.transform.position);
                    proof.projectileVisualPositions.Add(shot.visualPosition);
                    yield return output.Capture(camera, "projectile-" + i.ToString("00"), "offscreen-player");
                    if (!output.isCaptured)
                    {
                        yield break;
                    }
                }
                proof.logicalCharacterUnchanged = character.transform.position == proof.characterPosition;
                proof.passed = proof.characterRenderers == 0 && !proof.characterHasRig
                    && !proof.characterTrampleEnabled && Mathf.Abs(proof.castViewport.x - 0.5f) < 0.001f
                    && proof.castViewport.y < 0f && proof.castViewport.z > 0f
                    && proof.boardTufts == GrassLayout.Generate(_manager.player.grid.width, _manager.player.grid.height,
                        _manager.player.grid.size, _manager.player.grid.transform.position, _manager.board.max.y,
                        _manager.grass.tuftBudget, _manager.grass.seed).Length
                    && proof.resolvedCharacterHeals > 0 && proof.healLinks > 0
                    && proof.statusCount > 0 && proof.statusLinks > 0 && proof.projectileDressedBySpawn
                    && proof.projectileLogicalStart == logicalStart && proof.projectileEntryViewport.y < 0f
                    && proof.projectileLogicalPositions.Count == 3 && proof.logicalCharacterUnchanged
                    && proof.projectileVisualPositions[0] != proof.projectileLogicalPositions[0];
            }
            finally
            {
                Time.timeScale = timeScale;
                if (focus)
                {
                    focus.enabled = focusEnabled;
                }
                if (shotGo)
                {
                    Object.Destroy(shotGo);
                }
                size.Dispose();
                proof.passed &= !output.hasFailure && output.manifest.frames.Count == 6;
                if (output.manifest.frames.Count > 0)
                {
                    StageMotionFrame first = output.manifest.frames[0];
                    foreach (StageMotionFrame frame in output.manifest.frames)
                    {
                        output.manifest.maximumCameraPositionError = Mathf.Max(
                            output.manifest.maximumCameraPositionError,
                            Vector3.Distance(first.cameraPosition, frame.cameraPosition));
                        output.manifest.maximumCameraRotationError = Mathf.Max(
                            output.manifest.maximumCameraRotationError,
                            Quaternion.Angle(first.cameraRotation, frame.cameraRotation));
                    }
                    output.manifest.cameraFixed = output.manifest.maximumCameraPositionError < 0.00001f
                        && output.manifest.maximumCameraRotationError < 0.001f;
                }
                output.manifest.isPassed = proof.passed;
                output.Write();
                File.WriteAllText(Path.Combine(folder, "proof.json"), JsonUtility.ToJson(proof, true));
                Debug.Log("[OffscreenPlayerRun] " + JsonUtility.ToJson(proof));
                StagePlay.Finish(this, proof.passed);
            }
        }

        static int Links(SpellVisualSink sink)
        {
            int count = 0;
            foreach (SpellEffect effect in sink.GetComponentsInChildren<SpellEffect>())
            {
                if (effect.recipe != null && effect.recipe.socket == EffectSocket.Link)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
