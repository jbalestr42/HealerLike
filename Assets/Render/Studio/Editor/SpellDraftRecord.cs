using System;

namespace HealerLike.Render.Studio.Editor
{
    // One unsaved spell in EditorPrefs: the preset as JSON, its asset references as GUIDs
    [Serializable]
    public class SpellDraftRecord
    {
        public string json;
        public string vocabularyGuid;
        public string handlerGuid;
        public string looksGuid;
        public string projectileGuid;
    }
}
