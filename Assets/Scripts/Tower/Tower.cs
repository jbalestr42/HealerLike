using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;

[RequireComponent(typeof(BuffManager), typeof(AttributeManager))]
public class Tower : MonoBehaviour, IAttacker, ISelectable, IBuffable
{
    public static UnityEvent<Tower> OnTowerSold = new UnityEvent<Tower>();

    [SerializeField] TowerRange _towerRange;
    List<AConsumerFactory> _onHitConsumers = new List<AConsumerFactory>();
    List<ABuffHandlerFactory> _onHitEffects = new List<ABuffHandlerFactory>();
    public List<ABuffHandlerFactory> onHitEffects { get { return _onHitEffects; } set { _onHitEffects = value; } }
    List<ABuffFactory> _projectileBehaviours = new List<ABuffFactory>();
    public List<ABuffFactory> projectileBehaviours { get { return _projectileBehaviours; } set { _projectileBehaviours = value; } }

    TowerData _data;
    public TowerData data { get { return _data; } set { _data = value; } }

    AttributeManager _attributeManager;
    public AttributeManager attributeManager { get { return _attributeManager; } set { _attributeManager = value; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    public class AttributeUpgrade
    {
        public UpgradeModifier modifier;
        public int cost;
        public float stacks = 0;
        public float costIncreaseFactor;

        public bool CanUpgrade()
        {
            return cost <= PlayerBehaviour.instance.gold;
        }

        public void IncreaseCost()
        {
            cost = (int)((float)cost * Mathf.Pow(1.1f, stacks / costIncreaseFactor));
        }

        public void Upgrade(GameObject owner)
        {
            PlayerBehaviour.instance.gold -= cost;
            stacks++;
            IncreaseCost();
            modifier.Stack(owner, owner);
        }
    }

    Dictionary<AttributeType, AttributeUpgrade> _attributeUpgrade = new Dictionary<AttributeType, AttributeUpgrade>();
    public Dictionary<AttributeType, AttributeUpgrade> attributeUpgrade { get { return _attributeUpgrade; } }

    TowerModel _model;
    public SkillSource skillStartPoint { get { return _model.GetSourcePoint(); } } 

    void Start()
    {
        _buffManager = GetComponent<BuffManager>();

        // Init attributes from data
        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            _attributeManager.Add(attribute.Key, new Attribute(attribute.Value));
        }

        // Init upgradable attribute (hardcoded from now)
        foreach (var attributeTypeUpgradeData in DataManager.instance.data.attributeUpgradeData)
        {
            AttributeType attributeType = attributeTypeUpgradeData.Key;
            GameData.AttributeUpgradeData upgradeData = attributeTypeUpgradeData.Value;
            UpgradeModifier modifier = new UpgradeModifier() { data = new UpgradeModifierData() { value = upgradeData.bonusPerUpgrade }, stacks = 0 };
            _attributeManager.Get(attributeType).AddAbsoluteModifier(gameObject, modifier);
            _attributeUpgrade[attributeType] = new AttributeUpgrade() { modifier = modifier, cost = upgradeData.startingUpgradeCost, costIncreaseFactor = upgradeData.costIncreaseFactor };
        }

        // Init model from data
        GameObject model = Instantiate(_data.model, transform);
        _model = model.GetComponent<TowerModel>();
        Assert.IsNotNull(_model, "The model must have a 'TowerModel' component");
        _model.Init(this);

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

    public void OnSold()
    {
        OnTowerSold.Invoke(this);
        EntityManager.instance.DestroyTower(gameObject);
    }

    #region Attribute upgrade

    public AttributeUpgrade GetUpgradeAttribute(AttributeType attributeType)
    {
        return _attributeUpgrade[attributeType];
    }

    public bool CanUpgradeAttribute(AttributeType attributeType)
    {
        return _attributeUpgrade[attributeType].CanUpgrade();
    }

    public void UpgradeAttribute(AttributeType attributeType)
    {
        if (CanUpgradeAttribute(attributeType))
        {
            _attributeUpgrade[attributeType].Upgrade(gameObject);
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

    #region ISelectable

    public void Select()
    {
        _towerRange.Show(_attributeManager.Get(AttributeType.Range).Value);
        _attributeManager.Get(AttributeType.Range).AddOnValueChangedListener(OnRangeChanged);
        UIManager.instance.GetView<GameView>(ViewType.Game).ShowPanel(PanelType.Tower, gameObject);
    }

    public void UnSelect()
    {
        _towerRange.Hide();
        _attributeManager.Get(AttributeType.Range).RemoveOnValueChangedListener(OnRangeChanged);
        UIManager.instance.GetView<GameView>(ViewType.Game).HidePanel(PanelType.Tower);
    }

    void OnRangeChanged(Attribute range)
    {
        _towerRange.Show(range.Value);
    }

    #endregion
}