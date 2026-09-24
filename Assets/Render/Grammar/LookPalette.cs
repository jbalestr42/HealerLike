using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // What a part's colour means, the palette turns it into a colour for one side and one accent
    // Stored by value in assets: append new members, never reorder or remove
    public enum ColourRole
    {
        Body,
        Stem,
        Limb,
        Moss,
        Ochre,
        Accent,
        BoonAccent,
        BaneAccent,
        RotAccent,
        // The colour a wilting part fades toward, the side rim under an effect, a mana element
        Wilt,
        Rim,
        Mana,
        // Scenery: a mushroom tree's stem and its two cap tints
        MushroomStem,
        MushroomCap,
        MushroomCapPale
    }

    // Every colour the look grammar uses, so a unit or an effect names a role and never a colour
    [CreateAssetMenu(menuName = "Custom/Data/Render/LookPalette")]
    public class LookPalette : ScriptableObject
    {
        public Color plantBody;
        public Color plantStem;

        public Color stoneBody;
        public Color stoneLimb;
        public Color stoneOchre;
        public Color stoneWilt;
        public Color moss;

        public Color damage;
        public Color heal;
        public Color rot;
        public Color renew;
        public Color boon;
        public Color bane;
        // Bane's navy lifted for the lit side of a part, where the dark shade would read as a hole
        public Color baneLit;

        public Color mana;

        public Color mushroomStem;
        public Color mushroomCap;
        public Color mushroomCapPale;

        public Color Accent(EffectFamily family)
        {
            switch (family)
            {
                case EffectFamily.Heal:
                    return heal;
                case EffectFamily.Rot:
                    return rot;
                case EffectFamily.Renew:
                    return renew;
                case EffectFamily.Boon:
                    return boon;
                case EffectFamily.Bane:
                    return bane;
                default:
                    return damage;
            }
        }

        // Body, stem and limb depend on the side, a plant has no limb colour of its own and uses its stem
        public Color Colour(ColourRole role, EffectFamily accent, LookSide side = LookSide.Plant)
        {
            bool isStone = side == LookSide.Stone;
            switch (role)
            {
                case ColourRole.Body:
                    if (isStone)
                    {
                        return stoneBody;
                    }
                    return plantBody;
                case ColourRole.Stem:
                case ColourRole.Limb:
                    if (isStone)
                    {
                        return stoneLimb;
                    }
                    return plantStem;
                case ColourRole.Moss:
                    return moss;
                case ColourRole.Ochre:
                    return stoneOchre;
                case ColourRole.BoonAccent:
                    return boon;
                case ColourRole.BaneAccent:
                    return bane;
                case ColourRole.RotAccent:
                    return rot;
                case ColourRole.Wilt:
                    if (isStone)
                    {
                        return stoneWilt;
                    }
                    return plantStem;
                // A plant's rim is its body, a stone's the lit bane
                case ColourRole.Rim:
                    if (isStone)
                    {
                        return baneLit;
                    }
                    return plantBody;
                case ColourRole.Mana:
                    return mana;
                case ColourRole.MushroomStem:
                    return mushroomStem;
                case ColourRole.MushroomCap:
                    return mushroomCap;
                case ColourRole.MushroomCapPale:
                    return mushroomCapPale;
                default:
                    return Accent(accent);
            }
        }
    }
}
