using System.Collections.Generic;
using UnityEngine;

public class EntityModel : MonoBehaviour
{
    List<SkillSource> _sourcePoints = new List<SkillSource>();
    int _currentIndex = 0;

    public void Init(Entity entity)
    {
        _sourcePoints.AddRange(GetComponentsInChildren<SkillSource>());

        foreach (IVisualBehaviour visualBehaviour in GetComponentsInChildren<IVisualBehaviour>())
        {
            visualBehaviour.Init(entity);
        }
    }

    public SkillSource GetSourcePoint()
    {
        SkillSource sourcePoint = _sourcePoints[_currentIndex];
        _currentIndex = _currentIndex == _sourcePoints.Count - 1 ? 0 : _currentIndex + 1;
        return sourcePoint;
    }
}