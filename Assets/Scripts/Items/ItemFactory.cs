using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Items/Item")]
public class ItemFactory : ItemFactory<Item, ItemData> {}

[Serializable]
public class ItemData : BaseItemData
{
    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(buffList)")]
    public List<ABuffFactory> buffList;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(onHitEffects)")]
    public List<ABuffHandlerFactory> onHitEffects;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AConsumerFactory>, AConsumerFactory>(onHitConsumers)")]
    public List<AConsumerFactory> onHitConsumers;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(projectileBehaviours)")]
    public List<ABuffFactory> projectileBehaviours;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillFactory>, ASkillFactory>(skills)")]
    public List<ASkillFactory> skills;
}

public class Item : AItem<ItemData>
{
    List<ASkill> _skillInstances = new List<ASkill>();

    public override void Equip(GameObject target)
    {
        foreach (ABuffFactory buffFactory in data.buffList)
        {
            target.GetComponent<IBuffable>().AddBuff(buffFactory, target, target);
        }

        foreach (ABuffHandlerFactory buffHandlerFactory in data.onHitEffects)
        {
            target.GetComponent<IAttacker>().AddOnHitEffect(buffHandlerFactory);
        }

        foreach (AConsumerFactory consumerFactory in data.onHitConsumers)
        {
            target.GetComponent<IAttacker>().AddOnHitConsumer(consumerFactory);
        }

        foreach (ABuffFactory projectileBehaviour in data.projectileBehaviours)
        {
            // target.GetComponent<Tower>().projectileBehaviours.Add(projectileBehaviour);
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
        foreach (ABuffFactory buffFactory in data.buffList)
        {
            target.GetComponent<IBuffable>().RemoveBuff(buffFactory, target, target);
        }

        foreach (ABuffHandlerFactory buffHandlerFactory in data.onHitEffects)
        {
            target.GetComponent<IAttacker>().RemoveOnHitEffect(buffHandlerFactory);
        }

        foreach (AConsumerFactory consumerFactory in data.onHitConsumers)
        {
            target.GetComponent<IAttacker>().RemoveOnHitConsumer(consumerFactory);
        }

        foreach (ABuffFactory projectileBehaviour in data.projectileBehaviours)
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