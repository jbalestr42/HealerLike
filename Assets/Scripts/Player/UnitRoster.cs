using System.Collections.Generic;
using UnityEngine.Events;

// The units the player has and can place on the board, the same unit listed once per copy
public class UnitRoster
{
    public UnityEvent OnChanged = new UnityEvent();

    List<EntityData> _units = new List<EntityData>();
    public IReadOnlyList<EntityData> units => _units;

    public void Init(IEnumerable<EntityData> units)
    {
        _units.Clear();
        _units.AddRange(units);
        OnChanged.Invoke();
    }

    public void Add(EntityData unit)
    {
        _units.Add(unit);
        OnChanged.Invoke();
    }

    // One copy of the unit, e.g. placed on the board. False when the roster has none
    public bool Remove(EntityData unit)
    {
        if (!_units.Remove(unit))
        {
            return false;
        }

        OnChanged.Invoke();
        return true;
    }
}
