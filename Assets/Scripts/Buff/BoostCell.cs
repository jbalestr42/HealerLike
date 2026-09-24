using System.Collections.Generic;
using UnityEngine;

// Spawned by BoostEntitiesOnRelativeCellBuff on a relative cell - applies its buff to whichever
// entity is physically standing on it, and removes it again as soon as the entity leaves, so
// moving either the source or the boosted entity keeps the buff correct without any extra bookkeeping.
public class BoostCell : MonoBehaviour
{
    GameObject _source;
    ABuffHandlerFactory _buffHandlerFactory;
    Entity.EntityType _entityType;
    List<GameObject> _boostedEntities = new List<GameObject>();

    public void Init(GameObject source, ABuffHandlerFactory buffHandlerFactory)
    {
        _source = source;
        _buffHandlerFactory = buffHandlerFactory;
        _entityType = source.GetComponent<Entity>().entityType;
    }

    void OnTriggerEnter(Collider other)
    {
        Entity entity = other.GetComponent<Entity>();
        if (entity == null || entity.gameObject == _source || entity.entityType != _entityType)
        {
            return;
        }

        entity.GetComponent<BuffManager>().AddHandler(_buffHandlerFactory, _source, entity.gameObject);
        _boostedEntities.Add(entity.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        Entity entity = other.GetComponent<Entity>();
        if (entity == null || !_boostedEntities.Contains(entity.gameObject))
        {
            return;
        }

        entity.GetComponent<BuffManager>().RemoveHandler(_buffHandlerFactory, _source, entity.gameObject);
        _boostedEntities.Remove(entity.gameObject);
    }

    void OnDestroy()
    {
        foreach (GameObject entity in _boostedEntities)
        {
            if (entity != null)
            {
                entity.GetComponent<BuffManager>().RemoveHandler(_buffHandlerFactory, _source, entity);
            }
        }
        _boostedEntities.Clear();
    }
}
