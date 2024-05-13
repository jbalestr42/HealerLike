using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInteraction : AInteraction
{
    TowerData _data = null;
    TowerRange _range = null;
    GameObject _tower = null;

    public GridInteraction(TowerData data, TowerRange range)
    {
        _data = data;
        _tower = GameObject.Instantiate(_data.model);

        _range = GameObject.Instantiate(range);
        _range.Show(_data.attributes[AttributeType.Range]);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Terrain;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        GameObject tower = EntityManager.instance.SpawnTower(_data, PlayerBehaviour.instance.grid.GetNearestWalkablePosition(hit.point));
        InteractionManager.instance.EndInteraction();
    }

    public override void OnMouseOver(RaycastHit hit)
    {
        if (_tower)
        {
            _tower.transform.position = PlayerBehaviour.instance.grid.GetNearestWalkablePosition(hit.point);
            _range.transform.position = _tower.transform.position;
        }
    }

    public override void Cancel()
    {
        if (_tower)
        {
            GameObject.Destroy(_tower);
            GameObject.Destroy(_range.gameObject);
            _tower = null;
        }
    }

    public override void End()
    {
        if (_tower)
        {
            GameObject.Destroy(_tower);
            GameObject.Destroy(_range.gameObject);
            _tower = null;
        }
    }
}