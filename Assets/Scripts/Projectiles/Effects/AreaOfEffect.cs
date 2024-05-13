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

    void Start()
    {
        ATargetBehaviour targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.Nearest);
        targetBehaviour.targetCount = ATargetBehaviour.MaxTarget;

        transform.localScale = new Vector3(_radius, _radius, _radius);

        List<GameObject> targets = targetBehaviour.GetTargets(transform.position, _radius, source.GetComponent<Entity>().GetTargetType());
        foreach (GameObject nextTarget in targets)
        {
            // _target is already hit, we don't want to hit twice
            if (nextTarget != _target)
            {
                IAttackable attackable = nextTarget.GetComponent<IAttackable>();
                if (attackable != null)
                {
                    OnHitData onHitData = new OnHitData();
                    onHitData.resourceModifier.source = source;
                    onHitData.attacker = source.GetComponent<IAttacker>();
                    onHitData.source = source;
                    onHitData.attackable = attackable;
                    onHitData.target = nextTarget;

                    List<AConsumerFactory> onHitConsumers = onHitData.attacker.GetOnHitConsumers();
                    foreach (AConsumerFactory consumerFactory in onHitConsumers)
                    {
                        onHitData.resourceModifier.consumers.Add(consumerFactory.GetConsumer(source, nextTarget));
                    }
                    attackable.OnHit(onHitData);
                }
            }
        }
    }
}
