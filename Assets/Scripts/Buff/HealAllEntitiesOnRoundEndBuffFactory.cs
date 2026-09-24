using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/HealAllEntitiesOnRoundEndBuff")]
public class HealAllEntitiesOnRoundEndBuffFactory : BuffFactory<HealAllEntitiesOnRoundEndBuff, HealAllEntitiesOnRoundEndBuffData> { }

[Serializable]
public class HealAllEntitiesOnRoundEndBuffData
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
}

public class HealAllEntitiesOnRoundEndBuff : ABuff<HealAllEntitiesOnRoundEndBuffData>, IStackableBuff
{
    int _stacks = 1;

    void OnRoundEnd()
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(Entity.EntityType.Player))
        {
            entity.GetComponent<Entity>().health.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, entity.gameObject, entity.gameObject, _stacks));
        }
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        AscensionGameType.OnRoundEnd.AddListener(OnRoundEnd);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        AscensionGameType.OnRoundEnd.RemoveListener(OnRoundEnd);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}