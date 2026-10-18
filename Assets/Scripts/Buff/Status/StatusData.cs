using UnityEngine;
using Oisif.Inspector;

// A status a unit can be in (e.g. Blessed): the tag it carries while in it, and the buff putting it there (duration,
// icon, the tag itself). Given and checked through it, never through the tag or the buff alone, for both to stay in step
[CreateAssetMenu(menuName = "Custom/Data/Status")]
public class StatusData : ScriptableObject
{
    public string title;
    [TextArea]
    public string description;
    // A buff (e.g. Hasted), else a debuff (e.g. Vulnerable): sets the color of its keyword in the descriptions
    public bool isPositive;
    // Carried while in the status, what the reactions to it look for (e.g. armor while blessed)
    public GameplayTag tag;
    // Gives the status: must give the tag above (e.g. with an AddTagBuff)
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;

    // Its buff gives the tag while it lasts, and is tagged with it too, for the buffs reacting to each application
    // (see TagTrigger.EachApplication)
    public bool GivesItsTag()
    {
        BuffHandlerFactory handler = buffHandlerFactory as BuffHandlerFactory;
        if (tag == null || handler == null || handler.data == null || handler.data.buffFactoryList == null)
        {
            return false;
        }
        return handler.data.buffFactoryList.Exists(buff => buff is AddTagBuffFactory addTag && addTag.data != null && addTag.data.tag == tag)
            && handler.HasTag(tag);
    }
}

public static class StatusExtensions
{
    // Puts the unit in the status, or refreshes it, the source being the one giving it (e.g. the purifier)
    public static void ApplyStatus(this Entity entity, StatusData status, GameObject source)
    {
        BuffManager buffManager = entity != null ? entity.GetComponent<BuffManager>() : null;
        if (buffManager == null || status == null || status.buffHandlerFactory == null)
        {
            return;
        }
        buffManager.AddHandler(status.buffHandlerFactory, source, entity.gameObject);
    }

    // In the status, or in one descending from it
    public static bool HasStatus(this Entity entity, StatusData status)
    {
        return entity != null && status != null && status.tag != null && entity.HasTag(status.tag);
    }
}
