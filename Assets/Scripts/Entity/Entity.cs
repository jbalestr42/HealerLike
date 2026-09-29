using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;

[RequireComponent(typeof(BuffManager), typeof(AttributeManager))]
public class Entity : MonoBehaviour, IAttackable, IAttacker, IBuffable, IMarkable
{
    public UnityEvent<bool> OnMarkChanged = new UnityEvent<bool>();
    // Damage this entity dealt to a target, after its armor (e.g. for a life steal)
    public UnityEvent<GameObject, float> OnDamageDealt = new UnityEvent<GameObject, float>();
    // Each attack this entity makes (e.g. to repeat it)
    public UnityEvent<ProjectileAttack> OnAttack = new UnityEvent<ProjectileAttack>();
    // Each heal this entity receives (e.g. to empower its next attack)
    public UnityEvent<GameObject, ConsumerResult> OnHealReceived = new UnityEvent<GameObject, ConsumerResult>();

    public enum EntityType
    {
        None,
        Player,
        Computer,
        Any
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
    public InventoryHandler inventoryHandler { get { return _inventoryHandler; } }

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
    public List<ASkill> skills { get { return _skills; } }

    List<AItem> _items = new List<AItem>();
    public List<AItem> items { get { return _items; } }

    bool _isDraggable;
    public bool isDraggable { get { return _isDraggable; } }

    // Tags given at runtime (e.g. Summon), on top of the tags of the data
    List<GameplayTag> _runtimeTags = new List<GameplayTag>();
    public List<GameplayTag> runtimeTags { get { return _runtimeTags; } }

    // Asked in turn when the entity should die, the first one returning true keeps it alive (e.g. a revive)
    public delegate bool DeathPrevention(Entity dying);
    List<DeathPrevention> _deathPreventions = new List<DeathPrevention>();

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
        _health.OnAllConsumerProcessed.AddListener(OnConsumerProcessed);

        // Init target behaviour from data
        _targetProvider.Init(data.targetBehaviourType, data.targetValidators);

        // Init model from data
        GameObject model = Instantiate(_data.model, transform);
        _model = model.GetComponent<EntityModel>();
        Assert.IsNotNull(_model, "The model must have a 'EntityModel' component");
        _model.Init(this);
        _targetPoint = model.GetComponentInChildren<SkillTargetPointTag>()?.gameObject ?? gameObject;

        // Init items (passives, on hit effects, ...) from data
        foreach (AItemFactory itemFactory in _data.items)
        {
            AItem item = itemFactory.GetItem();
            item.Equip(gameObject);
            _items.Add(item);
        }

        // Init skills from data
        foreach (ASkillFactory skillFactory in _data.skillFactories)
        {
            ASkill skill = skillFactory.AddSkill(gameObject);
            _skills.Add(skill);
        }

        // Register inventory events
        _inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);

        EntityManager.instance.OnEntitySpawned.Invoke(this);

        // Disable the unit since we are not in combat
        Enable(false);
    }

    void OnHealthChanged(ResourceAttribute health)
    {
        if (health.Value <= 0f && !TryPreventDeath())
        {
            EntityManager.instance.DestroyEntity(gameObject, entityType);
        }
    }

    void OnConsumerProcessed(GameObject target, ResourceModifier resourceModifier, ConsumerResult result)
    {
        NotifyAttacker(resourceModifier.source, target, result.value);
        NotifyHealed(resourceModifier.source, target, result);
    }

    // Damage is a negative value, reported as a positive amount to the entity that dealt it
    public static void NotifyAttacker(GameObject source, GameObject target, float value)
    {
        // Damage an entity deals to itself (e.g. the Cursed Idol) isn't dealt to anyone
        if (value >= 0f || source == null || source == target)
        {
            return;
        }

        Entity attacker = source.GetComponent<Entity>();
        if (attacker != null)
        {
            attacker.OnDamageDealt.Invoke(target, -value);
        }
    }

    // A heal is a positive value, reported to the entity that received it
    public static void NotifyHealed(GameObject source, GameObject target, ConsumerResult result)
    {
        if (result.value <= 0f || target == null)
        {
            return;
        }

        Entity healed = target.GetComponent<Entity>();
        if (healed != null)
        {
            healed.OnHealReceived.Invoke(source, result);
        }
    }

    public void AddTag(GameplayTag tag)
    {
        if (tag == null)
        {
            Debug.LogError($"[Entity] Can't add a null tag to {name}, is it missing from the GameData tags?");
            return;
        }

        if (!_runtimeTags.Contains(tag))
        {
            _runtimeTags.Add(tag);
        }
    }

    public void AddDeathPrevention(DeathPrevention deathPrevention)
    {
        _deathPreventions.Add(deathPrevention);
    }

    public void RemoveDeathPrevention(DeathPrevention deathPrevention)
    {
        _deathPreventions.Remove(deathPrevention);
    }

    // True when one of the death preventions keeps the entity alive, the last added one asked first
    public bool TryPreventDeath()
    {
        // Backwards, so a prevention can remove itself while being asked
        for (int i = _deathPreventions.Count - 1; i >= 0; i--)
        {
            if (_deathPreventions[i](this))
            {
                return true;
            }
        }
        return false;
    }

    public void RemoveTag(GameplayTag tag)
    {
        _runtimeTags.Remove(tag);
    }

    // Has the tag, or one of its descendants, from the data or given at runtime
    public bool HasTag(GameplayTag tag)
    {
        if (tag == null)
        {
            return false;
        }

        System.Predicate<GameplayTag> matches = entityTag => entityTag != null && (entityTag == tag || entityTag.IsDescendantOf(tag));
        return _runtimeTags.Exists(matches) || (_data != null && _data.tags.Exists(matches));
    }

    public void Enable(bool isEnabled)
    {
        _isDraggable = !isEnabled;
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

        GameplayTag permanentTag = DataManager.instance.GetTagWithName("Permanent");
        _buffManager.RemoveBuff(buffHandlerData => !buffHandlerData.buffHandlerFactory.tags.Exists(tag => tag.IsDescendantOf(permanentTag)));

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
        itemData.item.Equip(gameObject);
    }

    public void OnItemRemoved(InventoryItemData itemData)
    {
        itemData.item.Unequip(gameObject);
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