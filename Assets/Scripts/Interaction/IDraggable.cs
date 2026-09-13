using UnityEngine;

public interface IDraggable
{
    bool CanDrag();
    void StartDrag(RaycastHit hit);
    void Drag(RaycastHit hit);
    void EndDrag(RaycastHit hit);
    void CancelDrag();
}
