using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public class FuseSkillData : SkillDataBase
{
    // Time of battle before the holder blows up by itself, drawn at random between the two for each fuse
    [Min(0.1f)]
    public float minDelay = 10f;
    [Min(0.1f)]
    public float maxDelay = 10f;
    // Dealt to every opponent when it blows up, on top of what its death triggers (e.g. the Kamikaze's Self-Destruct)
    [CreateDataButton]
    public AConsumerFactory explosion;
}

// Lit fuse (Lit Kamikaze): once the delay of battle is over, the holder blows up by itself, hurting every opponent,
// and dies
public class FuseSkill : ASkill<FuseSkillData>, ICooldownSkill
{
    float _elapsed = 0f;
    bool _hasExploded = false;
    // Drawn on the first tick of each fuse, negative until then
    float _delay = -1f;

    public bool hasExploded => _hasExploded;
    public float delay
    {
        get
        {
            if (_delay < 0f)
            {
                // Random.Range can land a hair off a range of one value: a fixed delay stays exactly that delay
                _delay = data.maxDelay > data.minDelay ? UnityEngine.Random.Range(data.minDelay, data.maxDelay) : data.minDelay;
            }
            return _delay;
        }
    }

    public override void UpdateBehaviour(GameObject source)
    {
        Tick(source, Time.deltaTime);
    }

    public void Tick(GameObject source, float deltaTime)
    {
        if (_hasExploded)
        {
            return;
        }

        _elapsed += deltaTime;
        if (_elapsed >= delay)
        {
            _hasExploded = true;
            Explode(source);
        }
    }

    void Explode(GameObject source)
    {
        Entity owner = source.GetComponent<Entity>();
        if (owner == null)
        {
            return;
        }

        if (data.explosion != null)
        {
            foreach (GameObject opponent in new List<GameObject>(EntityManager.instance.GetEntities(owner.GetTargetType())))
            {
                Entity entity = opponent != null ? opponent.GetComponent<Entity>() : null;
                if (entity != null)
                {
                    entity.health.AddResourceModifier(ResourceModifier.Create(data.explosion, source, opponent));
                }
            }
        }

        // Whatever its armor, invincibility or health: it dies, which triggers its own death effects
        ResourceModifier death = new ResourceModifier { source = source };
        death.consumers.Add(new RuntimeConsumer(-(owner.health.Max + owner.health.Value) * 10f, ignoreDamageReduction: true, ignoreConsumerPrevention: true, canBeCritical: false));
        owner.health.AddResourceModifier(death);
    }

    public override void Reset()
    {
        _elapsed = 0f;
        _hasExploded = false;
        _delay = -1f;
    }

    #region ICooldownSkill

    // Part of the fuse left to burn
    public float cooldownProgress => _hasExploded ? 0f : Mathf.Clamp01(1f - _elapsed / delay);

    public float cooldownDuration => delay;

    // The fuse burns faster: blows up on the next update once the delay is reached
    public void ReduceCooldown(float seconds)
    {
        if (!_hasExploded)
        {
            _elapsed = Mathf.Min(delay, _elapsed + Mathf.Max(0f, seconds));
        }
    }

    #endregion
}
