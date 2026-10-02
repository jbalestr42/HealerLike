using System;

// One choice of an event or rest room, e.g. "Rest: heal every unit"
public class EventChoice
{
    public string label;
    public string description;

    // Shown but can't be picked, e.g. resurrect when no unit is dead
    public bool isAvailable = true;

    public Action onSelected;
}
