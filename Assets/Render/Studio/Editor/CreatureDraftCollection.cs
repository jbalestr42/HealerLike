using System;
using System.Collections.Generic;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's recipe drafts as EditorPrefs keeps them, with the selection: a draft by index or an asset
    [Serializable]
    public class CreatureDraftCollection
    {
        public List<CreatureDraftRecord> items = new List<CreatureDraftRecord>();
        public int selectedIndex = -1;
        public string selectedAsset;
    }
}
