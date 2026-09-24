using System.Collections.Generic;
using UnityEngine;

// Displays an icon (with its stack count) for each active effect of a BuffManager
public class BuffIconBar : MonoBehaviour
{
    [SerializeField] BuffIconView _iconPrefab;

    BuffIconTracker _tracker;
    Dictionary<ABuffHandlerFactory, BuffIconView> _icons = new Dictionary<ABuffHandlerFactory, BuffIconView>();

    public void Init(BuffManager buffManager)
    {
        _tracker = new BuffIconTracker(buffManager);
        _tracker.OnStacksChanged.AddListener(OnStacksChanged);
    }

    void OnStacksChanged(ABuffHandlerFactory buffHandlerFactory, int stacks)
    {
        _icons.TryGetValue(buffHandlerFactory, out BuffIconView icon);

        if (stacks <= 0)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
                _icons.Remove(buffHandlerFactory);
            }
            return;
        }

        if (icon == null)
        {
            icon = Instantiate(_iconPrefab, transform);
            icon.Init(buffHandlerFactory.icon);
            _icons[buffHandlerFactory] = icon;
        }
        icon.SetStacks(stacks);
    }
}
