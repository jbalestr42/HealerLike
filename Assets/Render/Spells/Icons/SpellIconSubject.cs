using System;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // A private, static pose of the same geometry that the live spell builds. Never mutates a vocabulary.
    public sealed class SpellIconSubject : IDisposable
    {
        public static readonly int MaxGlyphs = 4;
        readonly GameObject _root;
        public GameObject root => _root;
        readonly SpellIconSeal _seal;

        public SpellIconSubject(SpellIconRecipe recipe, PrimitiveMeshes meshes, Material material, Vector3 origin)
        {
            if (!IsValid(recipe))
            {
                throw new ArgumentException("Require a bounded, valid spell icon composition.");
            }
            _root = new GameObject("Spell icon subject") { hideFlags = HideFlags.HideAndDontSave };
            _root.transform.position = origin;
            _seal = new SpellIconSeal(_root.transform, material);
            try
            {
                int shown = Mathf.Min(MaxGlyphs, recipe.layers.Count);
                for (int i = 0; i < shown; i++)
                {
                    BuildLayer(recipe.layers[i], meshes, material, i, shown);
                }
                _seal.Build(recipe);
                foreach (Transform part in _root.GetComponentsInChildren<Transform>(true))
                {
                    part.gameObject.layer = SpellIconRenderer.CaptureLayer;
                    part.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        void BuildLayer(EffectRecipe source, PrimitiveMeshes meshes, Material material, int index, int count)
        {
            EffectRecipe recipe = EffectRecipeCopy.Copy(source);
            if (!EffectValidator.TryValidate(recipe, out string problem))
            {
                throw new ArgumentException("Invalid spell icon layer: " + problem);
            }

            GameObject host = new GameObject("Spell glyph " + index);
            host.transform.SetParent(_root.transform, false);
            SpellEffect effect = host.AddComponent<SpellEffect>();
            effect.enabled = false;
            effect.Init(recipe, meshes, material, LookSide.Plant);
            if (recipe.socket == EffectSocket.Link)
            {
                Vector3 middle = _root.transform.position;
                effect.SetEndpoints(middle + Vector3.left * 1.1f, middle + Vector3.right * 1.1f, false);
                effect.Advance(recipe.cycleSeconds * .4f);
            }
            else
            {
                effect.Pose(.42f, recipe.cycleSeconds * .42f);
                host.transform.localRotation = recipe.presentation.billboard ? Quaternion.identity
                    : Quaternion.Euler(recipe.socket == EffectSocket.Ground ? 65f : 30f, -18f, 0f);
            }

            float extent = count == 1 ? .92f : count == 2 ? .58f : .46f;
            Fit(host.transform, Slot(index, count), extent);
        }

        static void Fit(Transform glyph, Vector3 centre, float extent)
        {
            if (!BoundsOf(glyph, out Bounds bounds))
            {
                throw new InvalidOperationException("Spell icon layer has no visible geometry.");
            }
            float largest = Mathf.Max(.001f, Mathf.Max(bounds.extents.x, bounds.extents.y));
            glyph.localScale *= extent / largest;
            BoundsOf(glyph, out bounds);
            glyph.position += glyph.parent.TransformPoint(centre) - bounds.center;
        }

        public static Vector3 Slot(int index, int count)
        {
            if (count == 1)
            {
                return new Vector3(0, .08f, 0);
            }
            if (count == 2)
            {
                return new Vector3(index == 0 ? -.53f : .53f, index == 0 ? .3f : -.2f, 0);
            }
            if (count == 3)
            {
                return index == 0 ? new Vector3(0, .55f, 0)
                    : new Vector3(index == 1 ? -.52f : .52f, -.38f, 0);
            }
            return new Vector3(index % 2 == 0 ? -.52f : .52f, index < 2 ? .53f : -.43f, 0);
        }

        static bool BoundsOf(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < .000001f)
                {
                    continue;
                }
                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }
            return found;
        }

        public static bool IsValid(SpellIconRecipe recipe)
        {
            if (recipe?.layers == null || recipe.layers.Count == 0 || recipe.layers.Count > EffectValidator.MaxParts)
            {
                return false;
            }
            long parts = 0;
            foreach (EffectRecipe layer in recipe.layers)
            {
                if (!EffectValidator.TryValidate(layer, out _))
                {
                    return false;
                }
                var pending = new System.Collections.Generic.Stack<EffectRecipe>();
                pending.Push(layer);
                while (pending.Count > 0)
                {
                    EffectRecipe item = pending.Pop();
                    ElementEntry entry = item.entry;
                    parts += entry.parts.Length + (entry.stackBeads?.Length ?? 0)
                        + (entry.criticalRings?.Length ?? 0) + (entry.sideRim?.Length ?? 0);
                    if (parts > EffectValidator.MaxParts)
                    {
                        return false;
                    }
                    foreach (EffectRecipe child in item.additions ?? Array.Empty<EffectRecipe>())
                    {
                        pending.Push(child);
                    }
                }
            }
            return true;
        }

        public void Dispose()
        {
            if (_root)
            {
                _root.SetActive(false);
            }
            _seal.Dispose();
            RenderObjects.Release(_root);
        }
    }
}
