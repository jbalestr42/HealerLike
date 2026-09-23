using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A scripted player on his real game: it presses his buttons and places allies through his public calls,
    // and counts the attacks, heals and zones the render layer sees. Editor scripts cannot be components,
    // so StagePlay steps it from the editor update while the game plays.
    public abstract class AStageRun
    {
        readonly HashSet<ResourceAttribute> _observed = new HashSet<ResourceAttribute>();
        protected StagePlayer _player = new StagePlayer();

        protected RenderManager _manager;
        protected GameHUD _hud;
        protected int _attacks;
        protected int _heals;
        protected int _maxZones;

        readonly Stack<IEnumerator> _steps = new Stack<IEnumerator>();

        public void Begin()
        {
            _steps.Clear();
            _steps.Push(Start());
        }

        // Runs until the next yield, a yielded enumerator runs first
        public void Step()
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

        protected static IEnumerator Wait(float seconds)
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
                    StagePlay.Finish(false);
                    yield break;
                }

                _manager = Object.FindAnyObjectByType<RenderManager>();
                GameView gameView = Object.FindAnyObjectByType<GameView>();
                _hud = gameView != null ? gameView.gameHUD : null;
                yield return null;
            }

            _hud.startGameButton.onClick.Invoke();
            yield return Run();
        }

        protected abstract IEnumerator Run();

        // The editor update runs between frames, after the manager's LateUpdate
        protected IEnumerator NextFrame()
        {
            Observe();
            yield return null;
        }

        protected void Observe()
        {
            foreach (ResourceAttribute resource in Object.FindObjectsByType<ResourceAttribute>(FindObjectsSortMode.None))
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
