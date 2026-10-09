using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Items/GrowingItem")]
public class GrowingItemFactory : ItemFactory<GrowingItem, GrowingItemData> {}

[Serializable]
public class GrowingItemData : BaseItemData
{
    // Stacked once on the holder for each battle it survives: it must stack (maxStacks 0) and outlive the
    // end of the battles (Permanent tag, e.g. FromItem)
    [CreateDataButton]
    public ABuffHandlerFactory growthBuffHandlerFactory;

    // Battles it grows at most, 0 for no limit
    [Min(0)] public int maxGrowth = 0;

    // Optional, stacked once on the holder for each enemy it kills, like the growth of the battles
    [CreateDataButton]
    public ABuffHandlerFactory killGrowthBuffHandlerFactory;

    // Kills it grows at most, 0 for no limit
    [Min(0)] public int maxKillGrowth = 0;
}

// Grows each battle its holder survives (e.g. Growing Seed), and optionally each enemy it kills (e.g.
// Hungering Mask), each growth up to its limit. The growth belongs to the item: moved to another unit, the
// item takes it along
public class GrowingItem : AItem<GrowingItemData>
{
    GameObject _holder;
    Entity _holderEntity;

    int _growth = 0;
    public int growth => _growth;

    int _killGrowth = 0;
    public int killGrowth => _killGrowth;

    public override string description
    {
        get
        {
            if (data.killGrowthBuffHandlerFactory != null)
            {
                return _growth > 0 || _killGrowth > 0 ? $"{data.description} ({_killGrowth} kills, {_growth} battles)" : data.description;
            }
            return _growth > 0 ? $"{data.description} (grown {_growth} times)" : data.description;
        }
    }

    public override void Equip(GameObject target)
    {
        _holder = target;
        IBuffable buffable = target.GetComponent<IBuffable>();
        AddHandlers(buffable, data.growthBuffHandlerFactory, _growth, target);
        AddHandlers(buffable, data.killGrowthBuffHandlerFactory, _killGrowth, target);
        AscensionGameType.OnRoundEnd.AddListener(Grow);

        _holderEntity = target.GetComponent<Entity>();
        if (_holderEntity != null && data.killGrowthBuffHandlerFactory != null)
        {
            _holderEntity.OnKill.AddListener(OnKill);
        }
    }

    public override void Unequip(GameObject target)
    {
        AscensionGameType.OnRoundEnd.RemoveListener(Grow);
        if (_holderEntity != null)
        {
            _holderEntity.OnKill.RemoveListener(OnKill);
        }

        IBuffable buffable = target.GetComponent<IBuffable>();
        RemoveHandlers(buffable, data.growthBuffHandlerFactory, _growth, target);
        RemoveHandlers(buffable, data.killGrowthBuffHandlerFactory, _killGrowth, target);
        _holder = null;
        _holderEntity = null;
    }

    // At the end of a won battle, where the units killed in it are already destroyed
    public void Grow()
    {
        // The holder died with the item on it: nothing left to grow
        if (_holder == null)
        {
            AscensionGameType.OnRoundEnd.RemoveListener(Grow);
            return;
        }

        if (data.growthBuffHandlerFactory == null || IsAtMax(_growth, data.maxGrowth))
        {
            return;
        }

        _growth++;
        _holder.GetComponent<IBuffable>().AddBuffHandler(data.growthBuffHandlerFactory, _holder, _holder);
    }

    // The holder killed an enemy
    public void OnKill(Entity killed)
    {
        if (_holder == null || data.killGrowthBuffHandlerFactory == null || IsAtMax(_killGrowth, data.maxKillGrowth))
        {
            return;
        }

        _killGrowth++;
        _holder.GetComponent<IBuffable>().AddBuffHandler(data.killGrowthBuffHandlerFactory, _holder, _holder);
    }

    static bool IsAtMax(int count, int max)
    {
        return max > 0 && count >= max;
    }

    static void AddHandlers(IBuffable buffable, ABuffHandlerFactory handler, int count, GameObject target)
    {
        if (handler == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            buffable.AddBuffHandler(handler, target, target);
        }
    }

    static void RemoveHandlers(IBuffable buffable, ABuffHandlerFactory handler, int count, GameObject target)
    {
        if (handler == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            buffable.RemoveBuffHandler(handler, target, target);
        }
    }
}
