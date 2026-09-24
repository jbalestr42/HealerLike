using System.Collections.Generic;
using UnityEngine.Events;

// Tracks the active buff handlers of a BuffManager that have an icon, grouped by handler factory
// (the same effect applied by several sources is displayed once, with the sum of its stacks)
public class BuffIconTracker
{
    // Handler factory, total stacks (0 = the effect is not active anymore)
    public UnityEvent<ABuffHandlerFactory, int> OnStacksChanged = new UnityEvent<ABuffHandlerFactory, int>();

    Dictionary<ABuffHandlerFactory, List<BuffManager.BuffHandlerData>> _handlersPerFactory = new Dictionary<ABuffHandlerFactory, List<BuffManager.BuffHandlerData>>();

    public BuffIconTracker(BuffManager buffManager)
    {
        buffManager.OnBuffHandlerRefreshed.AddListener(OnBuffHandlerRefreshed);
        buffManager.OnBuffHandlerStopped.AddListener(OnBuffHandlerStopped);
    }

    public int GetStacks(ABuffHandlerFactory buffHandlerFactory)
    {
        int stacks = 0;
        if (_handlersPerFactory.TryGetValue(buffHandlerFactory, out List<BuffManager.BuffHandlerData> handlers))
        {
            foreach (BuffManager.BuffHandlerData handler in handlers)
            {
                stacks += handler.currentStacks;
            }
        }
        return stacks;
    }

    void OnBuffHandlerRefreshed(BuffManager.BuffHandlerData buffHandlerData)
    {
        ABuffHandlerFactory buffHandlerFactory = buffHandlerData.buffHandlerFactory;
        if (buffHandlerFactory.icon == null)
        {
            return;
        }

        if (!_handlersPerFactory.TryGetValue(buffHandlerFactory, out List<BuffManager.BuffHandlerData> handlers))
        {
            handlers = new List<BuffManager.BuffHandlerData>();
            _handlersPerFactory[buffHandlerFactory] = handlers;
        }
        if (!handlers.Contains(buffHandlerData))
        {
            handlers.Add(buffHandlerData);
        }

        OnStacksChanged.Invoke(buffHandlerFactory, GetStacks(buffHandlerFactory));
    }

    void OnBuffHandlerStopped(BuffManager.BuffHandlerData buffHandlerData)
    {
        ABuffHandlerFactory buffHandlerFactory = buffHandlerData.buffHandlerFactory;
        if (!_handlersPerFactory.TryGetValue(buffHandlerFactory, out List<BuffManager.BuffHandlerData> handlers))
        {
            return;
        }

        handlers.Remove(buffHandlerData);
        if (handlers.Count == 0)
        {
            _handlersPerFactory.Remove(buffHandlerFactory);
        }

        OnStacksChanged.Invoke(buffHandlerFactory, GetStacks(buffHandlerFactory));
    }
}
