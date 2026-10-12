using UnityEngine;

public interface IRequirement
{
    bool IsValid(GameObject source);
}