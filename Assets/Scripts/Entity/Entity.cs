using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;

[RequireComponent(typeof(BuffManager), typeof(AttributeManager))]
public class Entity : MonoBehaviour, IAttackable, IAttacker, ISelectable, IBuffable, IMarkable
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
    List<ABuffFactory> _projectileBehaviours = new List<ABuffFactory>();
    public List<ABuffFactory> projectileBehaviours { get { return _projectileBehaviours; } set { _projectileBehaviours = value; } }

    EntityData _data;
    public EntityData data { get { return _data; } set { _data = value; } }

    AttributeManager _attributeManager;
    public AttributeManager attributeManager { get { return _attributeManager; } set { _attributeManager = value; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    EntityModel _model;
    public SkillSource skillStartPoint { get { return _model.GetSourcePoint(); } } 

    GameObject _targetPoint;
    public GameObject targetPoint => _targetPoint;

    public void Init()
    {
        _buffManager = GetComponent<BuffManager>();

        // Init attributes from data
        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            _attributeManager.Add(attribute.Key, new Attribute(attribute.Value));
        }

        _health = gameObject.AddComponent<ResourceAttribute>();
        _health.Init();
        _health.OnValueChanged.AddListener(OnHealthChanged);

        // Init model from data
        GameObject model = Instantiate(_data.model, transform);
        _model = model.GetComponent<EntityModel>();
        Assert.IsNotNull(_model, "The model must have a 'EntityModel' component");
        _model.Init(this);
        _targetPoint = model.GetComponentInChildren<SkillTargetPointTag>()?.gameObject ?? gameObject;

        // Init self buff from data
        foreach (ABuffFactory passive in _data.passives)
        {
            AddBuff(passive, gameObject, gameObject);
        }

        // Init on hit effects from data
        foreach (ABuffHandlerFactory onHitEffect in _data.onHitEffects)
        {
            AddOnHitEffect(onHitEffect);
        }

        // Init damage from data
        foreach (AConsumerFactory consumerFactory in _data.onHitConsumer)
        {
            AddOnHitConsumer(consumerFactory);
        }

        // Init skills from data
        foreach (ASkillFactory skillFactory in _data.skillFactories)
        {
            skillFactory.AddSkill(gameObject);
        }

        // Register to inventory events
        _inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);
    }

    void OnHealthChanged(ResourceAttribute health)
    {
        if (health.Value <= 0f)
        {
            EntityManager.instance.DestroyEntity(gameObject, entityType);
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

    public void AddBuff(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        _buffManager.Add(buffFactory, source, target);
    }

    public void RemoveBuff(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        _buffManager.Remove(buffFactory, source, target);
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

    #region ISelectable

    public void Select()
    {
        UIManager.instance.GetView<GameView>(ViewType.Game).ShowPanel(PanelType.Entity, gameObject);
    }

    public void UnSelect()
    {
        UIManager.instance.GetView<GameView>(ViewType.Game).HidePanel(PanelType.Entity);
    }

    #endregion
}