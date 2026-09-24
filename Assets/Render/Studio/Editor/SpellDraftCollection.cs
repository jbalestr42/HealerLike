using System;
using System.Collections.Generic;

namespace HealerLike.Render.Studio.Editor
{
    // The spell studio's drafts as EditorPrefs keeps them, with the selection: a draft by index or a saved asset
    [Serializable]
    public class SpellDraftCollection
    {
        public List<SpellDraftRecord> items = new List<SpellDraftRecord>();
        public int selectedDraftIndex = -1;
        public string selectedAssetGuid;
    }
}
