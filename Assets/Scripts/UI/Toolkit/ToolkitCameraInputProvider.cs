using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// The board camera's orbit input (the mouse wheel zooms it). Over an interface panel the wheel scrolls that panel
// instead, so the camera reads no input while the mouse is over one.
#pragma warning disable CS0618 // The scenes' FreeLook cameras still read their input through the CM2 provider
public class ToolkitCameraInputProvider : CinemachineInputProvider
#pragma warning restore CS0618
{
    readonly List<RaycastResult> _hits = new List<RaycastResult>();

    public override float GetAxisValue(int axis)
    {
        return IsPointerOverInterface() ? 0f : ReadAxis(axis);
    }

    protected virtual float ReadAxis(int axis)
    {
        return base.GetAxisValue(axis);
    }

    protected virtual bool IsPointerOverInterface()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && ToolkitTouchInput.IsPointOverInterface(mouse.position.ReadValue(), _hits);
    }
}
