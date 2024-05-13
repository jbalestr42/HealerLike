using UnityEngine;

public class BuffTargetInteraction : AInteraction
{
    ABuffHandlerFactory _buffHandlerFactory;
    int _layerMask = 0;

    public BuffTargetInteraction(ABuffHandlerFactory buffHandlerFactory, LayerMask layerMask)
    {
        _buffHandlerFactory = buffHandlerFactory;
        _layerMask = layerMask.value;
    }

    public override int GetLayerMask()
    {
        return _layerMask;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        Debug.Log("[DEBUG] OnMouseClick");
        hit.transform.GetComponent<IBuffable>().AddBuffHandler(_buffHandlerFactory, hit.transform.gameObject, hit.transform.gameObject);
        InteractionManager.instance.EndInteraction();
    }

    public override void OnMouseEnter(RaycastHit hit)
    {
    }

    public override void OnMouseExit()
    {
    }

    public override void Cancel()
    {
    }

    public override void End()
    {
    }
}