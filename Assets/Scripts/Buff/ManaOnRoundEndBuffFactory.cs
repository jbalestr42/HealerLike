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
        character.mana.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, character.gameObject, character.gameObject, _stacks));
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