using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffOnTagBuff")]
public class ApplyBuffOnTagBuffFactory : BuffFactory<ApplyBuffOnTagBuff, ApplyBuffOnTagBuffData> { }

public enum TagTrigger
{
    // Each time the holder gets the tag, the buff is given once: its own duration says how long it lasts
    WhenTagged,
    // The buff lasts as long as the holder has the tag
    WhileTagged,
    // Each time the holder is given a buff tagged with it, a refresh included (e.g. every blessing received, even
    // while already blessed): the buff is given once, its own duration and max stacks say how they add up
    EachApplication,
}

[Serializable]
public class ApplyBuffOnTagBuffData
{
    // The holder getting it, or one of its descendants, at runtime (e.g. Blessed)
    public GameplayTag tag;
    public TagTrigger trigger;
    // Given to the holder by itself
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Gives the holder a buff when it gets a tag, while it has it, or on each buff carrying it (e.g. a unit getting
// armor while blessed)
public class ApplyBuffOnTagBuff : ABuff<ApplyBuffOnTagBuffData>
{
    Entity _owner;
    BuffManager _buffManager;
    // WhileTagged: the buff is held, to remove it when the tag goes
    bool _isApplied;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _buffManager = target.GetComponent<BuffManager>();
        if (data.trigger == TagTrigger.EachApplication)
        {
            _buffManager.OnBuffHandlerApplied.AddListener(OnBuffHandlerApplied);
            return;
        }

        _owner.OnTagChanged.AddListener(OnTagChanged);
        // Already tagged when the buff comes (e.g. an item equipped on a blessed unit): only the lasting buff
        // applies, the moment of getting the tag is gone
        if (data.trigger == TagTrigger.WhileTagged && _owner.HasTag(data.tag))
        {
            Apply();
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner == null)
        {
            return;
        }

        _owner.OnTagChanged.RemoveListener(OnTagChanged);
        if (_buffManager != null)
        {
            _buffManager.OnBuffHandlerApplied.RemoveListener(OnBuffHandlerApplied);
        }
        if (_isApplied)
        {
            Unapply();
        }
    }

    bool Matches(GameplayTag tag)
    {
        return data.tag != null && tag != null && (tag == data.tag || tag.IsDescendantOf(data.tag));
    }

    void OnTagChanged(GameplayTag tag, bool isAdded)
    {
        if (!Matches(tag))
        {
            return;
        }

        if (data.trigger == TagTrigger.WhenTagged)
        {
            if (isAdded)
            {
                // Lasts its own duration, never taken back with the tag
                GiveBuff();
            }
        }
        else if (isAdded && !_isApplied)
        {
            Apply();
        }
        // Another tag descending from it may still be there
        else if (!isAdded && _isApplied && !_owner.HasTag(data.tag))
        {
            Unapply();
        }
    }

    void OnBuffHandlerApplied(ABuffHandlerFactory buffHandlerFactory)
    {
        // Its own buff carrying the tag would give itself again forever, also given as stacks of a status
        if (buffHandlerFactory == data.buffHandlerFactory || buffHandlerFactory == data.buffHandlerFactory.appliedFactory || buffHandlerFactory.tags == null || !buffHandlerFactory.tags.Exists(Matches))
        {
            return;
        }

        GiveBuff();
    }

    void Apply()
    {
        _isApplied = true;
        GiveBuff();
    }

    void Unapply()
    {
        _isApplied = false;
        _buffManager.RemoveHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }

    void GiveBuff()
    {
        _buffManager.AddHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }
}
