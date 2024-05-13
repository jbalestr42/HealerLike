using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoValidRequirement : IRequirement
{
    public bool IsValid(GameObject source)
    {
        return true;
    }
}