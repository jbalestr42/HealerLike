using UnityEngine;

public interface IStackableBuff
{
    public void Stack(GameObject source, GameObject target);
    public void Unstack(GameObject source, GameObject target);
}