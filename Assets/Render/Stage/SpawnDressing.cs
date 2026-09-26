using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // Listens to what the game spawns and gives each one its render: a view for an entity or the healer, the
    // delivery visuals for a projectile, a pulse for an area; a spawn or a death also reframes the battle focus
    public class SpawnDressing
    {
        RenderManager _manager;
        EntityManager _entityManager;
        PlayerBehaviour _player;
        StageRangeDriver _rangeDriver;
        BattleFocus _battleFocus;
        readonly List<CreatureBuilder> _creatures = new List<CreatureBuilder>();

        public int RebuildViews()
        {
            int rebuilt = 0;
            for (int i = _creatures.Count - 1; i >= 0; i--)
            {
                if (!_creatures[i])
                {
                    _creatures.RemoveAt(i);
                    continue;
                }
                if (_creatures[i].Rebuild(_manager))
                {
                    rebuilt++;
                }
            }
            return rebuilt;
        }

        public void Init(RenderManager manager, StageRangeDriver rangeDriver, BattleFocus battleFocus)
        {
            Clear();
            _manager = manager;
            _entityManager = manager.entityManager;
            _player = manager.player;
            _rangeDriver = rangeDriver;
            _battleFocus = battleFocus;
            _entityManager.OnEntitySpawned.AddListener(OnEntitySpawned);
            _entityManager.OnEntityKilled.AddListener(OnEntityKilled);
            _entityManager.OnProjectileSpawned.AddListener(OnProjectileSpawned);
            _player.OnCharacterInit.AddListener(OnCharacterInit);
        }

        public void Clear()
        {
            if (_entityManager != null)
            {
                _entityManager.OnEntitySpawned.RemoveListener(OnEntitySpawned);
                _entityManager.OnEntityKilled.RemoveListener(OnEntityKilled);
                _entityManager.OnProjectileSpawned.RemoveListener(OnProjectileSpawned);
            }

            if (_player != null)
            {
                _player.OnCharacterInit.RemoveListener(OnCharacterInit);
            }

            _entityManager = null;
            _player = null;
            _creatures.Clear();
        }

        void OnEntitySpawned(Entity entity)
        {
            DressEntity(entity);
            _battleFocus.MarkDirty();
        }

        // A stone collapses by itself at zero health, the camera only reframes
        void OnEntityKilled(Entity entity)
        {
            _battleFocus.MarkDirty();
        }

        void OnCharacterInit(Character character)
        {
            DressCharacter(character);
        }

        // Areas of effect spawn through the projectile pool too
        void OnProjectileSpawned(GameObject spawnedGo)
        {
            Projectile projectile = spawnedGo.GetComponent<Projectile>();
            if (projectile != null)
            {
                DressProjectile(projectile);
                return;
            }

            AreaOfEffect area = spawnedGo.GetComponent<AreaOfEffect>();
            if (area != null)
            {
                DressArea(area);
            }
        }

        // The model's sockets and HUD stay live, so projectiles leave from where gameplay puts them
        void DressEntity(Entity entity)
        {
            if (entity.model == null)
            {
                return;
            }

            foreach (Renderer modelRenderer in entity.model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.enabled = false;
            }

            GameObject viewPrefab = _manager.creatureLooks.GetView(entity.data, entity.entityType);
            GameObject viewGo = Object.Instantiate(viewPrefab, entity.model.transform);
            foreach (IEntityView view in viewGo.GetComponentsInChildren<IEntityView>())
            {
                view.Init(entity, _manager);
                if (view is CreatureBuilder creature)
                {
                    _creatures.Add(creature);
                }
            }

            // Ground shadows and trample clearings measure the grown geometry during IEntityView.Init.
            // Appearance changes only the rendered pose after those one-time measurements are complete.
            foreach (CreatureBuilder creature in viewGo.GetComponentsInChildren<CreatureBuilder>())
            {
                creature.BeginAppearance();
            }

            foreach (RangePreview preview in viewGo.GetComponentsInChildren<RangePreview>())
            {
                _rangeDriver.Add(preview);
            }

            // The creature's health reads in the grass around it
            viewGo.AddComponent<GroundAura>().Init(entity, _manager.zones, StageCalibration.CellSize);
        }

        void DressCharacter(Character character)
        {
            GameObject viewGo = Object.Instantiate(_manager.creatureLooks.GetView(character.data), character.transform);
            CharacterView view = viewGo.GetComponent<CharacterView>();
            view.Init(character, _manager);
            foreach (HealPulse pulse in viewGo.GetComponentsInChildren<HealPulse>())
            {
                pulse.Init(character.gameObject, _manager.registry, _manager.zones);
            }

            foreach (TrampleZone trample in viewGo.GetComponentsInChildren<TrampleZone>())
            {
                trample.enabled = view.showBody;
                if (view.showBody)
                {
                    trample.InitFootprint(_manager.zones);
                }
            }
        }

        // EntityManager raises OnProjectileSpawned before Projectile.Init, which then sets the projectile on these
        // and calls their Init(source) with its own behaviours
        void DressProjectile(Projectile projectile)
        {
            GameObject projectileGo = projectile.gameObject;
            // A spawned projectile carries no link to its prefab, so its delivery is read from its baked behaviours
            DeliveryStyle style = EffectDerivation.Delivery(projectileGo);
            projectileGo.AddComponent<ProjectileVisualObserver>().Init(_manager, style);
            projectileGo.AddComponent<StoneProjectileImpactBridge>();
            projectileGo.AddComponent<LaunchWave>().Init(_manager.zones, _manager.gust);

            foreach (LineRenderer line in projectileGo.GetComponentsInChildren<LineRenderer>(true))
            {
                line.enabled = false;
            }
        }

        // The area is dressed at spawn, before its caller sets the source and radius, so the pulse that reads them
        // waits for Start
        void DressArea(AreaOfEffect area)
        {
            RenderManager manager = _manager;
            area.gameObject.AddComponent<AreaPulseOnStart>().Init(() =>
                manager.spellSink.PulseArea(area.transform.position, area.radius, AreaKind(area.source), 1f));
            area.gameObject.AddComponent<LegacyAreaVisualMask>();
        }

        // An area whose every on hit consumer heals pulses as a heal, anything else as hostile
        static ZoneKind AreaKind(GameObject source)
        {
            IAttacker attacker = null;
            if (source != null)
            {
                attacker = source.GetComponent<IAttacker>();
            }

            if (attacker == null)
            {
                return ZoneKind.Hostile;
            }

            List<AConsumerFactory> consumers = attacker.GetOnHitConsumers();
            if (consumers == null || consumers.Count == 0)
            {
                return ZoneKind.Hostile;
            }

            foreach (AConsumerFactory consumer in consumers)
            {
                if (EffectDerivation.ConsumerFamily(consumer, false) != EffectFamily.Heal)
                {
                    return ZoneKind.Hostile;
                }
            }

            return ZoneKind.Heal;
        }
    }
}
