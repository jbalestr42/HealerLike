using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // A cosmetic consumer of the same authored view as a live creature. Never instantiates its gameplay host.
    public class CreaturePreview : IDisposable
    {
        CreatureRecipe _derivedRecipe;
        CreatureRig _rig;
        bool _disposed;
        public CreatureRig rig
        {
            get { return _rig; }
        }

        public bool Init(
            CreatureLooks looks,
            EntityData data,
            Entity.EntityType side,
            PrimitiveMeshes meshes,
            Transform parent,
            float cellSize
        )
        {
            if (_disposed || _rig != null || !looks || !data || !parent)
            {
                return false;
            }

            GameObject view = looks.GetView(data, side);
            CreatureBuilder host = view ? view.GetComponentInChildren<CreatureBuilder>(true) : null;
            if (!host || !host.material)
            {
                return false;
            }

            CreatureRecipe recipe = host.recipe;
            if (!recipe)
            {
                _derivedRecipe = looks.GetRecipe(data, side);
                recipe = _derivedRecipe;
            }

            if (!recipe)
            {
                return false;
            }

            CreatureRig created = new CreatureRig();
            if (
                !created.Init(
                    recipe,
                    parent,
                    host.material,
                    host.bodyMaterial,
                    host.meshes ? host.meshes : meshes,
                    cellSize
                )
            )
            {
                created.Dispose();
                RenderObjects.Release(_derivedRecipe);
                _derivedRecipe = null;
                return false;
            }

            _rig = created;
            _rig.BeginAppearance();
            return true;
        }

        public void Tick(float time, float deltaTime, FootFrame frame, Vector3? presentationForward)
        {
            if (_disposed || _rig == null)
            {
                return;
            }

            _rig.SetPresentationForward(presentationForward);
            _rig.AdvanceAppearance(deltaTime);
            _rig.Tick(time, deltaTime, frame);
        }

        // Request before Tick when capturing a portrait or rebuilding an already visible preview.
        public void CompleteAppearance()
        {
            if (_rig != null)
            {
                _rig.CompleteAppearance();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_rig != null)
            {
                _rig.Dispose();
            }

            _rig = null;
            RenderObjects.Release(_derivedRecipe);
            _derivedRecipe = null;
        }
    }
}
