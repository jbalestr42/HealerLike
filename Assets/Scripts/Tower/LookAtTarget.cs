using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTarget : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] float _speed = 1f;
    TargetProvider _targetProvider;
    Vector3 _originalForward;
    bool _isInitialized = false;

    public void Init(Entity entity)
    {
        _isInitialized = true;
        _originalForward = transform.forward;
        _targetProvider = entity.GetComponent<TargetProvider>();
    }

    void Update()
    {
        if (_isInitialized)
        {
            var targets = _targetProvider.GetTargets();
            Vector3 targetPos = targets.Count > 0 ? targets[0].transform.position : transform.position + _originalForward;
            targetPos.y = transform.position.y; //set targetPos y equal to mine, so I only look at my own plane
            var targetDir = Quaternion.LookRotation(targetPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetDir, _speed * Time.deltaTime);
        }
    }
}
