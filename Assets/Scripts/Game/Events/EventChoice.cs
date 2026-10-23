using System;

// One choice of an event or rest room, e.g. "Rest: heal every unit"
public class EventChoice
{
    public string label;
    public string description;

    // Shown but can't be picked, e.g. resurrect when no unit is dead
    public bool isAvailable = true;

    // The unit or item the choice gives (e.g. to show its icon), null for the others (Heal, Leave...)
    public object source;

    public Action onSelected;
}
