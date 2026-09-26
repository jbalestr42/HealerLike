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
        bool _isStepping;
        bool _isStopping;
        bool _stopRequested;
        public bool hasFailed { get { return _hasFailed; } }

        public void Begin()
        {
            Begin(Start());
        }

        public void Begin(IEnumerator steps)
        {
            if (_isStepping || _isStopping)
            {
                Debug.LogError("[AStageRun] Begin must be called outside a running step or cleanup.");
                return;
            }

            Stop();
            _hasFailed = false;
            _steps.Push(steps);
        }

        // Runs until the next yield, a yielded enumerator runs first
        public void Step()
        {
            if (_hasFailed || _isStepping)
            {
                return;
            }

            _isStepping = true;
            System.Exception failure = null;
            try
            {
                while (_steps.Count > 0)
                {
                    IEnumerator step = _steps.Peek();
                    bool hasNext = step.MoveNext();
                    if (_stopRequested)
                    {
                        return;
                    }

                    if (!hasNext)
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
                _stopRequested = true;
                failure = error;
            }
            finally
            {
                _isStepping = false;
                if (_stopRequested)
                {
                    Stop();
                }
            }

            if (failure != null)
            {
                OnFailed(failure);
            }
        }

        // Stop can be requested by Finish inside MoveNext; cleanup waits until that call returns.
        public void Stop()
        {
            if (_isStopping)
            {
                return;
            }

            _stopRequested = true;
            if (_isStepping)
            {
                return;
            }

            _isStopping = true;
            try
            {
                while (_steps.Count > 0)
                {
                    System.IDisposable step = _steps.Pop() as System.IDisposable;
                    try
                    {
                        if (step != null)
                        {
                            step.Dispose();
                        }
                    }
                    catch (System.Exception cleanupError)
                    {
                        Debug.LogException(cleanupError);
                    }
                }

                StopObserving();
            }
            finally
            {
                _stopRequested = false;
                _isStopping = false;
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
                // Legacy visual captures use a Toolkit navigation submit, not a measured touch gesture.
                StageInterfaceActions actions = new StageInterfaceActions
                    { ui = Object.FindAnyObjectByType<ToolkitGameUI>() };
                yield return StageMapActions.SelectFirst(actions, false);
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
                Object.FindObjectsByType<ResourceAttribute>())
            {
                if (_observed.Add(resource))
                {
                    resource.OnAllConsumerProcessed.AddListener(OnProcessed);
                }
            }

            _maxZones = Mathf.Max(_maxZones, _manager.zones.count);
        }

        public void StopObserving()
        {
            foreach (ResourceAttribute resource in _observed)
            {
                if (resource != null)
                {
                    resource.OnAllConsumerProcessed.RemoveListener(OnProcessed);
                }
            }

            _observed.Clear();
        }

        void OnProcessed(GameObject owner, ResourceModifier modifier, float value, bool isCritical)
        {
            _attacks += value < 0f ? 1 : 0;
            _heals += value > 0f ? 1 : 0;
        }
    }
}
