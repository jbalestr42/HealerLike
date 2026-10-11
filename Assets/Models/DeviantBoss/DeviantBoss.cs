using System.Collections;
using System.Collections.Generic;
using Oisif.Inspector;
using UnityEngine;
using UnityEngine.VFX;

public class DeviantBoss : MonoBehaviour
{
    [SerializeField] VisualEffect _vfx;

    [Button]
    public void StartAnimation()
    {
        // Add power up animation for the model ?
        _vfx.Play();
    }
}
