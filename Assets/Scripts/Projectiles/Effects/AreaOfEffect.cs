using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOfEffect : MonoBehaviour
{
    float _radius = 1f;
    public float radius { get { return _radius; } set { _radius = value; } }

    GameObject _source;
    public GameObject source { get { return _source; } set { _source = value; } }

    GameObject _target;
    public GameObject target { get { return _target; } set { _target = value; } }

    // Applied on top of the source's on hit consumers (ex: the ones of the projectile that exploded)
    List<AConsumerFactory> _extraOnHitConsumers = new List<AConsumerFactory>();
    public List<AConsumerFactory> extraOnHitConsumers { get { return _extraOnHitConsumers; } set { _extraOnHitConsumers = value; } }

    void Start()
    {
        ATargetBehaviour targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.Nearest);
        targetBehaviour.targetCount = ATargetBehaviour.MaxTarget;

        transform.localScale = new Vector3(_radius, _radius, _radius);

        List<GameObject> targets = targetBehaviour.GetTargets(source, transform.position, _radius, source.GetComponent<Entity>().GetTargetType());
        foreach (GameObject nextTarget in targets)
        {
            // _target is already hit, we don't want to hit twice
            if (nextTarget != _target)
            {
                HitTarget(nextTarget);
            }
        }
    }

    public void HitTarget(GameObject nextTarget)
    {
        IAttackable attackable = nextTarget.GetComponent<IAttackable>();
        if (attackable == null)
        {
            return;
        }

        OnHitData onHitData = new OnHitData();
        onHitData.resourceModifier.source = source;
        onHitData.attacker = source.GetComponent<IAttacker>();
        onHitData.source = source;
        onHitData.attackable = attackable;
        onHitData.target = nextTarget;

        foreach (AConsumerFactory consumerFactory in onHitData.attacker.GetOnHitConsumers())
        {
            onHitData.resourceModifier.consumers.Add(consumerFactory.GetConsumer(source, nextTarget));
        }
        foreach (AConsumerFactory consumerFactory in _extraOnHitConsumers)
        {
            onHitData.resourceModifier.consumers.Add(consumerFactory.GetConsumer(source, nextTarget));
        }
        attackable.OnHit(onHitData);
    }
}
