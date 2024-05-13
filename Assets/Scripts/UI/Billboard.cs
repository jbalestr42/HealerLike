using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        //transform.LookAt(_mainCamera.transform);
        //transform.Rotate(0, 180, 0);
        transform.rotation = _mainCamera.transform.rotation;
    }
}
