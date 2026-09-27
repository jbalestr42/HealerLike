using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public partial class SpellEffect
    {
        readonly List<SpellEffect> _layers = new List<SpellEffect>();
        void BuildLayers(PrimitiveMeshes meshes, Material material, LookSide side)
        {
            foreach (EffectRecipe addition in _recipe.additions ?? System.Array.Empty<EffectRecipe>())
            {
                var root = new GameObject("Spell layer " + addition.element);
                root.transform.SetParent(transform, false);
                SpellEffect layer = root.AddComponent<SpellEffect>();
                layer.enabled = false; // The owning effect advances all layer clocks exactly once.
                layer.Init(addition, meshes, material, side);
                _layers.Add(layer);
            }
        }
        public void PlaceLayers(EffectAnchors anchors)
        {
            foreach (SpellEffect layer in _layers) EffectPlacement.Place(layer, transform, anchors);
        }
        void AdvanceLayers(float delta)
        {
            foreach (SpellEffect layer in _layers) layer.Advance(delta);
        }
        float RemovalSeconds() => _recipe?.presentation != null && _recipe.presentation.enabled
            ? _recipe.presentation.releaseSeconds : removalSeconds;
        bool LayersRemoved()
        {
            foreach (SpellEffect layer in _layers) if (!layer.removalComplete) return false;
            return true;
        }
        float Visibility() => _isRemoving
            ? 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01(_removalAge / RemovalSeconds()))
            : EffectEnvelope.Visibility(_recipe.presentation, _age, _recipe.cycleSeconds, _isStatus);
        float CompositeLifetime()
        {
            float seconds = _recipe != null ? _recipe.cycleSeconds : 0;
            foreach (SpellEffect layer in _layers) seconds = Mathf.Max(seconds, layer.lifetime);
            return seconds;
        }
    }
}
