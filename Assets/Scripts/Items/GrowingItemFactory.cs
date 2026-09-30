using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Items/GrowingItem")]
public class GrowingItemFactory : ItemFactory<GrowingItem, GrowingItemData> {}

[Serializable]
public class GrowingItemData : BaseItemData
{
    // Stacked once on the holder for each battle it survives: it must stack (maxStacks 0) and outlive the
    // end of the battles (Permanent tag, e.g. FromItem)
    [CreateDataButton]
    public ABuffHandlerFactory growthBuffHandlerFactory;
}

// Grows each battle its holder survives (e.g. Growing Seed). The growth belongs to the item: moved to
// another unit, the item takes it along
public class GrowingItem : AItem<GrowingItemData>
{
    GameObject _holder;
    int _growth = 0;
    public int growth => _growth;

    public override string description => _growth > 0 ? $"{data.description} (grown {_growth} times)" : data.description;

    public override void Equip(GameObject target)
    {
        _holder = target;
        IBuffable buffable = target.GetComponent<IBuffable>();
        for (int i = 0; i < _growth; i++)
        {
            buffable.AddBuffHandler(data.growthBuffHandlerFactory, target, target);
        }
        AscensionGameType.OnRoundEnd.AddListener(Grow);
    }

    public override void Unequip(GameObject target)
    {
        AscensionGameType.OnRoundEnd.RemoveListener(Grow);
        IBuffable buffable = target.GetComponent<IBuffable>();
        for (int i = 0; i < _growth; i++)
        {
            buffable.RemoveBuffHandler(data.growthBuffHandlerFactory, target, target);
        }
        _holder = null;
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

        _growth++;
        _holder.GetComponent<IBuffable>().AddBuffHandler(data.growthBuffHandlerFactory, _holder, _holder);
    }
}
