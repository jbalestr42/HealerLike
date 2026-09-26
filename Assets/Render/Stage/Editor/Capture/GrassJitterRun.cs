using System.Collections;
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

        bool _isCasting;
        BattleFocus _focus;
        int _casts;
        int _targetedCasts;
        float _nextCast;

        protected override IEnumerator Run()
        {
            AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
            Character character = _manager != null && _manager.player != null ? _manager.player.character : null;
            if (character == null || character.buffManager == null || character.mana == null
                || character.skillSlots.Count == 0 || _manager.entityManager == null
                || _manager.entityManager.GetEntities(Entity.EntityType.Computer).Count == 0 || ascension == null
                || ascension.state != AscensionGameType.State.WaitForRoundToStart)
            {
                throw new System.InvalidOperationException("[GrassJitterRun] Expected an initialized character "
                    + "and a combat room prepared through normal game/map startup.");
            }
            Debug.Log($"[GrassJitterRun] Prepared combat room with {character.skillSlots.Count} character skills.");
            _manager.SetLandscape(false);
            yield return Wait(1f);
            Camera camera = _manager.gameCamera;
            _focus = Object.FindAnyObjectByType<BattleFocus>();
            _isCasting = false;
            _casts = 0;
            _targetedCasts = 0;
            _attacks = 0;
            _heals = 0;
            _nextCast = 0f;
            GrassJitterState state = new GrassJitterState(camera, _focus, _manager.environment.grass.strips);
            try
            {
                using (state)
                {
                    Pose pose = _manager.overviewPose;
                    state.UseOverview(pose, (float)width / height);
                    Time.captureDeltaTime = 1f / 60f;
                    yield return Measure(camera, "stock");
                    // The Editor runs at whatever rate it gets; the same at a fast, a slow and the real frame pace
                    Time.captureDeltaTime = 1f / 240f;
                    yield return Measure(camera, "240-fps");
                    Time.captureDeltaTime = 1f / 20f;
                    yield return Measure(camera, "20-fps");
                    Time.captureDeltaTime = 0f;
                    yield return Measure(camera, "real-time");
                    Time.captureDeltaTime = 1f / 60f;
                    state.SetWind(0f);
                    yield return Measure(camera, "environment-still");
                    state.RestoreEnvironment();
                    state.SetShadows(ShadowCastingMode.Off);
                    yield return Measure(camera, "environment-no-shadows");
                    state.RestoreEnvironment();

                    // The same during a wave, then with the decor plants' sway stopped
                    _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
                    Observe();
                    _hud.nextWaveButton.onClick.Invoke();
                    float deadline = Time.realtimeSinceStartup + 12f;
                    while (ascension.state != AscensionGameType.State.OnGoingBattle)
                    {
                        if (state.errorCount > 0 || Time.realtimeSinceStartup > deadline)
                        {
                            throw new System.InvalidOperationException("[GrassJitterRun] The prepared round "
                                + "did not reach real battle without errors.");
                        }
                        yield return NextFrame();
                    }
                    Debug.Log("[GrassJitterRun] Entered real battle through Next Wave.");
                    _isCasting = true;
                    state.UseOverview(pose, (float)width / height);
                    yield return Measure(camera, "battle");
                    yield return Gusts();
                    state.StopSways(Object.FindObjectsByType<EnvironmentSway>());
                    state.UseOverview(pose, (float)width / height);
                    yield return Measure(camera, "battle-decor-still");
                    state.RestoreSways();
                    yield return Film(camera);
                }
            }
            finally
            {
                _isCasting = false;
            }

            bool isPassed = state.errorCount == 0 && _targetedCasts > 0 && _attacks + _heals > 0;
            Debug.Log($"[GrassJitterRun] targeted cast attempts {_targetedCasts}, attacks {_attacks}, heals {_heals}, "
                + $"errors {state.errorCount}; passed {isPassed}");
            StagePlay.Finish(this, isPassed);
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
                yield return NextFrame();
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
            if (_focus != null)
            {
                _focus.enabled = true;
            }

            yield return Wait(1f);
            string folder = System.IO.Path.Combine(StagePlay.CaptureFolder, "grass-jitter");
            System.IO.Directory.CreateDirectory(folder);
            for (int frame = 0; frame < 8; frame++)
            {
                yield return NextFrame();
                Cast();
                Texture2D shot = StageReadback.Render(camera, width, height);
                StageCaptureTexture.SaveAndRelease(shot, System.IO.Path.Combine(folder, $"frame-{frame}.png"));
            }

            if (_focus != null)
            {
                _focus.enabled = false;
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
                    _targetedCasts++;
                    return;
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
                yield return NextFrame();
                Cast();
                Texture2D shot = StageReadback.Render(camera, width, height);
                Color32[] pixels = StageCaptureTexture.PixelsAndRelease(shot);
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
                        if (onBoard[i])
                        {
                            boardCount++;
                        }
                        else
                        {
                            environmentCount++;
                        }
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
