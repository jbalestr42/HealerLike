
using UnityEngine;

public interface IBuffable
{
    public void AddBuffHandler(ABuffHandlerFactory buffFactory, GameObject source, GameObject target);
    public void AddBuff(ABuffFactory buffFactory, GameObject source, GameObject target);
    public void RemoveBuff(ABuffFactory buffFactory, GameObject source, GameObject target);
}