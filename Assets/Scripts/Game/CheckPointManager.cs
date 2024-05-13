using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : Singleton<CheckPointManager>
{
    [SerializeField] List<CheckPoint> _checkPoints;

    public CheckPoint start => _checkPoints[0];

    public void Add(CheckPoint checkPoint)
    {
        _checkPoints.Add(checkPoint);
    }

    public void Clear()
    {
        _checkPoints.Clear();
    }
}
