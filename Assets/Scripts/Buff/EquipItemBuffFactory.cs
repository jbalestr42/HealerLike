using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/EquipItemBuff")]
public class EquipItemBuffFactory : BuffFactory<EquipItemBuff, EquipItemBuffData> { }

[Serializable]
public class EquipItemBuffData
{
    // Equipped on the holder while it has the buff
    public AItemFactory item;
}

// Gives an item to the holder for as long as the buff lasts (e.g. the Archon getting the Blight of a dead Blight
// Acolyte, its hits blighting from then on)
public class EquipItemBuff : ABuff<EquipItemBuffData>
{
    AItem _item;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        if (data.item == null || target == null || _item != null)
        {
            return;
        }
        _item = data.item.GetItem();
        _item.Equip(target);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_item != null && target != null)
        {
            _item.Unequip(target);
        }
        _item = null;
    }
}
