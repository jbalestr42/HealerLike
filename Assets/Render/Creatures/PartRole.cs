namespace HealerLike.Render.Creatures
{
    // What a part does for the rig, so the rig never reads an id or a primitive to decide
    // Stored by value in assets: append new members, never reorder or remove
    public enum PartRole
    {
        Body,
        Stem,
        Limb,
        Head,
        Tip,
        Accessory,
        Crown
    }
}
