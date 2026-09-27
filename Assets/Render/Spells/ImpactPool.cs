using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
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

        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly HashSet<SupportLinkKey> _supportLinks = new HashSet<SupportLinkKey>();
        readonly GroupImpactLinks _groups = new GroupImpactLinks();
        Transform _parent;
        EffectVocabulary _vocabulary;
        PrimitiveMeshes _meshes;
        Material _material;
        ZoneRegistry _zones;
        Ground _ground;
        Camera _camera;

        public int count { get { return _impacts.Count; } }

        public void Init(Transform parent, EffectVocabulary vocabulary, PrimitiveMeshes meshes, Material material,
                         ZoneRegistry zones, Ground ground, Camera camera)
        {
            _parent = parent;
            _vocabulary = vocabulary;
            _meshes = meshes;
            _material = material;
            _zones = zones;
            _ground = ground;
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
                _groups.Add(source, target, recipe.family);
            }

            // Armless creature healers have no plant gesture. The event identifies owner/recipient only,
            // so direct and periodic positive health outcomes use that same primary anatomical source.
            if (resource == ResourceKind.Health && preClampAmount > 0f && CreatureSources.HasExplicit(source, true))
            {
                SpellEffect link = ShowSupportLink(EffectPlacement.Anchors(source).castPoint,
                    EffectPlacement.Anchors(target).bodyCentre, recipe.family, false);
                if (link) link.SetCastSource(source);
            }

            EffectPlacement.Place(effect, _parent, EffectPlacement.Anchors(target));
            if (recipe.presentation != null && recipe.presentation.billboard)
            {
                // The star is flat, so it turns to the camera; a rim under it would read as a bar
                EffectPlacement.FaceCamera(effect, _camera);
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
            SpellGround.Play(_ground, recipe, target.transform.position, Mathf.Lerp(.85f, 1.5f, amount), isCritical);
            // Only a hit on health blasts the grass; a spell's mana cost is not a blow
            if (_ground != null && recipe.entry.ground == null && resource == ResourceKind.Health && preClampAmount < 0f)
            {
                _ground.Play(_ground.vocabulary.hit, target.transform.position, ShockRadius(amount, isCritical),
                             HitShock(amount));
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

        public SpellEffect ShowSupportLink(Vector3 start, Vector3 end, EffectFamily family, bool isScreenCast = false)
        {
            return ShowSupportLink(start, end, family, isScreenCast, false);
        }

        public SpellEffect ShowSupportLink(Vector3 start, Vector3 end, EffectFamily family, bool isScreenCast,
                                            bool allowRepeat)
        {
            SupportLinkKey key = new SupportLinkKey(start, end, family);
            if (!allowRepeat && !_supportLinks.Add(key)) return null;
            if (allowRepeat) _supportLinks.Remove(key);
            SpellEffect link = ShowLink(start, end, family, false, isScreenCast);
            if (link == null && !allowRepeat) _supportLinks.Remove(key);
            return link;
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
            SpellGround.Line(_ground, recipe, start, end);
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

            SpellGround.Play(_ground, recipe, center, radius);
            if (_zones != null)
            {
                _zones.AddPulse(kind, center, radius, zone.strength, recipe.cycleSeconds);
            }
        }

        // A character that reached two or more recipients of one family in one frame cast on a group, and each
        // recipient gets a beam. Recipients of one cast hit in different frames draw no beam.
        public void Flush(bool isShown)
        {
            _groups.Flush(this, isShown);
            _supportLinks.Clear();
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
            _supportLinks.Clear();
            foreach (GameObject impact in _impacts)
            {
                SpellEffect.Dispose(impact);
            }

            _impacts.Clear();
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

        struct SupportLinkKey : System.IEquatable<SupportLinkKey>
        {
            readonly Vector3 start;
            readonly Vector3 end;
            readonly EffectFamily family;

            public SupportLinkKey(Vector3 start, Vector3 end, EffectFamily family)
            {
                this.start = start;
                this.end = end;
                this.family = family;
            }

            public bool Equals(SupportLinkKey other) => start == other.start && end == other.end && family == other.family;
            public override bool Equals(object obj) => obj is SupportLinkKey && Equals((SupportLinkKey)obj);
            public override int GetHashCode() => start.GetHashCode() ^ (end.GetHashCode() * 397) ^ (int)family;
        }
    }
}
