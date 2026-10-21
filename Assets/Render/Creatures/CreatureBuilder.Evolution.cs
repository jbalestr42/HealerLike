using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    public partial class CreatureBuilder
    {
        readonly CreatureEvolution _evolution = new CreatureEvolution();

        void ObserveEvolution()
        {
            if (!_derivedRecipe || !_manager || !_manager.creatureLooks)
            {
                return;
            }
            // Entity publishes this reference after its simulation attributes are initialized.
            // A prefab or EditMode preview can carry an unawakened component; keep its data-derived look.
            _evolution.Init(_entity.data, _entity.entityType, _entity.attributeManager);
        }

        void RefreshEvolution()
        {
            if (!_derivedRecipe || !_manager || !_manager.creatureLooks
                || !_evolution.TryRead(out UnitChannels channels))
            {
                return;
            }

            SyncGeometry();
            CreatureRecipe next = LookComposer.Compose(channels, _manager.creatureLooks.vocabulary);
            if (!ApplyRecipe(next, true))
            {
                return;
            }

            _evolution.Accept(channels);
            rig.Heal();
        }

        // Recompose only the view. The owner, health subscriptions and delivery leases stay live.
        public bool Rebuild(RenderManager manager)
        {
            if (manager != _manager || !_entity || rig == null)
            {
                return false;
            }

            SyncGeometry();
            CreatureRecipe next = _recipe;
            bool isDerived = _derivedRecipe != null;
            if (isDerived)
            {
                if (!manager || !manager.creatureLooks)
                {
                    return false;
                }

                ObserveEvolution();
                next = LookComposer.Compose(_evolution.ReadCurrent(), manager.creatureLooks.vocabulary);
            }

            bool accepted = ApplyRecipe(next, isDerived);
            if (accepted && isDerived)
            {
                _evolution.Accept(_evolution.ReadCurrent());
            }

            return accepted;
        }

        bool ApplyRecipe(CreatureRecipe next, bool isDerived)
        {
            if (next == null || !rig.Recompose(next, _material, _bodyMaterial, _meshes))
            {
                if (isDerived)
                {
                    RenderObjects.Release(next);
                }

                return false;
            }

            if (isDerived)
            {
                RenderObjects.Release(_derivedRecipe);
                _derivedRecipe = next;
            }

            _recipe = next;
            RefreshArms();
            _readout.Read();
            rig.SetReadout(_readout.target, _readout.healthFraction, _readout.readiness, _readout.readiness);
            // Recompose leaves complete geometry for structural measurements, before restoring its growth pose.
            HealerLike.Render.Zones.TrampleZone trample = GetComponent<HealerLike.Render.Zones.TrampleZone>();
            if (trample != null)
            {
                trample.Refresh();
            }

            HealerLike.Render.Stones.StoneBody stone = GetComponent<HealerLike.Render.Stones.StoneBody>();
            if (stone != null)
            {
                stone.RefreshRig();
            }

            TickPresentation(0f);
            return true;
        }

    }
}
