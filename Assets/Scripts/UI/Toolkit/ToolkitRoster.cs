using System.Collections.Generic;

// A slot is an owned inventory choice, not an EntityData. Pair disappearing choices with
// newly deployed entities one-to-one, retaining order even when multiple choices share data.
public sealed class ToolkitRoster
{
    public sealed class Entry
    {
        public string key;
        public SelectEntityButton choice;
        public EntityData data;
        public Entity entity;
    }
    readonly List<Entry> _entries = new List<Entry>();
    int _next;
    public IReadOnlyList<Entry> entries => _entries;
    public void Clear() { _entries.Clear(); }
    public void Sync(IReadOnlyList<SelectEntityButton> choices, IReadOnlyList<Entity> entities)
    {
        // Gameplay consumes the first matching data choice, even when a later identical card was dragged.
        // Transfer the surviving choice into that vacated slot before clearing the deployed slot.
        foreach (Entry deployed in _entries)
        {
            if (deployed.entity == null || deployed.choice == null)
            {
                continue;
            }

            if (Contains(choices, deployed.choice))
            {
                Entry vacant = _entries.Find(e => e != deployed && e.entity == null
                    && e.data == deployed.data && !Contains(choices, e.choice));
                if (vacant != null)
                {
                    vacant.choice = deployed.choice;
                }
            }
            deployed.choice = null;
        }
        foreach (SelectEntityButton choice in choices)
        {
            if (choice == null || choice.data == null || _entries.Exists(e => e.choice == choice))
            {
                continue;
            }

            _entries.Add(new Entry { key = "roster-" + _next++, choice = choice, data = choice.data });
        }
        foreach (Entity entity in entities)
        {
            if (entity == null || _entries.Exists(e => e.entity == entity))
            {
                continue;
            }

            Entry entry = _entries.Find(e => e.entity == null && e.data == entity.data && !Contains(choices, e.choice));
            if (entry == null)
            {
                entry = new Entry { key = "roster-" + _next++, data = entity.data };
                _entries.Add(entry);
            }
            entry.entity = entity;
            entry.choice = null;
        }
        _entries.RemoveAll(e => e.entity == null && !Contains(choices, e.choice));
    }
    static bool Contains(IReadOnlyList<SelectEntityButton> choices, SelectEntityButton choice)
    {
        if (choice == null)
        {
            return false;
        }

        foreach (var current in choices)
        {
            if (current == choice)
            {
                return true;
            }
        }

        return false;
    }
}
