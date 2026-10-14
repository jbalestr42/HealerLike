using UnityEngine.Events;

// A buff counting toward an effect (e.g. the Rosary, the spells before the free one): its count is shown on the icon
// of the item giving it
public interface ICounterBuff
{
    int counter { get; }
    // The count at which the effect happens (e.g. 5: the fifth spell)
    int counterMax { get; }
    UnityEvent OnCounterChanged { get; }
}
