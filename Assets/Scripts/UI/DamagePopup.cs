using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    [SerializeField] AnimationCurve _opacity;
    [SerializeField] float _duration = 1f;
    [SerializeField] float _maxSpeed = 0.5f;

    [SerializeField] Material _positiveMaterial;
    [SerializeField] Material _negativeMaterial;

    Transform _cameraTransform;
    Quaternion _originalRotation;
    float _time = 0f;
    Color _color;
    Vector3 _direction;
    float _speed;

    void Start()
    {
        Camera camera = Camera.main;
        _cameraTransform = camera.transform;
        _originalRotation = transform.rotation;
        Destroy(gameObject, _duration);
    }

    public void Init(GameObject source, float value)
    {
        _text.text = value.ToString("F0");
        _text.fontSharedMaterial = value > 0f ? _positiveMaterial : _negativeMaterial;
        _color = _text.color;
        _direction = -Vector3.Normalize(source.transform.position - transform.position);
        _direction += AddNoiseOnAngle(0f, 30f);
        _speed = Math.RemapClamped(Vector3.Distance(source.transform.position, transform.position), 0f, 10f, 0f, _maxSpeed);
    }
 
    Vector3 AddNoiseOnAngle(float min, float max)
    {
        return new Vector3( 
            Mathf.Sin(2f * Mathf.PI * Random.Range(min, max) / 360f), 
            Mathf.Sin(2f * Mathf.PI * Random.Range(min, max) / 360f), 
            Mathf.Sin(2f * Mathf.PI * Random.Range(min, max) / 360f));
    }

    void Update()
    {
        _time += Time.deltaTime;
        transform.position += _direction * Time.deltaTime * _speed;
        _color.a = _opacity.Evaluate(_time / _duration);
        _text.color = _color;
        transform.rotation = _cameraTransform.rotation * _originalRotation;        
    }
}
