using UnityEngine;

// Explicit data and authored presentation contracts keep icon lookup independent of field names.
public static class DataIconSource
{
    public static object Unwrap(object source)
    {
        IGameDataSource owner = source as IGameDataSource;
        object data = owner != null ? owner.sourceData : null;
        return data ?? source;
    }

    public static string Label(object source)
    {
        string label = null;
        if (source is IDataIconMetadata metadata)
        {
            label = metadata.label;
        }
        else if (source is EntityData entity)
        {
            label = entity.title;
        }
        else if (source is CharacterData character)
        {
            label = character.title;
        }
        else if (source is CharacterSkillData skill)
        {
            label = skill.name;
        }
        else if (source is BaseItemData item)
        {
            label = item.name;
        }

        if (string.IsNullOrWhiteSpace(label) && source is UnityEngine.Object asset)
        {
            label = asset.name;
        }

        return string.IsNullOrWhiteSpace(label) ? source.GetType().Name : label;
    }

    public static Sprite AuthoredSprite(object source)
    {
        if (source is IDataIconMetadata metadata)
        {
            return metadata.icon;
        }
        if (source is BaseItemData item)
        {
            return item.icon;
        }
        if (source is CharacterSkillData skill)
        {
            return skill.icon;
        }
        if (source is BuffHandlerBaseData handler)
        {
            return handler.icon;
        }

        return null;
    }
}
