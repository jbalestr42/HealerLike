
using UnityEngine;

public interface IBuffable
{
    public void AddBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target);
    public void RemoveBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target);
}