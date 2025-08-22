using System.Collections.Generic;
using UnityEngine;

public interface ITargetProvider
{
    List<GameObject> GetTargets();
    int targetCount { get; set; }
    TargetBehaviourType targetBehaviourType { get; set; }
}