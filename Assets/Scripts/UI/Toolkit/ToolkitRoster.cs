using System.Collections.Generic;

// The cards of the party: one per unit of the roster and one per ally on the board. A card keeps its key when its
// unit is placed, so the list doesn't jump, even when several copies of a unit share the same data.
public sealed class ToolkitRoster
{
    public sealed class Entry
    {
        public string key;
        public EntityData data;
        // Null while the unit waits in the roster
        public Entity entity;
        public bool isDeployed;
    }
    readonly List<Entry> _entries = new List<Entry>();
    int _next;
    public IReadOnlyList<Entry> entries => _entries;
    public void Clear() { _entries.Clear(); }

    // units: the roster, a copy per unit; entities: the allies on the board
    public void Sync(IReadOnlyList<EntityData> units, IReadOnlyList<Entity> entities)
    {
        // An ally taken off the board (destroyed, e.g. back to the roster where it gets a card of its own) loses its card
        _entries.RemoveAll(e => e.isDeployed && e.entity == null);
        foreach (Entity entity in entities)
        {
            if (entity == null || _entries.Exists(e => e.entity == entity))
            {
                continue;
            }

            // The card of the unit placed, the first copy as the roster gives the first one
            Entry entry = _entries.Find(e => !e.isDeployed && e.data == entity.data);
            if (entry == null)
            {
                entry = new Entry { key = "roster-" + _next++, data = entity.data };
                _entries.Add(entry);
            }
            Deploy(entry, entity);
        }

        // As many cards waiting as copies of each unit in the roster
        Dictionary<EntityData, int> waiting = new Dictionary<EntityData, int>();
        foreach (EntityData unit in units)
        {
            if (unit != null)
            {
                waiting.TryGetValue(unit, out int count);
                waiting[unit] = count + 1;
            }
        }
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            if (entry.isDeployed)
            {
                continue;
            }

            if (waiting.TryGetValue(entry.data, out int left) && left > CountWaitingBefore(i, entry.data))
            {
                continue;
            }

            _entries.RemoveAt(i);
        }
        foreach (KeyValuePair<EntityData, int> unit in waiting)
        {
            int missing = unit.Value - CountWaitingBefore(_entries.Count, unit.Key);
            for (int i = 0; i < missing; i++)
            {
                _entries.Add(new Entry { key = "roster-" + _next++, data = unit.Key });
            }
        }
    }

    public static void Deploy(Entry entry, Entity entity)
    {
        entry.entity = entity;
        entry.isDeployed = entity != null;
    }

    // The cards of the unit waiting in the roster before index
    int CountWaitingBefore(int index, EntityData data)
    {
        int count = 0;
        for (int i = 0; i < index; i++)
        {
            if (!_entries[i].isDeployed && _entries[i].data == data)
            {
                count++;
            }
        }
        return count;
    }
}
