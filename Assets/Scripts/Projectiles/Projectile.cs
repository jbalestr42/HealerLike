using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BuffManager))]
public class Projectile : MonoBehaviour, IBuffable
{
    [HideInInspector] public UnityEvent<OnHitData> OnHit = new UnityEvent<OnHitData>();
    [HideInInspector] public UnityEvent OnUpdate = new UnityEvent();
    [HideInInspector] public UnityEvent<GameObject, GameObject> OnCollisionEnter = new UnityEvent<GameObject, GameObject>();

    GameObject _target;
    public GameObject target { get { return _target; } }

    GameObject _source;
    public GameObject source { get { return _source; } set { _source = value; } }

    GameObject _targetPoint;
    public GameObject targetPoint { get { return _targetPoint; } set { _targetPoint = value; } }

    List<AProjectileBehaviour> _projectileBehaviours;
    public List<AProjectileBehaviour> projectileBehaviours { get { return _projectileBehaviours; } set { _projectileBehaviours = value; } }

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    public void Init(GameObject source, GameObject target, List<ABuffFactory> projectileBehaviours)
    {
        _buffManager = GetComponent<BuffManager>();

        _source = source;
        _target = target;
        _targetPoint = target.GetComponent<Entity>().targetPoint;

        // Init buff from data
        foreach (ABuffFactory passive in projectileBehaviours)
        {
            AddBuff(passive, source, gameObject);
        }

        // Init projectile behaviours
        _projectileBehaviours = new List<AProjectileBehaviour>(GetComponents<AProjectileBehaviour>());
        foreach (AProjectileBehaviour projectileBehaviour in _projectileBehaviours)
        {
            projectileBehaviour.projectile = this;
            projectileBehaviour.Init(source);
        }
    }

    void Update()
    {
        OnUpdate.Invoke();

        if (ShouldDestroyProjectile())
        {
            Destroy(gameObject);
        }
	}

    public void SetTarget(GameObject newTarget)
    {
        _target = newTarget;
        _targetPoint = newTarget?.GetComponent<Entity>().targetPoint;
    }

    public virtual bool ShouldDestroyProjectile()
    {
        return target == null;
    }

    void OnTriggerEnter(Collider other)
    {
        OnCollision(other);
    }

    void OnTriggerStay(Collider other)
    {
        OnCollision(other);
    }

    void OnCollision(Collider other)
    {
        if (source == null)
        {
            SetTarget(null);
            return;
        }

        if (other.gameObject.GetComponent<IAttackable>() != null && source.GetComponent<IAttacker>() != null)
        {
            OnCollisionEnter.Invoke(other.gameObject, source);
        }
    }

    public void ApplyOnHit(GameObject currentTarget, GameObject source)
    {
        IAttackable attackable = currentTarget.GetComponent<IAttackable>();
        IAttacker attacker = source.GetComponent<IAttacker>();
        if (currentTarget == target && attackable != null && attacker != null)
        {
            // Destroy projectile if target is not reset by a behaviour during OnHit event
            SetTarget(null);

            OnHitData onHitData = new OnHitData();
            onHitData.resourceModifier.source = source;
            onHitData.attacker = attacker;
            onHitData.source = source;
            onHitData.attackable = attackable;
            onHitData.target = currentTarget;

            List<AConsumerFactory> onHitConsumers = attacker.GetOnHitConsumers();
            foreach (AConsumerFactory consumerFactory in onHitConsumers)
            {
                onHitData.resourceModifier.consumers.Add(consumerFactory.GetConsumer(source, currentTarget));
            }

            // Notify that something has been hit so all observers can add their own behaviour
            OnHit.Invoke(onHitData);

            // Process on hit data
            attackable.OnHit(onHitData);
        }
    }

    #region IBuffable

    public void AddBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target)
    {
        _buffManager.AddHandler(buffHandlerFactory, source, target);
    }

    public void AddBuff(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        _buffManager.Add(buffFactory, source, target);
    }

    public void RemoveBuff(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        _buffManager.Remove(buffFactory, source, target);
    }

    #endregion
}