using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Spells
{
    public partial class SpellEffect
    {
        GroundHandle _groundHandle;
        Transform _groundTarget;
        public void BindGround(Ground ground, GameObject target)
        {
            ReleaseGround();
            foreach (SpellEffect layer in _layers) layer.BindGround(ground, target);
            if (ground == null || _recipe?.entry.ground == null || target == null) return;
            _groundTarget = target.transform;
            _groundHandle = ground.Hold(_recipe.entry.ground);
            UpdateGround();
        }
        void UpdateGround()
        {
            if (_groundHandle == null) return;
            if (_groundTarget == null || !_groundTarget.gameObject.activeInHierarchy || !gameObject.activeInHierarchy)
            {
                _groundHandle.Hide();
                return;
            }
            if (_recipe.entry.ground.shape == GroundShape.Line)
                _groundHandle.ShowLine(_linkStart, _linkEnd, _recipe.entry.groundStrength);
            else
                _groundHandle.Show(_groundTarget.position, _recipe.entry.groundRadius * _recipe.scale,
                    _recipe.entry.groundStrength);
        }
        void ReleaseGround() { _groundHandle?.Release(); _groundHandle = null; _groundTarget = null; }
        // Composite layers disable their own Update; their parent therefore owns deactivation cleanup too.
        void ReleaseGroundTree()
        {
            ReleaseGround();
            foreach (SpellEffect layer in _layers) if (layer) layer.ReleaseGroundTree();
        }
        void OnDisable() => ReleaseGroundTree();

        // A new element under the parent; on a unit, its body and stem parts take the unit's side
        public static SpellEffect Create(EffectRecipe recipe, Transform parent, PrimitiveMeshes meshes,
            Material material, GameObject target)
        {
            if (meshes == null || !EffectValidator.TryValidate(recipe, out _))
            {
                return null;
            }

            LookSide targetSide = LookSide.Plant;
            Entity entity = null;
            if (target != null)
            {
                entity = target.GetComponent<Entity>();
            }

            if (entity != null)
            {
                targetSide = LookDerivation.Side(entity.entityType);
            }

            GameObject effectGo = new GameObject(recipe.element.ToString());
            effectGo.transform.SetParent(parent, false);
            SpellEffect effect = effectGo.AddComponent<SpellEffect>();
            effect.Init(recipe, meshes, material, targetSide);
            return effect;
        }

        // Also runs from edit mode tests, where Destroy is not allowed
        public static void Dispose(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            // EditMode previews may never enter Unity's native lifecycle; release ownership explicitly.
            SpellEffect owner = effect.GetComponent<SpellEffect>();
            if (owner) owner.ReleaseGroundTree();
            effect.SetActive(false);
            RenderObjects.Release(effect);
        }
    }
}
