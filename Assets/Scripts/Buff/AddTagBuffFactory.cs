using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/AddTagBuff")]
public class AddTagBuffFactory : BuffFactory<AddTagBuff, AddTagBuffData> { }

[Serializable]
public class AddTagBuffData
{
    // Given to the holder as long as the buff lasts (e.g. a taunt)
    public GameplayTag tag;
}

public class AddTagBuff : ABuff<AddTagBuffData>
{
    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().AddTag(data.tag);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        if (entity != null)
        {
            entity.RemoveTag(data.tag);
        }
    }
}
