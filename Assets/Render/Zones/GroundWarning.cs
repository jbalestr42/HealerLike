using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // An enemy's area attack warned in the grass: as the skill's cooldown runs out, the grass it will land on
    // shivers, harder the closer the blast. The area lands on the caster at its range, so that is where it shows.
    public class GroundWarning : MonoBehaviour
    {
        // The last share of the cooldown the warning shows over
        public static readonly float WarningShare = 0.3f;

        GroundHandle _handle;
        AreaOfEffectSkill _skill;
        Entity _entity;

        public bool hasSkill { get { return _skill != null; } }

        // Only enemies holding an area skill warn
        public void Init(Entity entity, Ground ground)
        {
            _entity = entity;
            _skill = null;
            if (entity != null && entity.entityType == Entity.EntityType.Computer)
            {
                _skill = entity.GetComponentInChildren<AreaOfEffectSkill>();
            }

            _handle?.Release();
            _handle = _skill != null && ground != null ? ground.Hold(ground.vocabulary.warning) : null;
        }

        public bool isShown { get { return _handle != null && _handle.isShown; } }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            // A skill that is not running keeps its last cooldown; only a live one is about to fire
            bool isLive = _skill != null && _skill.isEnabled && isActiveAndEnabled;
            float strength = isLive ? Strength(_skill.cooldownProgress) : 0f;
            float radius = Range();
            if (_handle == null)
            {
                return;
            }

            if (strength <= 0f || radius <= 0f)
            {
                _handle.Hide();
                return;
            }

            _handle.Show(_entity.transform.position, radius, strength);
        }

        // Nothing until the last WarningShare of the cooldown, then rising to full as it runs out. A skill waiting
        // at zero without a target shows nothing, as it may wait there for ever.
        public static float Strength(float cooldownProgress)
        {
            if (!float.IsFinite(cooldownProgress) || cooldownProgress <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - cooldownProgress / WarningShare);
        }

        float Range()
        {
            AttributeManager attributes = _entity != null ? _entity.attributeManager : null;
            if (attributes == null || !attributes.Has(AttributeType.Range))
            {
                return 0f;
            }

            return Mathf.Max(0f, attributes.Get(AttributeType.Range).Value);
        }

        void OnDisable()
        {
            _handle?.Hide();
        }

        void OnDestroy()
        {
            _handle?.Release();
            _handle = null;
        }
    }
}
