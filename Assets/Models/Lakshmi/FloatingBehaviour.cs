using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingBehaviour : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] float _speed = 1f;
    Vector3 _originalPosition;
    bool _isInitialized = false;

    public void Init(Entity entity)
    {
        _originalPosition = transform.localPosition;
        _isInitialized = true;
    }

    void Update()
    {
        if (_isInitialized)
        {
            transform.localPosition = _originalPosition + new Vector3(0f, Mathf.SmoothStep(0f, 0.1f, Mathf.PingPong(Time.time * _speed, 1f)), 0f);
        }
    }
}
