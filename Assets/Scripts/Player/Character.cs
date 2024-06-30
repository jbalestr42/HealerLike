using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Character : MonoBehaviour
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
        foreach (ABuffFactory passive in _data.passives)
        {
            AddBuff(passive, gameObject, gameObject);
        }

        // Init skills
        foreach (CharacterSkillSlotData skillData in _data.skills)
        {
            UseCharacterSkillButton skillButton = UIManager.instance.GetView<GameView>(ViewType.Game).characterSkillInventory.Create();
            CharacterSkillSlot skillSlot = gameObject.AddComponent<CharacterSkillSlot>();
            skillSlot.Init(skillData, skillButton);
        }

        // Init starting entities
        _entityPool.AddRange(_data.entities);
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