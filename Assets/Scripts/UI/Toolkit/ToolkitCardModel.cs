using System;

namespace HealerLike.UI.Toolkit
{
    /// <summary>Presentation-only data. Layout and theme never need to know gameplay types.</summary>
    public sealed class ToolkitCardModel
    {
        public string Key;
        public object IconSource;
        public string Title;
        public string Description;
        public string Status;
        public bool Enabled = true;
        public Action Activate;
    }
}
