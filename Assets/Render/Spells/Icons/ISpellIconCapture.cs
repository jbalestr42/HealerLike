using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public interface ISpellIconCapture : IDisposable
    {
        Texture2D Capture(SpellIconRecipe recipe);
    }
}
