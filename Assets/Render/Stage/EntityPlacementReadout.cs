using System.Reflection;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The legacy interaction owns the selected data and its cosmetic model but exposes neither.
    // Cache this narrow bridge once; no input, entity spawning or grid calls belong in the adapter.
    public static class EntityPlacementReadout
    {
        static readonly FieldInfo dataField = typeof(EntityGridInteraction).GetField("_data",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo modelField = typeof(EntityGridInteraction).GetField("_entity",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo sideField = typeof(EntityGridInteraction).GetField("_entityType",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryRead(AInteraction interaction, out EntityData data, out GameObject model,
            out Entity.EntityType side)
        {
            data = null;
            model = null;
            side = Entity.EntityType.Player;
            if (!(interaction is EntityGridInteraction) || dataField == null || modelField == null || sideField == null)
                return false;
            data = dataField.GetValue(interaction) as EntityData;
            model = modelField.GetValue(interaction) as GameObject;
            side = (Entity.EntityType)sideField.GetValue(interaction);
            return data && model;
        }
    }
}
