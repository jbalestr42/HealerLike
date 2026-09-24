using System;

namespace HealerLike.Render.Deliveries
{
    // How an arm draws one delivery style: a rod telescopes straight at its shot, the widths scale the arm and its
    // leaves, and a rod may snap back faster than the arm's own retract
    [Serializable]
    public class ArmStyle
    {
        public bool isRod;
        public float width = 1f;
        public float leafWidth = 1f;
        // Zero keeps the arm's own retract time
        public float retractSeconds;
    }
}
