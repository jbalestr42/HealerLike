using UnityEngine;

// What happens in an event room of the map (e.g. recruit a unit, train one, open a cursed chest). The run
// draws one of its events when the player enters the room, the event plays and tells the run when it's over
public abstract class AEventRoom : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;

    // Must end with host.EndEvent(), at once or once the player made their choice
    public abstract void Play(IEventRoomHost host);
}
