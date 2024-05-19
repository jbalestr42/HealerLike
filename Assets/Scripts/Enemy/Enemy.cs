using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuffManager), typeof(AttributeManager))]
public class Enemy : MonoBehaviour, IAttackable, ISelectable, IBuffable, IMarkable
{
    [SerializeField] EnemyHUD _hud;

    EnemyData _data;
    public EnemyData data { get { return _data; } set { _data = value; } }

    AttributeManager _attributeManager;
    public AttributeManager attributeManager { get { return _attributeManager; } }

    ResourceAttribute _health;
    public ResourceAttribute health { get { return _health; } }

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    GameObject _targetPoint;
    public GameObject targetPoint { get { return _targetPoint; } set { _targetPoint = value; } }

    public void Init()
    {
        _buffManager = GetComponent<BuffManager>();

        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            Attribute att = new Attribute(attribute.Value);
            _attributeManager.Add(attribute.Key, att);
        }

        _health = GetComponent<ResourceAttribute>();
        _health.Init(AttributeType.HealthMax);
        _health.OnValueChanged.AddListener(OnHealthChanged);

        // Init self buff from data
        foreach (ABuffFactory passive in _data.passives)
        {
            AddBuff(passive, gameObject, gameObject);
        }

        // Init skills from data
        foreach (ASkillFactory skillFactory in _data.skillFactories)
        {
            skillFactory.AddSkill(gameObject);
        }
        
        GameObject model = Instantiate(_data.model, transform);
        _targetPoint = model.GetComponentInChildren<SkillTargetPointTag>()?.gameObject ?? gameObject;

        EntityManager.instance.OnEnemySpawned.Invoke(this);
    }

    public void OnHealthChanged(ResourceAttribute health)
    {
        _hud.SetHealth(health.Value, health.Max);
        if (health.Value <= 0f)
        {
            EntityManager.instance.DestroyEnemy(gameObject, false);
        }
    }

    #region IMakable

    public void Mark()
    {
        _hud.ShowMark(true);
    }

    public void UnMark()
    {
        _hud.ShowMark(false);
    }

    #endregion

    #region IAttackable

    public void OnHit(ResourceModifier resourceModifier)
    {
        health.AddResourceModifier(resourceModifier);
    }

    public void OnHit(OnHitData onHitData)
    {
        OnHit(onHitData.resourceModifier);

        if (onHitData.attacker != null)
        {
            List<ABuffHandlerFactory> onHitEffects = onHitData.attacker.GetOnHitEffects();
            foreach (ABuffHandlerFactory onHitEffect in onHitEffects)
            {
                AddBuffHandler(onHitEffect, onHitData.source, gameObject);
            }
        }
    }

    #endregion

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

    #region ISelectable

    public void Select()
    {
    }

    public void UnSelect()
    {
    }

    #endregion
}