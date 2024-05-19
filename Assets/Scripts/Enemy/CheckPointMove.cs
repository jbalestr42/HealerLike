using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class CheckPointMove : MonoBehaviour
{
    [SerializeField] AILerp _aiLerp;
    Attribute _speed;
    public float speed => _speed.Value;

    CheckPoint _destination;
    public CheckPoint destination { get { return _destination; } set { _destination = value; } }

    void Start()
    {
        AttributeManager attributeManager = GetComponent<AttributeManager>();
        _speed = attributeManager.Get(AttributeType.Speed);
    }

    void Update()
    {
        if (_aiLerp != null)
        {
            _aiLerp.speed = _speed.Value;
        }
    }

    public void SetNextDestination(CheckPoint destination)
    {
        _destination = destination;
        _aiLerp.destination = destination.transform.position;
    }

    void OnTriggerEnter(Collider collider)
    {
        CheckPoint checkpoint = collider.gameObject.GetComponent<CheckPoint>();
        if (checkpoint != null && checkpoint == _destination)
        {
            if (checkpoint.isLast)
            {
                EntityManager.instance.DestroyEntity(gameObject, Entity.EntityType.Computer);
            }
            else
            {
                SetNextDestination(checkpoint.next);
            }
        }
    }
}
