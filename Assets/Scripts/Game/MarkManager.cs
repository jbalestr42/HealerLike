using UnityEngine;

public class MarkManager : Singleton<MarkManager>
{
    GameObject _markedEntity;
    // The enemy the units following the mark attack first (see the Mark spell), null when none
    public GameObject markedEntity => _markedEntity;

    void Start()
    {
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    void OnEntityKilled(Entity entity)
    {
        if (IsEntityMarked(entity.gameObject))
        {
            ResetMark();
        }
    }

    public bool IsEntityMarked(GameObject entity)
    {
        return entity == _markedEntity;
    }

    public void MarkEntity(GameObject markedEntity)
    {
        if (markedEntity.GetComponent<IMarkable>() != null)
        {
            if (_markedEntity != null)
            {
                _markedEntity.GetComponent<IMarkable>().UnMark();
            }
            _markedEntity = markedEntity;
            _markedEntity.GetComponent<IMarkable>().Mark();
        }
    }

    public void UnMarkEntity()
    {
        if (_markedEntity != null)
        {
            _markedEntity.GetComponent<IMarkable>().UnMark();
            _markedEntity = null;
        }
    }

    public void ResetMark()
    {
        _markedEntity = null;
    }
}