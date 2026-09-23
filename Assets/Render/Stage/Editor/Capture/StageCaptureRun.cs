using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    // Places two allies, starts the wave, casts on an ally then an enemy, and captures one still of the fight
    public class StageCaptureRun : AStageRun
    {
        public static readonly float CaptureAt = 8f;
        public static readonly int Width = 1080;
        public static readonly int Height = 1920;

        public bool isLandscape { get; set; }

        protected override IEnumerator Run()
        {
            _manager.SetLandscape(isLandscape);
            float started = Time.time;
            yield return new WaitForSeconds(0.5f);
            PlaceAllies(LoadAllies());
            _hud.nextWaveButton.onClick.Invoke();
            float nextCast = Time.time + 2f;
            while (Time.time - started < CaptureAt)
            {
                if (Time.time >= nextCast)
                {
                    nextCast = Time.time + 1f;
                    Entity.EntityType side = _heals == 0 ? Entity.EntityType.Player : Entity.EntityType.Computer;
                    GameObject targetGo = FirstOf(side);
                    CastOn(targetGo != null ? targetGo.GetComponent<Entity>() : null);
                }

                yield return EndOfFrame();
            }

            string path = StagePlay.CaptureFolder + (isLandscape ? "d2-stage-landscape.png" : "d2-stage-portrait.png");
            bool isCaptured = Capture(path);
            Debug.Log($"[StageCaptureRun] {path} attacks {_attacks} heals {_heals} zones {_maxZones}");
            StagePlay.Finish(isCaptured);
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

        // Portrait as his device autorotates, the aspect follows the target rather than the batchmode screen
        bool Capture(string path)
        {
            Camera camera = _manager.gameCamera;
            int width = isLandscape ? Height : Width;
            int height = isLandscape ? Width : Height;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.aspect = (float)width / height;
            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
            request.destination = target;
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Directory.CreateDirectory(StagePlay.CaptureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            return File.Exists(path);
        }
    }
}
