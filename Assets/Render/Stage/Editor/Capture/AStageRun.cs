using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A scripted player on the real game: it presses its buttons and places allies through its public calls,
    // and counts the attacks, heals and zones the render layer sees. StagePlay drives its enumerators;
    // runs that inspect screen coordinates use the editor-only game-frame component.
    public abstract class AStageRun
    {
        readonly HashSet<ResourceAttribute> _observed = new HashSet<ResourceAttribute>();
        protected StagePlayer _player = new StagePlayer();

        protected RenderManager _manager;
        public RenderManager manager { get { return _manager; } }
        protected GameHUD _hud;
        protected int _attacks;
        protected int _heals;
        protected int _maxZones;

        readonly Stack<IEnumerator> _steps = new Stack<IEnumerator>();
        bool _hasFailed;
        public bool hasFailed { get { return _hasFailed; } }

        public void Begin()
        {
            Begin(Start());
        }

        public void Begin(IEnumerator steps)
        {
            _hasFailed = false;
            _steps.Clear();
            _steps.Push(steps);
        }

        // Runs until the next yield, a yielded enumerator runs first
        public void Step()
        {
            if (_hasFailed)
            {
                return;
            }
            try
            {
                while (_steps.Count > 0)
                {
                    IEnumerator step = _steps.Peek();
                    if (!step.MoveNext())
                    {
                        _steps.Pop();
                        continue;
                    }
                    if (step.Current is IEnumerator inner)
                    {
                        _steps.Push(inner);
                        continue;
                    }
                    return;
                }
            }
            catch (System.Exception error)
            {
                _hasFailed = true;
                // Dispose every parent iterator too, so a failed child cannot resume into a passing result.
                while (_steps.Count > 0)
                {
                    System.IDisposable step = _steps.Pop() as System.IDisposable;
                    try
                    {
                        step?.Dispose();
                    }
                    catch (System.Exception cleanupError)
                    {
                        Debug.LogException(cleanupError);
                    }
                }
                OnFailed(error);
            }
        }

        protected virtual void OnFailed(System.Exception error)
        {
            Debug.LogException(error);
            StagePlay.Finish(this, false);
        }

        public static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        IEnumerator Start()
        {
            // The launcher loads Main, the manager attaches on sceneLoaded
            float deadline = Time.realtimeSinceStartup + 30f;
            while (_hud == null || _manager == null || _manager.entityManager == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogError("[AStageRun] The render manager never attached to Main.");
                    StagePlay.Finish(this, false);
                    yield break;
                }

                _manager = Object.FindAnyObjectByType<RenderManager>();
                GameView gameView = Object.FindAnyObjectByType<GameView>();
                _hud = gameView != null ? gameView.gameHUD : null;
                yield return null;
            }

            if (shouldStartGame)
            {
                _hud.startGameButton.onClick.Invoke();
            }
            yield return Run();
        }

        protected virtual bool shouldStartGame { get { return true; } }

        protected abstract IEnumerator Run();

        // The editor update runs between frames, after the manager's LateUpdate
        public IEnumerator NextFrame()
        {
            Observe();
            yield return null;
        }

        protected void Observe()
        {
            foreach (ResourceAttribute resource in
                Object.FindObjectsByType<ResourceAttribute>(FindObjectsSortMode.None))
            {
                if (_observed.Add(resource))
                {
                    resource.OnAllConsumerProcessed.AddListener(OnProcessed);
                }
            }

            _maxZones = Mathf.Max(_maxZones, _manager.zones.count);
        }

        void OnProcessed(GameObject owner, ResourceModifier modifier, float value, bool isCritical)
        {
            _attacks += value < 0f ? 1 : 0;
            _heals += value > 0f ? 1 : 0;
        }
    }
}
