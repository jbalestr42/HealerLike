using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityGridInteraction : AInteraction
{
    EntityData _data = null;
    GameObject _entity = null;
    Entity.EntityType _entityType = Entity.EntityType.Player;
    bool _isRepeatable = false;
    System.Action<Entity> _onEntitySpawned = null;

    // isRepeatable: keep placing entities until the interaction is cancelled
    public EntityGridInteraction(EntityData data, Entity.EntityType entityType = Entity.EntityType.Player, bool isRepeatable = false, System.Action<Entity> onEntitySpawned = null)
    {
        _data = data;
        _entityType = entityType;
        _isRepeatable = isRepeatable;
        _onEntitySpawned = onEntitySpawned;
        _entity = GameObject.Instantiate(_data.model);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Terrain;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        SpawnAt(hit.point);
    }

    // Same as clicking the position, e.g. for a cell chosen by code: the entity goes on the nearest free cell
    public void SpawnAt(Vector3 position)
    {
        GameObject entity = EntityManager.instance.SpawnEntity(_data, PlayerBehaviour.instance.grid.GetNearestWalkablePosition(position), _entityType);
        if (entity != null && _onEntitySpawned != null)
        {
            _onEntitySpawned(entity.GetComponent<Entity>());
        }

        if (!_isRepeatable)
        {
            InteractionManager.instance.EndInteraction();
        }
    }

    public override void OnMouseOver(RaycastHit hit)
    {
        if (_entity)
        {
            _entity.transform.position = PlayerBehaviour.instance.grid.GetNearestWalkablePosition(hit.point);
        }
    }

    public override void Cancel()
    {
        if (_entity)
        {
            GameObject.Destroy(_entity);
            _entity = null;
        }
    }

    public override void End()
    {
        if (_entity)
        {
            GameObject.Destroy(_entity);
            _entity = null;
        }
    }
}