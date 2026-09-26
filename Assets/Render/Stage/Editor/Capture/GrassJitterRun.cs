using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // How much the grass flickers from frame to frame, on the board and in the environment around it: the idle
    // stage through the overview camera at a fixed 60 Hz, measured as the mean change of brightness between
    // frames and the share of pixels whose change reverses from one frame to the next. Repeated with the
    // environment's wind off, with its shadows off, then during a wave with and without the decor plants' sway,
    // to tell what the flicker comes from.
    public class GrassJitterRun : AStageRun
    {
        static readonly int width = 1080;
        static readonly int height = 1920;
        static readonly int frames = 24;
        // Brightness changes smaller than this are noise, in 0..1
        static readonly float threshold = 0.02f;

        protected override bool shouldStartGame { get { return false; } }

        bool _isCasting;
        BattleFocus focus;
        int _casts;
        float _nextCast;

        protected override IEnumerator Run()
        {
            _manager.SetLandscape(false);
            yield return Wait(1f);
            Camera camera = _manager.gameCamera;
            focus = Object.FindAnyObjectByType<BattleFocus>();
            if (focus != null)
            {
                focus.enabled = false;
            }

            Pose pose = _manager.overviewPose;
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            try
            {
                yield return Measure(camera, "stock");
                // The Editor runs at whatever rate it gets; the same at a fast, a slow and the real frame pace
                Time.captureDeltaTime = 1f / 240f;
                yield return Measure(camera, "240-fps");
                Time.captureDeltaTime = 1f / 20f;
                yield return Measure(camera, "20-fps");
                Time.captureDeltaTime = 0f;
                yield return Measure(camera, "real-time");
                Time.captureDeltaTime = 1f / 60f;
                SetEnvironment(field => field.windStrength = 0f);
                yield return Measure(camera, "environment-still");
                SetEnvironment(field => field.windStrength = 1f);
                SetEnvironment(field => field.tuftDraw.shadowCastingMode = ShadowCastingMode.Off);
                yield return Measure(camera, "environment-no-shadows");
                SetEnvironment(field => field.tuftDraw.shadowCastingMode = ShadowCastingMode.On);

                // The same during a wave, then with the decor plants' sway stopped
                _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
                _hud.nextWaveButton.onClick.Invoke();
                _isCasting = true;
                yield return Measure(camera, "battle");
                yield return Gusts();
                yield return Film(camera);
                EnvironmentSway[] sways = Object.FindObjectsByType<EnvironmentSway>();
                foreach (EnvironmentSway sway in sways)
                {
                    sway.enabled = false;
                }

                yield return Measure(camera, "battle-decor-still");
                foreach (EnvironmentSway sway in sways)
                {
                    sway.enabled = true;
                }
            }
            finally
            {
                Time.captureDeltaTime = capture;
            }

            StagePlay.Finish(this, true);
        }

        // How much of a fight the attack gusts keep a plant at the board's centre bent
        IEnumerator Gusts()
        {
            EnvironmentGust gust = _manager.gust;
            int frames = 0;
            int bent = 0;
            float strongest = 0f;
            float started = Time.time;
            while (gust != null && Time.time - started < 5f)
            {
                yield return null;
                Cast();
                float push = gust.Sample(Time.timeAsDouble, _manager.board.center).magnitude;
                frames++;
                bent += push > 0.1f ? 1 : 0;
                strongest = Mathf.Max(strongest, push);
            }

            Debug.Log($"[GrassJitterRun] gust: the board centre was pushed over a tenth on {bent} of {frames} frames, "
                      + $"at most {strongest:F2}");
        }

        // Eight consecutive frames through the game's own battle camera, kept whole, to look at tuft by tuft
        IEnumerator Film(Camera camera)
        {
            if (focus != null)
            {
                focus.enabled = true;
            }

            yield return Wait(1f);
            string folder = System.IO.Path.Combine(StagePlay.CaptureFolder, "grass-jitter");
            System.IO.Directory.CreateDirectory(folder);
            for (int frame = 0; frame < 8; frame++)
            {
                yield return null;
                Cast();
                Texture2D shot = StageReadback.Render(camera, width, height);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"frame-{frame}.png"), shot.EncodeToPNG());
                Object.Destroy(shot);
            }

            if (focus != null)
            {
                focus.enabled = false;
            }
        }

        // Every few frames the player casts on an ally then an enemy, so the fight keeps going while measured
        void Cast()
        {
            if (!_isCasting || Time.time < _nextCast)
            {
                return;
            }

            _nextCast = Time.time + 0.2f;
            Entity.EntityType side = _casts++ % 2 == 0 ? Entity.EntityType.Player : Entity.EntityType.Computer;
            foreach (GameObject entityGo in _manager.entityManager.GetEntities(side))
            {
                if (entityGo != null)
                {
                    _player.CastOn(_manager, entityGo.GetComponent<Entity>());
                    return;
                }
            }
        }

        void SetEnvironment(System.Action<GrassField> change)
        {
            foreach (GrassField field in _manager.environment.grass.strips)
            {
                if (field != null && field.tuftDraw != null)
                {
                    change(field);
                }
            }
        }

        IEnumerator Measure(Camera camera, string variant)
        {
            yield return Wait(0.3f);
            bool[] onBoard = BoardMask(camera);
            float[] previous = null;
            float[] change = null;
            float[] boardTotals = new float[2];
            float[] environmentTotals = new float[2];
            int boardCount = 0;
            int environmentCount = 0;
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
                Cast();
                Texture2D shot = StageReadback.Render(camera, width, height);
                Color32[] pixels = shot.GetPixels32();
                Object.Destroy(shot);
                float[] brightness = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    brightness[i] = (0.3f * pixels[i].r + 0.59f * pixels[i].g + 0.11f * pixels[i].b) / 255f;
                }

                if (previous != null)
                {
                    float[] next = new float[brightness.Length];
                    for (int i = 0; i < brightness.Length; i++)
                    {
                        next[i] = brightness[i] - previous[i];
                        // Only the lower two thirds, the grass; the sky and ridge sit above
                        if (i / width > height * 2 / 3)
                        {
                            continue;
                        }

                        float moved = Mathf.Abs(next[i]);
                        bool reversed = change != null && Mathf.Abs(change[i]) > threshold && moved > threshold
                                        && Mathf.Sign(change[i]) != Mathf.Sign(next[i]);
                        float[] totals = onBoard[i] ? boardTotals : environmentTotals;
                        totals[0] += moved;
                        totals[1] += reversed ? 1f : 0f;
                        if (onBoard[i]) boardCount++; else environmentCount++;
                    }

                    change = next;
                }

                previous = brightness;
            }

            Debug.Log($"[GrassJitterRun] {variant}: board change {boardTotals[0] / Mathf.Max(1, boardCount):F4} "
                      + $"flicker {boardTotals[1] / Mathf.Max(1, boardCount):P2}; environment change "
                      + $"{environmentTotals[0] / Mathf.Max(1, environmentCount):F4} "
                      + $"flicker {environmentTotals[1] / Mathf.Max(1, environmentCount):P2}");
        }

        // Pixels whose ray meets the ground inside the board rectangle
        bool[] BoardMask(Camera camera)
        {
            bool[] mask = new bool[width * height];
            Bounds board = _manager.board;
            camera.aspect = (float)width / height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Ray ray = camera.ViewportPointToRay(new Vector3((x + 0.5f) / width, (y + 0.5f) / height, 0f));
                    if (PointerBrush.Ground(ray, board.max.y, out Vector3 point))
                    {
                        mask[y * width + x] = point.x > board.min.x && point.x < board.max.x
                                              && point.z > board.min.z && point.z < board.max.z;
                    }
                }
            }

            return mask;
        }
    }
}
