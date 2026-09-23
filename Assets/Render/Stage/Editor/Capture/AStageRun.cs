using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A scripted player on his real game: it presses his buttons and places allies through his public calls,
    // and counts the attacks, heals and zones the render layer sees. Editor scripts cannot be components,
    // so StagePlay steps it from the editor update while the game plays.
    public abstract class AStageRun
    {
        protected static readonly string[] Allies =
        {
            "Assets/Data/Entities/NormalEntity/NormalEntity.asset",
            "Assets/Data/Entities/ChainLightningEntity/ChainLightningEntity.asset"
        };

        static readonly Vector3[] offsets = { Vector3.left, Vector3.back, Vector3.right, Vector3.forward, 2f * Vector3.left, 2f * Vector3.back };

        readonly HashSet<ResourceAttribute> _observed = new HashSet<ResourceAttribute>();
        int _castSlot;

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

        protected void PlaceAllies(List<EntityData> allies)
        {
            List<GameObject> enemies = _manager.entityManager.GetEntities(Entity.EntityType.Computer);
            Vector3 anchor = enemies.Count > 0 ? enemies[0].transform.position : Vector3.zero;
            GridManager grid = _manager.player.grid;
            int next = 0;
            foreach (EntityData data in allies)
            {
                GameObject allyGo = null;
                // SpawnEntity refuses an occupied cell, so the next offset is tried
                while (allyGo == null && next < offsets.Length)
                {
                    Vector3 position = grid.GetNearestWalkablePosition(anchor + offsets[next] * grid.size);
                    next++;
                    allyGo = _manager.entityManager.SpawnEntity(data, position, Entity.EntityType.Player);
                }

                Debug.Log($"[AStageRun] Placed {data.name}: {(allyGo != null ? allyGo.name : "refused")}");
            }
        }

        protected static List<EntityData> LoadAllies()
        {
            List<EntityData> allies = new List<EntityData>();
            foreach (string path in Allies)
            {
                allies.Add(AssetDatabase.LoadAssetAtPath<EntityData>(path));
            }
            return allies;
        }

        // Tries the next skill slot on the target, as a click on the button then on the target would
        protected void CastOn(Entity target)
        {
            Character character = _manager.player.character;
            if (target == null || character == null || character.skillSlots.Count == 0)
            {
                return;
            }

            CharacterSkillSlot slot = character.skillSlots[_castSlot++ % character.skillSlots.Count];
            InteractionManager.instance.CancelInteraction();
            slot.UseSkill();
            AInteraction interaction = InteractionManager.instance.GetInteraction();
            Camera camera = _manager.gameCamera;
            Vector3 aim = target.targetPoint != null ? target.targetPoint.transform.position : target.transform.position;
            Ray ray = new Ray(camera.transform.position, aim - camera.transform.position);
            if (interaction != null && Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interaction.GetLayerMask())
                && interaction.IsValidTarget(hit.transform.gameObject))
            {
                interaction.OnMouseClick(hit);
                return;
            }

            InteractionManager.instance.CancelInteraction();
        }
    }
}
