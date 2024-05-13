using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityGridInteraction : AInteraction
{
    EntityData _data = null;
    GameObject _entity = null;

    public EntityGridInteraction(EntityData data)
    {
        _data = data;
        _entity = GameObject.Instantiate(_data.model);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Terrain;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        GameObject tower = EntityManager.instance.SpawnEntity(_data, PlayerBehaviour.instance.grid.GetNearestWalkablePosition(hit.point), Entity.EntityType.Player);
        InteractionManager.instance.EndInteraction();
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