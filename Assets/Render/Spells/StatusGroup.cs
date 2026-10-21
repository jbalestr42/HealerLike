using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // The running handlers of one factory on one target, summed into what an observer publishes as one status
    public struct StatusGroup
    {
        public int stacks;
        public float elapsed;
        public float duration;
        public GameObject source;

        // Groups the observed handlers by target and factory: stacks add up, the youngest clock and the longest
        // duration win, the first source stays
        public static void Collect(List<BuffManager.BuffHandlerData> observed,
            Dictionary<HandlerKey, StatusGroup> groups)
        {
            groups.Clear();
            foreach (BuffManager.BuffHandlerData data in observed)
            {
                if (data.target == null || data.buffHandlerFactory == null)
                {
                    continue;
                }

                HandlerKey key = new HandlerKey(data.target, data.buffHandlerFactory);
                StatusGroup group = Read(data);
                if (groups.TryGetValue(key, out StatusGroup previous))
                {
                    group.stacks += previous.stacks;
                    group.elapsed = Mathf.Min(previous.elapsed, group.elapsed);
                    group.duration = Mathf.Max(previous.duration, group.duration);
                    group.source = previous.source;
                }

                groups[key] = group;
            }
        }

        public bool IsSame(StatusGroup other)
        {
            return stacks == other.stacks && elapsed == other.elapsed && duration == other.duration;
        }

        static StatusGroup Read(BuffManager.BuffHandlerData data)
        {
            StatusGroup group = new StatusGroup();
            group.stacks = Mathf.Max(0, data.currentStacks + data.refreshStacks);
            group.source = data.source;
            BuffHandler handler = data.buffHandler as BuffHandler;
            if (handler != null)
            {
                group.elapsed = handler.durationTimer;
            }

            group.duration = data.buffHandlerFactory.duration;
            if (data.buffHandlerFactory.durationType == DurationType.Infinite)
            {
                group.duration = float.PositiveInfinity;
            }

            return group;
        }
    }
}
