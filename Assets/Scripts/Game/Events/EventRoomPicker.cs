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
}
