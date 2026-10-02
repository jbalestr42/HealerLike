using System.Collections.Generic;

// Draws the event played in an event room, each event with a chance proportional to its weight
public static class EventRoomPicker
{
    // Null when no event can be drawn (no event, or none with a weight)
    public static AEventRoom Pick(IReadOnlyList<EventRoomChance> events, System.Random random)
    {
        float totalWeight = 0f;
        AEventRoom lastEvent = null;
        if (events != null)
        {
            foreach (EventRoomChance chance in events)
            {
                if (CanBeDrawn(chance))
                {
                    totalWeight += chance.weight;
                    lastEvent = chance.eventRoom;
                }
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        double roll = random.NextDouble() * totalWeight;
        foreach (EventRoomChance chance in events)
        {
            if (!CanBeDrawn(chance))
            {
                continue;
            }

            roll -= chance.weight;
            if (roll < 0d)
            {
                return chance.eventRoom;
            }
        }
        // Float rounding on the very last one
        return lastEvent;
    }

    static bool CanBeDrawn(EventRoomChance chance)
    {
        return chance != null && chance.eventRoom != null && chance.weight > 0f;
    }

    // Different elements, as many as the pool has up to count, in a random order (e.g. the units of a
    // recruitment, the items of a library)
    public static List<T> PickDistinct<T>(IReadOnlyList<T> pool, int count, System.Random random) where T : class
    {
        List<T> candidates = new List<T>();
        if (pool != null)
        {
            foreach (T element in pool)
            {
                if (element != null && !candidates.Contains(element))
                {
                    candidates.Add(element);
                }
            }
        }

        List<T> picked = new List<T>();
        while (picked.Count < count && candidates.Count > 0)
        {
            int index = random.Next(candidates.Count);
            picked.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return picked;
    }
}
