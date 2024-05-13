using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuffManager))]
public class DebugBuffManager : MonoBehaviour
{
    [SerializeField] bool _debug = false;
    List<BuffManager.BuffData> _buffs = new List<BuffManager.BuffData>();
    List<BuffManager.BuffHandlerData> _buffHandlers = new List<BuffManager.BuffHandlerData>();

    void Start()
    {
        BuffManager buffManager = GetComponent<BuffManager>();
        buffManager.OnBuffAdded.AddListener(OnBuffAdded);
        buffManager.OnBuffRemoved.AddListener(OnBuffRemoved);
        buffManager.OnBuffHandlerStarted.AddListener(OnBuffHandlerStarted);
        buffManager.OnBuffHandlerStopped.AddListener(OnBuffHandlerStopped);
    }

    void OnGUI()
    {
        if (_debug)
        {
            int i = 0;
            foreach (BuffManager.BuffHandlerData buffHandler in _buffHandlers)
            {
                GUI.Label(new Rect(10, 10 + i, 1000, 20), $"{buffHandler.buffHandlerFactory.name} isInit={buffHandler.isInit} shouldRefresh={buffHandler.hasStarted} stacks={buffHandler.shouldRefresh}");
                i += 20;
            }

            foreach (BuffManager.BuffData buffData in _buffs)
            {
                GUI.Label(new Rect(10, 10 + i, 1000, 20), $"{buffData.first.ToString()} count={buffData.buffList.Count} stackable={buffData.first.isStackable} stacks={buffData.stacks}");
                i += 20;
            }
        }
    }

    void OnBuffAdded(BuffManager.BuffData buffData)
    {
        ABuff buff = buffData.first;

        if (!_buffs.Contains(buffData))
        {
            _buffs.Add(buffData);
        }
    }

    void OnBuffRemoved(BuffManager.BuffData buffData)
    {
        if (buffData.stacks == 0)
        {
            _buffs.Remove(buffData);
        }

    }

    void OnBuffHandlerStarted(BuffManager.BuffHandlerData buffHandlerData)
    {
        if (!_buffHandlers.Contains(buffHandlerData))
        {
            _buffHandlers.Add(buffHandlerData);
        }
    }

    void OnBuffHandlerStopped(BuffManager.BuffHandlerData buffHandlerData)
    {
        _buffHandlers.Remove(buffHandlerData);
    }
}