using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ManaOnRoundEndBuff")]
public class ManaOnRoundEndBuffFactory : BuffFactory<ManaOnRoundEndBuff, ManaOnRoundEndBuffData> { }

[Serializable]
public class ManaOnRoundEndBuffData
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
}

public class ManaOnRoundEndBuff : ABuff<ManaOnRoundEndBuffData>, IStackableBuff
{
    int _stacks = 1;

    void OnRoundEnd()
    {
        Character character = PlayerBehaviour.instance.character;
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(data.consumerFactory.GetConsumer(character.gameObject, character.gameObject));
        resourceModifier.multiplier = _stacks;
        resourceModifier.source = character.gameObject;

        character.mana.AddResourceModifier(resourceModifier);
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