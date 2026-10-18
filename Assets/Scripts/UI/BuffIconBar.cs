using System.Collections.Generic;
using UnityEngine;

// Displays an icon (with its stack count) for each active effect of a BuffManager
public class BuffIconBar : MonoBehaviour
{
    [SerializeField] BuffIconView _iconPrefab;
    // The icon of the hits the unit still blocks (Hit Armor, e.g. the guard of the Armored Footman), with their count
    [SerializeField] Sprite _hitArmorIcon;

    BuffIconTracker _tracker;
    Dictionary<ABuffHandlerFactory, BuffIconView> _icons = new Dictionary<ABuffHandlerFactory, BuffIconView>();
    Attribute _hitArmor;
    BuffIconView _hitArmorView;

    public BuffIconView hitArmorView => _hitArmorView;

    void OnDestroy()
    {
        if (_hitArmor != null)
        {
            _hitArmor.RemoveOnValueChangedListener(OnHitArmorChanged);
        }
    }

    // The time left of each effect shown on its icon
    void Update()
    {
        if (_tracker == null)
        {
            return;
        }
        foreach (KeyValuePair<ABuffHandlerFactory, BuffIconView> icon in _icons)
        {
            if (_tracker.TryGetRemaining(icon.Key, out float remaining, out float duration))
            {
                icon.Value.SetRemaining(remaining, duration);
            }
            else
            {
                icon.Value.SetRemaining(0f, 0f);
            }
        }
    }

    public void Init(BuffManager buffManager)
    {
        _tracker = new BuffIconTracker(buffManager);
        _tracker.OnStacksChanged.AddListener(OnStacksChanged);
    }

    // Shown while the unit blocks hits, its count going down with each hit blocked
    public void TrackHitArmor(Attribute hitArmor)
    {
        _hitArmor = hitArmor;
        _hitArmor.AddOnValueChangedListener(OnHitArmorChanged);
        OnHitArmorChanged(_hitArmor);
    }

    void OnHitArmorChanged(Attribute hitArmor)
    {
        SetHitArmor(Mathf.RoundToInt(hitArmor.Value));
    }

    public void SetHitArmor(int hits)
    {
        if (hits <= 0)
        {
            if (_hitArmorView != null)
            {
                DestroyIcon(_hitArmorView.gameObject);
                _hitArmorView = null;
            }
            return;
        }

        if (_hitArmorView == null)
        {
            _hitArmorView = Instantiate(_iconPrefab, transform);
            _hitArmorView.Init(_hitArmorIcon);
        }
        _hitArmorView.SetStacks(hits, showSingle: true);
    }

    // Also used from the EditMode tests, where Destroy isn't allowed
    static void DestroyIcon(GameObject icon)
    {
        if (Application.isPlaying)
        {
            Destroy(icon);
        }
        else
        {
            DestroyImmediate(icon);
        }
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
