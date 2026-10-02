using System.Collections.Generic;

// The run playing an event room
public interface IEventRoomHost
{
    // Shows the choices of the event, the screen closes before the picked choice applies
    void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices);

    // The event is over, the player goes back to the map
    void EndEvent();
}
