using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, IBuffable
{
    CharacterData _data;
    public CharacterData data { get { return _data; } set { _data = value; } }

    ResourceAttribute _mana;
    public ResourceAttribute mana { get { return _mana; } }

    AttributeManager _attributeManager;
    public AttributeManager attributeManager { get { return _attributeManager; } set { _attributeManager = value; } }

    BuffManager _buffManager;
    public BuffManager buffManager { get { return _buffManager; } }

    List<EntityData> _entityPool = new List<EntityData>();
    public List<EntityData> entityPool { get { return _entityPool; } }

    List<CharacterSkillSlot> _skillSlots = new List<CharacterSkillSlot>();
    public List<CharacterSkillSlot> skillSlots { get { return _skillSlots; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    public void Init()
    {
        _buffManager = GetComponent<BuffManager>();

        // Init attributes from data
        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            _attributeManager.Add(attribute.Key, new Attribute(attribute.Value));
        }

        _mana = gameObject.AddComponent<ResourceAttribute>();
        _mana.Init(AttributeType.ManaMax);

        // Init self buff from data
        foreach (ABuffHandlerFactory passive in _data.passives)
        {
            AddBuffHandler(passive, gameObject, gameObject);
        }

        // Init skills
        foreach (ACharacterSkillFactory skillFactory in _data.skills)
        {
            UseCharacterSkillButton skillButton = UIManager.instance.GetView<GameView>(ViewType.Game).characterSkillInventory.Create();
            CharacterSkillSlot skillSlot = gameObject.AddComponent<CharacterSkillSlot>();
            skillButton.character = this;
            skillSlot.Init(skillFactory.Create(), skillButton);
            _skillSlots.Add(skillSlot);
        }

        // Init starting entities
        _entityPool.AddRange(_data.entities);

        // Register inventory events
        _inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);
        
        // Disable the unit since we are not in combat
        Enable(false);
    }

    public void Enable(bool isEnabled)
    {
        _buffManager.isEnabled = isEnabled;
    }

    public void Reset()
    {
        _buffManager.Reset();
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
}