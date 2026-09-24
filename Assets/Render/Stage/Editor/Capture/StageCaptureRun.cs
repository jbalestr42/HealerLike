using System.Collections;
using System.IO;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Places two allies, starts the wave, casts on an ally then an enemy, and captures one still of the fight
    public class StageCaptureRun : AStageRun
    {
        public static readonly int Width = StageCalibration.PortraitWidth;
        public static readonly int Height = StageCalibration.PortraitHeight;
        public static readonly float CaptureAt = 8f;

        bool _isLandscape;

        public StageCaptureRun(bool isLandscape)
        {
            _isLandscape = isLandscape;
        }

        protected override IEnumerator Run()
        {
            _manager.SetLandscape(_isLandscape);
            float started = Time.time;
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            _hud.nextWaveButton.onClick.Invoke();
            float nextCast = Time.time + 2f;
            while (Time.time - started < CaptureAt)
            {
                // One heal, then strikes only until an enemy is hit, so the first wave is still fighting at the capture
                if (Time.time >= nextCast && (_heals == 0 || !IsAnyEnemyHit()))
                {
                    nextCast = Time.time + 1f;
                    Entity.EntityType side = _heals == 0 ? Entity.EntityType.Player : Entity.EntityType.Computer;
                    GameObject targetGo = FirstOf(side);
                    _player.CastOn(_manager, targetGo != null ? targetGo.GetComponent<Entity>() : null);
                }

                yield return NextFrame();
            }

            string path = StagePlay.CaptureFolder
                + (_isLandscape ? "render-stage-landscape.png" : "render-stage-portrait.png");
            bool isCaptured = Capture(path);
            Debug.Log($"[StageCaptureRun] {path} attacks {_attacks} heals {_heals} zones {_maxZones}");
            StagePlay.Finish(this, isCaptured);
        }

        GameObject FirstOf(Entity.EntityType side)
        {
            foreach (GameObject entityGo in _manager.entityManager.GetEntities(side))
            {
                if (entityGo != null)
                {
                    return entityGo;
                }
            }
            return null;
        }

        bool IsAnyEnemyHit()
        {
            foreach (GameObject entityGo in _manager.entityManager.GetEntities(Entity.EntityType.Computer))
            {
                Entity entity = entityGo != null ? entityGo.GetComponent<Entity>() : null;
                if (entity != null && entity.health != null && entity.health.Value < entity.health.Max)
                {
                    return true;
                }
            }

            return false;
        }

        // Portrait as the device autorotates, the aspect follows the target rather than the batchmode screen
        bool Capture(string path)
        {
            int width = Width;
            int height = Height;
            if (_isLandscape)
            {
                width = Height;
                height = Width;
            }

            Texture2D texture = StageReadback.Render(_manager.gameCamera, width, height);
            Directory.CreateDirectory(StagePlay.CaptureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);
            return File.Exists(path);
        }
    }
}
