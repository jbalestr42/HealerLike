using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // What a unit's view reads from the live entity every frame: its first target, how ready its skills are to fire
    // and how much health it has left
    public class UnitReadout
    {
        readonly List<ASkill> _skills = new List<ASkill>();
        readonly Dictionary<ASkill, ICooldownSkill> _cooldowns = new Dictionary<ASkill, ICooldownSkill>();
        readonly List<ASkill> _removedSkills = new List<ASkill>();
        Entity _entity;
        Vector3? _target;
        public Vector3? target
        {
            get { return _target; }
        }

        // One when the readiest skill can fire again
        float _readiness;
        public float readiness
        {
            get { return _readiness; }
        }

        float _healthFraction = 1f;
        public float healthFraction
        {
            get { return _healthFraction; }
        }

        public void Init(Entity entity)
        {
            _entity = entity;
            _cooldowns.Clear();
            RefreshSkills();
        }

        // Items add skills after spawn, so the skills are looked up again on every read
        public void Read()
        {
            if (!_entity)
            {
                return;
            }

            RefreshSkills();
            _target = null;
            TargetProvider provider = _entity.targetProvider;
            if (!provider)
            {
                provider = _entity.GetComponent<TargetProvider>();
            }

            if (provider)
            {
                List<GameObject> targets = provider.GetTargets();
                if (targets != null && targets.Count > 0 && targets[0])
                {
                    _target = targets[0].transform.position;
                }
            }

            _readiness = 0f;
            foreach (KeyValuePair<ASkill, ICooldownSkill> pair in _cooldowns)
            {
                if (!pair.Key || !pair.Key.isEnabled)
                {
                    continue;
                }

                float remaining = pair.Value.cooldownProgress;
                if (float.IsFinite(remaining))
                {
                    _readiness = Mathf.Max(_readiness, 1f - Mathf.Clamp01(remaining));
                }
            }

            ResourceAttribute health = _entity.health;
            _healthFraction = 1f;
            if (health && health.Max > 0)
            {
                _healthFraction = health.Value / health.Max;
            }
        }

        void RefreshSkills()
        {
            if (!_entity)
            {
                _cooldowns.Clear();
                return;
            }

            _entity.GetComponents(_skills);
            _removedSkills.Clear();
            foreach (ASkill skill in _cooldowns.Keys)
            {
                if (!skill || !_skills.Contains(skill))
                {
                    _removedSkills.Add(skill);
                }
            }

            foreach (ASkill skill in _removedSkills)
            {
                _cooldowns.Remove(skill);
            }

            foreach (ASkill skill in _skills)
            {
                if (!_cooldowns.ContainsKey(skill) && skill is ICooldownSkill cooldownSkill)
                {
                    _cooldowns.Add(skill, cooldownSkill);
                }
            }
        }
    }
}
