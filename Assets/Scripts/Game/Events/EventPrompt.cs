using System.Collections.Generic;

// The screen of an event or rest room: a title, a text and its choices, only one of them picked
public class EventPrompt
{
    public string title;
    public string description;

    List<EventChoice> _choices;
    public IReadOnlyList<EventChoice> choices => _choices;

    bool _hasChosen = false;
    public bool hasChosen => _hasChosen;

    public EventPrompt(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        this.title = title;
        this.description = description;
        _choices = new List<EventChoice>(choices);
    }

    // Only the first pick counts: the choice usually closes the screen, a second click would apply it twice. False
    // when the choice wasn't applied
    public bool Select(EventChoice choice)
    {
        if (_hasChosen || choice == null || !choice.isAvailable || !_choices.Contains(choice))
        {
            return false;
        }

        _hasChosen = true;
        choice.onSelected?.Invoke();
        return true;
    }
}
