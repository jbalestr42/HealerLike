using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerRange : MonoBehaviour
{
    [SerializeField] MeshRenderer _renderer;

    public void Show(float range)
    {
        transform.localScale = new Vector3(range, range, range) * 2f;
        _renderer.enabled = true;
    }

    public void Hide()
    {
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }
}
