using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{
    // Draws what the observers publish from the effect vocabulary: statuses through its StatusPool, impacts, links
    // and area pulses through its ImpactPool
    public class SpellVisualSink : MonoBehaviour, ISpellVisualSink
    {
        [SerializeField] EffectVocabulary _vocabulary;
        [SerializeField] Material _material;

        readonly StatusPool _statuses = new StatusPool();
        readonly ImpactPool _impacts = new ImpactPool();

        public int statusCount { get { return _statuses.count; } }

        // Counts until the next Tick sweeps the impacts that ended
        public int impactCount { get { return _impacts.count; } }

        void OnEnable()
        {
            // The registry may keep this sink across disable and enable
            Clear();
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }

        public void Init(RenderManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[SpellVisualSink] Init needs the RenderManager.");
                return;
            }

            Init(manager.spellLooks, manager.meshes, manager.zones, manager.gameCamera);
        }

        // The shared looks and meshes come from the manager, the vocabulary and the material from the prefab; the
        // zones take the area pulses and the bursts turn to the camera
        public void Init(SpellLooks looks, PrimitiveMeshes meshes, ZoneRegistry zones, Camera camera)
        {
            _statuses.Init(transform, _vocabulary, looks, meshes, _material);
            _impacts.Init(transform, _vocabulary, meshes, _material, zones, camera);
        }

        // The RenderManager ticks it in its LateUpdate, once every observer has published in Update
        public void Tick()
        {
            _impacts.Flush(isActiveAndEnabled);
            _statuses.Tick();
            _impacts.Sweep();
        }

        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory)
        {
            SpellEffect effect = _statuses.Get(target, factory);
            if (effect == null)
            {
                return null;
            }

            return effect.gameObject;
        }

        public SpellEffect GetElement(GameObject target, EffectElement element)
        {
            return _statuses.Get(target, element);
        }

        // The sink drops statuses on its own when it is cleared or their target goes, so an observer publishes
        // again what is no longer shown
        public bool IsShowing(GameObject target, ABuffHandlerFactory factory)
        {
            return _statuses.Get(target, factory) != null;
        }

        public void SetCharges(GameObject target, float charges)
        {
            if (target == null || CharacterView.ScreenSource(target))
            {
                return;
            }

            float shownCharges = 0f;
            if (isActiveAndEnabled)
            {
                shownCharges = charges;
            }

            _statuses.SetCharges(target, shownCharges);
        }

        public SpellEffect ShowContactLink(Vector3 previousContact, Vector3 contact)
        {
            if (!isActiveAndEnabled)
            {
                return null;
            }

            return _impacts.ShowLink(previousContact, contact, EffectFamily.Damage, true);
        }

        public void Clear()
        {
            _statuses.Clear();
            _impacts.Clear();

            // The tails of closed statuses play out under the sink
            foreach (SpellEffect tail in GetComponentsInChildren<SpellEffect>())
            {
                SpellEffect.Dispose(tail.gameObject);
            }
        }

        #region ISpellVisualSink

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
                               bool isCritical)
        {
            if (!isActiveAndEnabled || target == null || CharacterView.ScreenSource(target)
                || !float.IsFinite(preClampAmount) || preClampAmount == 0f)
            {
                return;
            }

            _impacts.ShowImpact(source, target, resource, preClampAmount, isCritical);
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
                              float elapsedSeconds, float durationSeconds)
        {
            if (!isActiveAndEnabled || target == null || factory == null || CharacterView.ScreenSource(target))
            {
                return;
            }

            if (stacks <= 0)
            {
                RemoveStatus(source, target, factory);
                return;
            }

            if (float.IsFinite(elapsedSeconds) && !float.IsNaN(durationSeconds))
            {
                int previousStacks = _statuses.Stacks(target, factory);
                _statuses.Set(source, target, factory, stacks, elapsedSeconds, durationSeconds);
                SpellEffect status = _statuses.Get(target, factory);
                if (stacks > previousStacks && status && CharacterView.ScreenSource(source))
                {
                    SpellEffect link = _impacts.ShowLink(EffectPlacement.Anchors(source).castPoint,
                        EffectPlacement.Anchors(target).bodyCentre, status.recipe.family, false);
                    if (link)
                    {
                        link.SetCastSource(source);
                    }
                }
            }
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (!ReferenceEquals(target, null) && !ReferenceEquals(factory, null))
            {
                _statuses.Remove(target, factory);
            }
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
            _impacts.PulseArea(center, radius, kind, strength, isActiveAndEnabled);
        }

        #endregion
    }
}
