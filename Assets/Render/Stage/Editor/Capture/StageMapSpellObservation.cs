using System;
using System.Collections.Generic;
using UnityEngine;
using SpellEvidence = HealerLike.Render.Stage.StageMapRun.SpellEvidence;

namespace HealerLike.Render.Stage
{
    public class StageMapSpellObservation : IDisposable
    {
        readonly List<ResourceAttribute> _health = new List<ResourceAttribute>();
        readonly ResourceAttribute _mana;
        readonly GameObject _source;
        readonly Entity _target;
        readonly ApplyConsumerCharacterSkillData _data;
        readonly SpellEvidence _evidence;

        public StageMapSpellObservation(StageMapSession session, Character character, Entity target,
            ApplyConsumerCharacterSkillData data, SpellEvidence evidence)
        {
            _source = character.gameObject;
            _target = target;
            _data = data;
            _evidence = evidence;
            _mana = character.mana;
            session.output.Check(_mana != null && _mana.OnAllConsumerProcessed != null,
                "Observed spell has an initialized mana resource");
            foreach (GameObject entity in session.manager.entityManager.GetEntities(data.entityType))
            {
                ResourceAttribute health = entity.GetComponent<Entity>().health;
                session.output.Check(health != null && health.OnAllConsumerProcessed != null,
                    "Every spell observer has an initialized health resource");
                _health.Add(health);
            }

            foreach (ResourceAttribute health in _health)
            {
                health.OnAllConsumerProcessed.AddListener(OnHealth);
            }

            _mana.OnAllConsumerProcessed.AddListener(OnMana);
        }

        void OnHealth(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            // The resolver clears consumers before notifying observers. Source and multiplier
            // identify this cast window; value is the actual resolved health effect.
            if (modifier.source != _source || !Mathf.Approximately(modifier.multiplier, _data.multiplier))
            {
                return;
            }

            if (_data.isSingle && (_target == null || owner != _target.gameObject))
            {
                return;
            }

            if (value > 0f)
            {
                _evidence.positiveHealth += value;
            }

            if (value < 0f)
            {
                _evidence.negativeHealth -= value;
            }

            if (value != 0f)
            {
                _evidence.affectedEntities++;
            }
        }

        void OnMana(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (modifier.source == _source && value < 0f)
            {
                _evidence.manaConsumed -= value;
            }
        }

        public void Dispose()
        {
            foreach (ResourceAttribute health in _health)
            {
                if (health != null)
                {
                    health.OnAllConsumerProcessed.RemoveListener(OnHealth);
                }
            }

            _health.Clear();
            if (_mana != null)
            {
                _mana.OnAllConsumerProcessed.RemoveListener(OnMana);
            }
        }
    }
}
