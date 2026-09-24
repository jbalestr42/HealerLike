using System;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // One unsaved recipe in EditorPrefs, with the surface it was previewed on
    [Serializable]
    public class CreatureDraftRecord
    {
        public string name;
        public string json;
        public LookSide surface;
        public bool hasSurface;
    }
}
