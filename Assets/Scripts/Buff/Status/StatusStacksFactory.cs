using System;
using System.Collections.Generic;
using UnityEngine;

// Applies stacks of a status wherever a buff handler is given (e.g. the on hit effect of an item, the buff of a skill):
// the Hexer making its target Vulnerable twice. The stacks land on the buff of the status itself, so every giver adds
// to the same stacks, with the effect, duration and max stacks of the status
[CreateAssetMenu(menuName = "Custom/Data/Status Stacks")]
public class StatusStacksFactory : ABuffHandlerFactory
{
    public StatusStacksData data = new StatusStacksData();

    ABuffHandlerFactory statusHandler => data.status != null ? data.status.buffHandlerFactory : null;

    public override ABuffHandlerFactory appliedFactory => statusHandler;
    public override int appliedStacks => Mathf.Max(1, data.stacks);

    public override ABuffHandler GetBuffHandler() => statusHandler != null ? statusHandler.GetBuffHandler() : null;
    public override List<ABuffFactory> buffFactoryList => statusHandler != null ? statusHandler.buffFactoryList : new List<ABuffFactory>();
    public override GameObject buffEffect => statusHandler != null ? statusHandler.buffEffect : null;
    public override Sprite icon => statusHandler != null ? statusHandler.icon : null;
    public override DurationType durationType => statusHandler != null ? statusHandler.durationType : DurationType.Instant;
    public override float duration => statusHandler != null ? statusHandler.duration : 0f;
    public override bool hasDuration => statusHandler != null && statusHandler.hasDuration;
    public override int maxStacks => statusHandler != null ? statusHandler.maxStacks : 0;
    public override bool stackAcrossSources => statusHandler != null && statusHandler.stackAcrossSources;
    public override List<GameplayTag> tags => statusHandler != null ? statusHandler.tags : new List<GameplayTag>();
}

[Serializable]
public class StatusStacksData
{
    public StatusData status;
    // Applied at once (e.g. 2 Vulnerable: twice the damage taken of one)
    [Min(1)] public int stacks = 1;
}
