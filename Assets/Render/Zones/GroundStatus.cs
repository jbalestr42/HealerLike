using System.Collections.Generic;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // What a creature suffers, written into the grass under it: while it is poisoned the grass around it sickens,
    // while it is slowed the grass frosts over and stiffens. Read from its buff manager's start and stop events.
    public class GroundStatus : MonoBehaviour
    {
        // In cells: the patch round a poisoned or slowed creature
        public static readonly float PatchRadius = 0.9f;

        GroundHandle _blight;
        GroundHandle _frost;
        readonly HashSet<BuffManager.BuffHandlerData> _poisons = new HashSet<BuffManager.BuffHandlerData>();
        readonly HashSet<BuffManager.BuffHandlerData> _slows = new HashSet<BuffManager.BuffHandlerData>();
        Entity _entity;
        BuffManager _buffs;
        Collider _hold;
        float _cellSize = 1f;

        public bool isPoisoned { get { return _poisons.Count > 0; } }
        public bool isSlowed { get { return _slows.Count > 0; } }

        public void Init(Entity entity, Ground ground, float cellSize)
        {
            Unbind();
            ReleaseHandles();
            if (ground != null)
            {
                _blight = ground.Hold(ground.vocabulary.blight);
                _frost = ground.Hold(ground.vocabulary.frost);
            }

            _entity = entity;
            _buffs = entity != null ? entity.buffManager : null;
            _hold = EntityHold.Find(entity);
            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            if (_buffs != null)
            {
                _buffs.OnBuffHandlerStarted.AddListener(OnStarted);
                _buffs.OnBuffHandlerStopped.AddListener(OnStopped);
            }
        }

        public void OnStarted(BuffManager.BuffHandlerData data)
        {
            if (data == null || data.buffHandlerFactory == null)
            {
                return;
            }

            if (IsPoison(data.buffHandlerFactory, IsSameSide(data.source)))
            {
                _poisons.Add(data);
            }

            if (IsSlow(data.buffHandlerFactory))
            {
                _slows.Add(data);
            }
        }

        public void OnStopped(BuffManager.BuffHandlerData data)
        {
            _poisons.Remove(data);
            _slows.Remove(data);
        }

        // A periodic harm: the Rot family of the effect grammar
        public static bool IsPoison(ABuffHandlerFactory factory, bool isSameSide)
        {
            return factory != null && EffectDerivation.Family(factory, isSameSide) == EffectFamily.Rot;
        }

        public static bool IsSlow(ABuffHandlerFactory factory)
        {
            if (factory == null || factory.buffFactoryList == null)
            {
                return false;
            }

            foreach (ABuffFactory buff in factory.buffFactoryList)
            {
                if (buff is SlowModifierFactory)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsSameSide(GameObject source)
        {
            Entity sourceEntity = source != null ? source.GetComponent<Entity>() : null;
            return sourceEntity != null && _entity != null && sourceEntity.entityType == _entity.entityType;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            bool isShown = isActiveAndEnabled && !EntityHold.IsHeld(_hold);
            Show(_blight, isShown && isPoisoned);
            Show(_frost, isShown && isSlowed);
        }

        void Show(GroundHandle handle, bool isShown)
        {
            if (handle == null)
            {
                return;
            }

            if (!isShown)
            {
                handle.Hide();
                return;
            }

            handle.Show(transform.position, PatchRadius * _cellSize, 1f);
        }

        void ReleaseHandles()
        {
            _blight?.Release();
            _frost?.Release();
            _blight = null;
            _frost = null;
        }

        void Unbind()
        {
            if (_buffs != null)
            {
                _buffs.OnBuffHandlerStarted.RemoveListener(OnStarted);
                _buffs.OnBuffHandlerStopped.RemoveListener(OnStopped);
            }

            _buffs = null;
            _poisons.Clear();
            _slows.Clear();
        }

        void OnDisable()
        {
            _blight?.Hide();
            _frost?.Hide();
        }

        void OnDestroy()
        {
            Unbind();
            ReleaseHandles();
        }
    }
}
