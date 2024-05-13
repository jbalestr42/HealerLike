using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    Transform _cameraTransform;
    Quaternion _originalRotation;

    void Start()
    {
        Camera camera = Camera.main;
        _cameraTransform = camera.transform;
        GetComponent<Canvas>().worldCamera = camera;
        _originalRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = _cameraTransform.rotation * _originalRotation;        
    }
}
