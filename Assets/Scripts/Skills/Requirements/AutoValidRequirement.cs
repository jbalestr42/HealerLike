using UnityEngine;

public class AutoValidRequirement : IRequirement
{
    #region IRequirement

    public bool IsValid(GameObject source)
    {
        return true;
    }

    #endregion
}