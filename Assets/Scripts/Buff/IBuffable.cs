
using UnityEngine;

public interface IBuffable
{
    public void AddBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target);
    public void RemoveBuffHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target);
    public void AddBuff(ABuffFactory buffFactory, GameObject source, GameObject target);
    public void RemoveBuff(ABuffFactory buffFactory, GameObject source, GameObject target);
}