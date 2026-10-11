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

    // Items the character starts with, outside the inventory
    List<AItem> _items = new List<AItem>();
    public List<AItem> items { get { return _items; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    // Skills ignore their validators (no cost, no cooldown)
    bool _hasUnrestrictedSkills;
    public bool hasUnrestrictedSkills { get { return _hasUnrestrictedSkills; } set { _hasUnrestrictedSkills = value; } }

    public void Init()
    {
        InitAttributes();
        InitItems();

        // Init skills
        foreach (ACharacterSkillFactory skillFactory in _data.skills)
        {
            UseCharacterSkillButton skillButton = UIManager.instance.GetView<GameView>(ViewType.Game).characterSkillInventory.Create();
            CharacterSkillSlot skillSlot = gameObject.AddComponent<CharacterSkillSlot>();
            skillButton.character = this;
            skillSlot.Init(skillFactory.Create(), skillButton, !_hasUnrestrictedSkills);
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

    // The attributes and the mana of the character, from its data. With InitItems, enough on their own to read
    // its real stats outside a run (e.g. on the character select screen), without its skill buttons and inventory
    public void InitAttributes()
    {
        _attributeManager = GetComponent<AttributeManager>();
        foreach (var attribute in _data.attributes)
        {
            _attributeManager.Add(attribute.Key, new Attribute(attribute.Value));
        }

        _mana = gameObject.AddComponent<ResourceAttribute>();
        _mana.Init(AttributeType.ManaMax);
    }

    // The items the character starts with (passives, ...), from its data: they buff its attributes, so after
    // InitAttributes
    public void InitItems()
    {
        _buffManager = GetComponent<BuffManager>();
        foreach (AItemFactory itemFactory in _data.items)
        {
            AItem item = itemFactory.GetItem();
            item.Equip(gameObject);
            _items.Add(item);
        }
    }

    // Applies the buffs added (e.g. by the starting items) to the attributes right away, instead of over
    // the next frames
    public void ApplyBuffs()
    {
        _buffManager.ForceUpdate();
        _attributeManager.ForceUpdate();
    }

    // On in a battle only: its buffs tick and its skills can be used
    public void Enable(bool isEnabled)
    {
        _buffManager.isEnabled = isEnabled;
        foreach (CharacterSkillSlot skillSlot in _skillSlots)
        {
            skillSlot.isEnabled = isEnabled;
        }
    }

    public void Reset()
    {
        // Also called by the editor when the component is added, before any Init()
        if (_buffManager != null)
        {
            _buffManager.Reset();
        }
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