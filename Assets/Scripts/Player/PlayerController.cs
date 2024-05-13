using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] InputActionReference _move;
    [SerializeField] InputActionReference _startPanning;
    [SerializeField] Transform _cameraTarget;
    [SerializeField] float _moveSpeed = 40.0f;

    Camera _camera;
    Vector3 _startPosition;
    bool _moveUsingMouse = false;

    void Start()
    {
        _camera = Camera.main;
        _move.asset.Enable();
        _startPanning.asset.Enable();
        _startPanning.action.started += (InputAction.CallbackContext ctx) => { _moveUsingMouse = true; _startPosition = GetWorldPosition(); };
        _startPanning.action.canceled += (InputAction.CallbackContext ctx) => { _moveUsingMouse = false; };
    }

    void Update()
    {
        if (_moveUsingMouse)
        {
            Move(_startPosition - GetWorldPosition());
        }
        else
        {
            Vector2 input = _move.action.ReadValue<Vector2>();
            Move(new Vector3(input.x, 0f, input.y) * Time.deltaTime * _moveSpeed);
        }
    }

    void Move(Vector3 move)
    {
        _cameraTarget.Translate(move);
    }

    Vector3 GetWorldPosition()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, 0.0f);
        float distance;
        ground.Raycast(ray, out distance);
        return ray.GetPoint(distance);
    }
}