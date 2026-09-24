using System;

namespace HealerLike.Render.Studio.Editor
{
    // One unsaved grammar preset in EditorPrefs: the preset as JSON, its vocabulary and source entity as GUIDs
    [Serializable]
    public class CreatureGrammarDraftRecord
    {
        public string json;
        public string vocabulary;
        public string source;
    }
}
