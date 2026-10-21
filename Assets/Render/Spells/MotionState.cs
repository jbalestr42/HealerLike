namespace HealerLike.Render.Spells
{
    // What of an element's own state its motion reads beside the recipe
    public struct MotionState
    {
        public bool isStatus;
        public bool isRemoving;
        // How far through its removal the element is, from 0 as it begins to 1 once it has closed
        public float removal;
        // How far a drop falls, in the element's own units
        public float fallDistance;
    }
}
