using System.Collections.Generic;
using UnityEngine;

public class TowerModel : MonoBehaviour
{
    List<SkillSource> _sourcePoints = new List<SkillSource>();
    int _currentIndex = 0;

    public void Init(Tower tower)
    {
        _sourcePoints.AddRange(GetComponentsInChildren<SkillSource>());

        Debug.Log("Must fix IVisualBehaviour with tower");
        // foreach (IVisualBehaviour visualBehaviour in GetComponentsInChildren<IVisualBehaviour>())
        // {
        //     visualBehaviour.Init(tower);
        // }
    }

    public SkillSource GetSourcePoint()
    {
        SkillSource sourcePoint = _sourcePoints[_currentIndex];
        _currentIndex = _currentIndex == _sourcePoints.Count - 1 ? 0 : _currentIndex + 1;
        return sourcePoint;
    }
}