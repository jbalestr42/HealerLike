using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The spell preview's runtime effect: built from a composed recipe, placed on its socket and set to the preset's
    // scale, stacks, side and critical. It is disabled, so only the preview's timeline moves it, in play mode too.
    public static class SpellPreviewEffect
    {
        // A link spans these two points in front of the target
        static readonly Vector3 linkStart = new Vector3(-0.9f, 0.6f, 0f);
        static readonly Vector3 linkEnd = new Vector3(0.9f, 0.6f, 0f);

        public static SpellEffect Build(StudioPreviewScene scene, SpellStudioPreset preset, EffectRecipe recipe,
            SpellPreviewTarget target)
        {
            GameObject effectObject = scene.AddChild("Preview Spell");
            SpellEffect effect = effectObject.AddComponent<SpellEffect>();
            effect.enabled = false;
            effect.Init(recipe, scene.meshes, scene.material, target.side);
            Place(effect, recipe.socket, scene, target);
            effect.transform.localScale *= preset.safeScale;
            if (recipe.tempo != EffectTempo.Once)
            {
                effect.SetStatus(preset.safeStacks, 0f, preset.previewDuration);
            }

            effect.SetSide(preset.safeSide);
            if (preset.critical)
            {
                effect.ShowCritical();
            }

            StudioPreviewScene.HideTree(effectObject);
            return effect;
        }

        static void Place(SpellEffect effect, EffectSocket socket, StudioPreviewScene scene, SpellPreviewTarget target)
        {
            if (socket == EffectSocket.Link)
            {
                effect.SetEndpoints(linkStart, linkEnd, false);
                return;
            }

            if (socket == EffectSocket.Ground)
            {
                effect.transform.localPosition = Vector3.up * 0.02f;
                return;
            }
            EffectPlacement.Place(effect, scene.root.transform, target.GetAnchors());
        }
    }
}
