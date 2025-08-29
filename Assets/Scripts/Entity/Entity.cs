using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;

[RequireComponent(typeof(BuffManager), typeof(AttributeManager))]
public class Entity : MonoBehaviour, IAttackable, IAttacker, IBuffable, IMarkable
{
    public UnityEvent<bool> OnMarkChanged = new UnityEvent<bool>();

    public enum EntityType
    {
        None,
        Player,
        Computer
    }
    public EntityType entityType { get; set; }

    ResourceAttribute _health;
    public ResourceAttribute health { get { return _health; } }
    List<AConsumerFactory> _onHitConsumers = new List<AConsumerFactory>();
    List<ABuffHandlerFactory> _onHitEffects = new List<ABuffHandlerFactory>();
    public List<ABuffHandlerFactory> onHitEffects { get { return _onHitEffects; } set { _onHitEffects = value; } }
    List<ABuffHandlerFactory> _projectileBehaviours = new List<ABuffHandlerFactory>();
    public List<ABuffHandlerFactory> projectileBehaviours { get { return _projectileBehaviours; } set { _projectileBehaviours = value; } }

    EntityData _data;
    public EntityData data { get { return _data; } set { _data = value; } }

    AttributeManager _attributeManager;
    public AttributeManager attributeManager { get { return _attributeManager; } set { _attributeManager = value; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    TargetProvider _targetProvider;
    public TargetProvider targetProvider { get { return _targetProvider; } }

    EntityModel _model;
    public EntityModel model => _model;
    public SkillSource skillStartPoint { get { return _model.GetSourcePoint(); } } 

    GameObject _targetPoint;
    public GameObject targetPoint => _targetPoint;

    List<ASkill> _skills = new List<ASkill>();

    public void Init()
    {
        _buffManager = GetComponent<BuffManager>();
        _targetProvider = GetComponent<TargetProvider>();

        // Init attributes from data
        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            _attributeManager.Add(attribute.Key, new Attribute(attribute.Value));
        }

        _health = gameObject.AddComponent<ResourceAttribute>();
        _health.Init(AttributeType.HealthMax);
        _health.OnValueChanged.AddListener(OnHealthChanged);

        // Init model from data
        GameObject model = Instantiate(_data.model, transform);
        _model = model.GetComponent<EntityModel>();
        Assert.IsNotNull(_model, "The model must have a 'EntityModel' component");
        _model.Init(this);
        _targetPoint = model.GetComponentInChildren<SkillTargetPointTag>()?.gameObject ?? gameObject;

        // Init self buff from data
        foreach (ABuffHandlerFactory passive in _data.passives)
        {
            AddBuffHandler(passive, gameObject, gameObject);
        }

        // Init on hit effects from data
        foreach (ABuffHandlerFactory onHitEffect in _data.onHitEffects)
        {
            AddOnHitEffect(onHitEffect);
        }

        // Init skills from data
        foreach (ASkillFactory skillFactory in _data.skillFactories)
        {
            ASkill skill = skillFactory.AddSkill(gameObject);
            _skills.Add(skill);
        }

        // Register to inventory events
        _inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);

        EntityManager.instance.OnEntitySpawned.Invoke(this);

        // Disable the unit since we are not in combat
        Enable(false);
    }

    void OnHealthChanged(ResourceAttribute health)
    {
        if (health.Value <= 0f)
        {
            EntityManager.instance.DestroyEntity(gameObject, entityType);
        }
    }

    public void Enable(bool isEnabled)
    {
        _targetProvider.isEnabled = isEnabled;
        _buffManager.isEnabled = isEnabled;

        foreach (ASkill skill in _skills)
        {
            skill.isEnabled = isEnabled;
        }
    }

    public void Reset()
    {
        _targetProvider.Reset();
        _buffManager.Reset();

        foreach (ASkill skill in _skills)
        {
            skill.Reset();
        }
    }

    public EntityType GetTargetType()
    {
        if (entityType == EntityType.Player)
        {
            return EntityType.Computer;
        }
        else if (entityType == EntityType.Computer)
        {
            return EntityType.Player;
        }
        return EntityType.None;
    }

    #region Inventory

    public void OnItemAdded(InventoryItemData itemData, bool isNewItem)
    {
        // If the item is in the first 2 slots, we stack it to be more powerfull
        int stacks = GetStackCount(itemData.inventoryIndex);
        for (int i = 0; i < stacks; i++)
        {
            itemData.item.Equip(gameObject);
        }
    }

    public void OnItemRemoved(InventoryItemData itemData)
    {
        int stacks = GetStackCount(itemData.inventoryIndex);
        for (int i = 0; i < stacks; i++)
        {
            itemData.item.Unequip(gameObject);
        }
    }

    int GetStackCount(int index)
    {
        int maxStacks = 2;
        return 1 + maxStacks - Mathf.Clamp(index, 0, maxStacks);
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

    public GameObject owner => gameObject;

    #endregion

    #region IAttacker

    public void AddOnHitConsumer(AConsumerFactory onHitConsumer)
    {
        _onHitConsumers.Add(onHitConsumer);
    }

    public List<AConsumerFactory> GetOnHitConsumers()
    {
        return _onHitConsumers;
    }

    public void RemoveOnHitConsumer(AConsumerFactory onHitConsumer)
    {
        _onHitConsumers.Remove(onHitConsumer);
    }

    public void AddOnHitEffect(ABuffHandlerFactory onHitEffects)
    {
        _onHitEffects.Add(onHitEffects);
    }

    public List<ABuffHandlerFactory> GetOnHitEffects()
    {
        return _onHitEffects;
    }

    public void RemoveOnHitEffect(ABuffHandlerFactory onHitEffects)
    {
        _onHitEffects.Remove(onHitEffects);
    }

    #endregion

    #region IBuffable

    public void AddBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target)
    {
        _buffManager.AddHandler(buffHandlerFactory, source, target);
    }

    public void RemoveBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target)
    {
        _buffManager.RemoveHandler(buffHandlerFactory, source, target);
    }

    #endregion

    #region IMarkable

    public void Mark()
    {
        OnMarkChanged.Invoke(true);
    }

    public void UnMark()
    {
        OnMarkChanged.Invoke(false);
    }

    #endregion
}