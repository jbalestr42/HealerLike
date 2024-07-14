using UnityEngine;

public interface IDraggable
{
    void StartDrag(RaycastHit hit);
    void Drag(RaycastHit hit);
    void EndDrag(RaycastHit hit);
    void CancelDrag();
}
