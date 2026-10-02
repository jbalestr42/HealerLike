using System.Collections.Generic;

// The run playing an event room
public interface IEventRoomHost
{
    // The character played in the run
    CharacterData characterData { get; }

    // Seeded with the run, so a seed always plays the same events
    System.Random random { get; }

    // Shows the choices of the event, the screen closes before the picked choice applies
    void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices);

    // The unit joins the ones the player can place on the grid
    void AddUnit(EntityData unit);

    // The event is over, the player goes back to the map
    void EndEvent();
}
