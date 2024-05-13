using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class AInteraction {

    public abstract int GetLayerMask();
    public virtual void OnMouseClick(RaycastHit hit) { }
    public virtual void OnMouseEnter(RaycastHit hit) { }
    public virtual void OnMouseOver(RaycastHit hit) { }
    public virtual void OnMouseExit() { }
    public virtual void Cancel() { }
    public virtual void End() { }
}