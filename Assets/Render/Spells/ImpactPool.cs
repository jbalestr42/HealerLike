using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{
    // The sink's short lived elements: impacts on units, beams between them and area footprints. It keeps the
    // newest MaxImpacts, hands each area pulse to the zones and draws the beams of a group cast once per frame.
    public class ImpactPool
    {
        public static readonly int MaxImpacts = 128;

        // Sizes an impact on a unit that has no health to read
        static readonly float defaultMaximumHealth = 100f;
        // A hit's blast through the grass, in world units: its least radius, what a whole health bar adds, and
        // the widening of a critical
        static readonly float shockMinRadius = 0.5f;
        static readonly float shockRadiusRange = 1f;
        static readonly float shockCriticalScale = 1.3f;

        // One recipient of a character's cast this frame
        struct Recipient
        {
            public GameObject target;
            public EffectFamily family;
        }

        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly Dictionary<GameObject, List<Recipient>> _groups = new Dictionary<GameObject, List<Recipient>>();
        Transform _parent;
        EffectVocabulary _vocabulary;
        PrimitiveMeshes _meshes;
        Material _material;
        ZoneRegistry _zones;
        Camera _camera;

        public int count { get { return _impacts.Count; } }

        public void Init(Transform parent, EffectVocabulary vocabulary, PrimitiveMeshes meshes, Material material,
                         ZoneRegistry zones, Camera camera)
        {
            _parent = parent;
            _vocabulary = vocabulary;
            _meshes = meshes;
            _material = material;
            _zones = zones;
            _camera = camera;
        }

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
                               bool isCritical)
        {
            Entity entity = target.GetComponent<Entity>();
            float maximum = defaultMaximumHealth;
            if (entity != null && entity.health != null)
            {
                maximum = entity.health.Max;
            }

            float amount = Mathf.Clamp01(Mathf.Abs(preClampAmount) / Mathf.Max(maximum, 1f));
            EffectRecipe recipe = EffectComposer.Impact(_vocabulary, resource, preClampAmount > 0f, amount);
            SpellEffect effect = SpellEffect.Create(recipe, _parent, _meshes, _material, target);
            if (effect == null)
            {
                return;
            }

            if (resource == ResourceKind.Health && source != null && source.GetComponent<Character>() != null)
            {
                AddRecipient(source, target, recipe.family);
            }

            EffectPlacement.Place(effect, _parent, EffectPlacement.Anchors(target));
            effect.transform.localScale *= recipe.scale;
            if (recipe.element == EffectElement.Burst)
            {
                // The star is flat, so it turns to the camera; a rim under it would read as a bar
                if (_camera != null)
                {
                    effect.transform.rotation = _camera.transform.rotation;
                }
            }
            else
            {
                effect.SetSide(Side(source));
            }

            if (isCritical)
            {
                effect.ShowCritical();
            }

            Add(effect.gameObject);
            // Only a hit on health blasts the grass; a spell's mana cost is not a blow
            if (_zones != null && resource == ResourceKind.Health && preClampAmount < 0f)
            {
                _zones.AddShock(target.transform.position, ShockRadius(amount, isCritical), HitShock(amount));
            }
        }

        // How hard a hit's blast throws the grass, a share of a landing's, harder as it takes more health
        public static float HitShock(float share)
        {
            return 0.3f + 0.35f * Mathf.Clamp01(share);
        }

        // The blast a hit throws through the grass, wider as it takes a larger share of the target's health
        public static float ShockRadius(float share, bool isCritical)
        {
            float radius = shockMinRadius + shockRadiusRange * Mathf.Clamp01(share);
            return isCritical ? radius * shockCriticalScale : radius;
        }

        public SpellEffect ShowLink(Vector3 start, Vector3 end, EffectFamily family, bool isContactThread,
                                    bool isScreenCast = false)
        {
            if (!RenderMath.IsFinite(start) || !RenderMath.IsFinite(end))
            {
                return null;
            }

            EffectRecipe recipe = EffectComposer.Link(_vocabulary, family);
            // The long player entry line crosses the navy ground; use Bane's authored lit tint for that line.
            if (isScreenCast && family == EffectFamily.Bane && recipe != null && recipe.palette)
            {
                recipe.colour = recipe.palette.baneLit;
            }
            SpellEffect effect = SpellEffect.Create(recipe, _parent, _meshes, _material, null);
            if (effect == null)
            {
                return null;
            }

            effect.SetEndpoints(start, end, isContactThread);
            Add(effect.gameObject);
            return effect;
        }

        // The footprint shows only while the sink does, the zones take the pulse either way and keep their own
        // lifetime; the entry's cycle is the pulse's length
        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength, bool isShown)
        {
            if (!ZonePacker.TryCreate(center, radius, kind, strength, 0f, out Zone zone))
            {
                return;
            }

            EffectRecipe recipe = EffectComposer.Area(_vocabulary, kind);
            if (recipe == null)
            {
                return;
            }

            SpellEffect footprint = null;
            if (isShown)
            {
                footprint = SpellEffect.Create(recipe, _parent, _meshes, _material, null);
            }

            if (footprint != null)
            {
                footprint.transform.position = center;
                footprint.transform.localScale = Vector3.one * radius;
                // A heal area plays for the player's side, a hostile one for the enemies'
                Entity.EntityType side = Entity.EntityType.Player;
                if (kind == ZoneKind.Hostile)
                {
                    side = Entity.EntityType.Computer;
                }

                footprint.SetSide(side);
                footprint.Advance(0f);
                Add(footprint.gameObject);
            }

            if (_zones != null)
            {
                _zones.AddPulse(kind, center, radius, zone.strength, recipe.cycleSeconds);
            }
        }

        // A character that reached two or more recipients of one family in one frame cast on a group, and each
        // recipient gets a beam. Recipients of one cast hit in different frames draw no beam.
        public void Flush(bool isShown)
        {
            if (isShown)
            {
                foreach (KeyValuePair<GameObject, List<Recipient>> group in _groups)
                {
                    if (group.Key != null)
                    {
                        Link(group.Key, group.Value);
                    }
                }
            }

            _groups.Clear();
        }

        // Forgets the impacts that ended on their own
        public void Sweep()
        {
            for (int i = _impacts.Count - 1; i >= 0; i--)
            {
                if (_impacts[i] == null)
                {
                    _impacts.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _groups.Clear();
            foreach (GameObject impact in _impacts)
            {
                SpellEffect.Dispose(impact);
            }

            _impacts.Clear();
        }

        void Link(GameObject caster, List<Recipient> recipients)
        {
            bool fromScreen = CharacterView.ScreenSource(caster);
            Vector3 start = EffectPlacement.Anchors(caster).castPoint;
            foreach (Recipient recipient in recipients)
            {
                if (recipient.target != null && (fromScreen || Count(recipients, recipient.family) >= 2))
                {
                    SpellEffect link = ShowLink(start, EffectPlacement.Anchors(recipient.target).bodyCentre,
                        recipient.family, false, fromScreen);
                    if (fromScreen && link)
                    {
                        link.SetCastSource(caster);
                    }
                }
            }
        }

        void Add(GameObject impact)
        {
            _impacts.Add(impact);
            if (_impacts.Count > MaxImpacts)
            {
                SpellEffect.Dispose(_impacts[0]);
                _impacts.RemoveAt(0);
            }
        }

        void AddRecipient(GameObject source, GameObject target, EffectFamily family)
        {
            if (!_groups.TryGetValue(source, out List<Recipient> recipients))
            {
                recipients = new List<Recipient>();
                _groups[source] = recipients;
            }

            foreach (Recipient recipient in recipients)
            {
                if (recipient.target == target)
                {
                    return;
                }
            }

            Recipient added = new Recipient();
            added.target = target;
            added.family = family;
            recipients.Add(added);
        }

        static int Count(List<Recipient> recipients, EffectFamily family)
        {
            int count = 0;
            foreach (Recipient recipient in recipients)
            {
                if (recipient.family == family)
                {
                    count++;
                }
            }

            return count;
        }

        // A caster on a side draws that side's rim, a sourceless impact the neutral one
        static Entity.EntityType Side(GameObject source)
        {
            Entity owner = null;
            if (source != null)
            {
                owner = source.GetComponent<Entity>();
            }

            if (owner == null)
            {
                return Entity.EntityType.None;
            }

            return owner.entityType;
        }
    }
}
