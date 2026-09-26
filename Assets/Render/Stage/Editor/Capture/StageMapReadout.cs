using System;
using System.Reflection;

namespace HealerLike.Render.Stage
{
    public static class StageMapReadout
    {
        // Capture-only observation: gameplay exposes selection commands but no selection getter.
        static readonly FieldInfo selectionField
            = typeof(InteractionManager).GetField("_selectable",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo waveField = typeof(AscensionGameType).GetField("_currentWave",
            BindingFlags.Instance | BindingFlags.NonPublic);
        public static ISelectable Selected(InteractionManager interaction)
        {
            if (interaction == null || selectionField == null || selectionField.FieldType != typeof(ISelectable))
            {
                throw new InvalidOperationException("Capture selection observation requires "
                    + "InteractionManager._selectable of type ISelectable.");
            }

            return (ISelectable)selectionField.GetValue(interaction);
        }

        public static WavePatternData Wave(AscensionGameType ascension)
        {
            return (WavePatternData)waveField.GetValue(ascension);
        }
    }
}
