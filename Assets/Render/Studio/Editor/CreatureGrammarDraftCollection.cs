using System;
using System.Collections.Generic;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's grammar drafts as EditorPrefs keeps them, with the override table, the parts preview
    // surface and the selection
    [Serializable]
    public class CreatureGrammarDraftCollection
    {
        public List<CreatureGrammarDraftRecord> items = new List<CreatureGrammarDraftRecord>();
        public int selectedIndex = -1;
        public string selectedAsset;
        public string creatureLooksAsset;
        public LookSide manualSurface;
        public bool grammarMode = true;
    }
}
