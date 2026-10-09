using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Items/Item")]
public class ItemFactory : ItemFactory<Item, ItemData> {}

[Serializable]
public class ItemData : BaseItemData
{
    [Space]
    [CreateDataButton]
    public List<ABuffHandlerFactory> buffs = new List<ABuffHandlerFactory>();

    [CreateDataButton]
    public List<ABuffHandlerFactory> onHitEffects = new List<ABuffHandlerFactory>();

    [CreateDataButton]
    public List<AConsumerFactory> onHitConsumers = new List<AConsumerFactory>();

    [CreateDataButton]
    public List<ABuffHandlerFactory> projectileBehaviours = new List<ABuffHandlerFactory>();

    [CreateDataButton]
    public List<ASkillFactory> skills = new List<ASkillFactory>();
}

public class Item : AItem<ItemData>
{
    List<ASkill> _skillInstances = new List<ASkill>();

    public override void Equip(GameObject target)
    {
        foreach (ABuffHandlerFactory buffHandlerFactory in data.buffs)
        {
            target.GetComponent<IBuffable>().AddBuffHandler(buffHandlerFactory, target, target);
        }

        foreach (ABuffHandlerFactory buffHandlerFactory in data.onHitEffects)
        {
            target.GetComponent<IAttacker>().AddOnHitEffect(buffHandlerFactory);
        }

        foreach (AConsumerFactory consumerFactory in data.onHitConsumers)
        {
            target.GetComponent<IAttacker>().AddOnHitConsumer(consumerFactory);
        }

        foreach (ABuffHandlerFactory projectileBehaviour in data.projectileBehaviours)
        {
            target.GetComponent<Entity>().projectileBehaviours.Add(projectileBehaviour);
        }

        foreach (ASkillFactory skillFactory in data.skills)
        {
            ASkill skill = skillFactory.AddSkill(target);
            _skillInstances.Add(skill);
        }
    }

    public override void Unequip(GameObject target)
    {
        foreach (ABuffHandlerFactory buffHandlerFactory in data.buffs)
        {
            target.GetComponent<IBuffable>().RemoveBuffHandler(buffHandlerFactory, target, target);
        }

        foreach (ABuffHandlerFactory buffHandlerFactory in data.onHitEffects)
        {
            target.GetComponent<IAttacker>().RemoveOnHitEffect(buffHandlerFactory);
        }

        foreach (AConsumerFactory consumerFactory in data.onHitConsumers)
        {
            target.GetComponent<IAttacker>().RemoveOnHitConsumer(consumerFactory);
        }

        foreach (ABuffHandlerFactory projectileBehaviour in data.projectileBehaviours)
        {
            target.GetComponent<Entity>().projectileBehaviours.Remove(projectileBehaviour);
        }

        foreach (ASkill skill in _skillInstances)
        {
            GameObject.Destroy(skill);
        }
        _skillInstances.Clear();
    }
}