using UnityEngine;

public class EntityHUD : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] ResourceView _resourceView;
    [SerializeField] GameObject _mark;
    [SerializeField] BuffIconBar _buffIconBar;
    // Optional: the shield shown at the end of the health, inside the fill area of the bar
    [SerializeField] RectTransform _shieldBar;

    Entity _entity;

    void ShowHealth(ResourceAttribute health)
    {
        float shield = _entity != null && _entity.shield != null ? _entity.shield.Value : 0f;
        _resourceView.SetResource(health.Value, health.Max);
        if (shield > 0f)
        {
            _resourceView.SetText($"{health.Value:F0} / {health.Max:F0} +{shield:F0}");
        }

        if (_shieldBar != null)
        {
            (float start, float end) = GetShieldSpan(health.Value, shield, health.Max);
            _shieldBar.gameObject.SetActive(end > start);
            _shieldBar.anchorMin = new Vector2(start, 0f);
            _shieldBar.anchorMax = new Vector2(end, 1f);
            _shieldBar.offsetMin = Vector2.zero;
            _shieldBar.offsetMax = Vector2.zero;
        }
    }

    void ShowShield(ResourceAttribute shield)
    {
        ShowHealth(_entity.health);
    }

    // Part of the bar the shield covers: right after the health, or at the end of the bar over the health when both
    // go above the max
    public static (float start, float end) GetShieldSpan(float health, float shield, float max)
    {
        if (shield <= 0f || max <= 0f)
        {
            return (0f, 0f);
        }
        float end = Mathf.Min(1f, (health + shield) / max);
        return (Mathf.Max(0f, end - shield / max), end);
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }

    #region IVisualBehaviour

    public void Init(Entity entity)
    {
        _entity = entity;
        entity.health.OnValueChanged.AddListener(ShowHealth);
        if (entity.shield != null)
        {
            entity.shield.OnValueChanged.AddListener(ShowShield);
        }
        // The health was set before the HUD listened to it: shown right away, not only after the first hit
        ShowHealth(entity.health);
        entity.OnMarkChanged.AddListener(ShowMark);
        _buffIconBar.Init(entity.buffManager);

        _resourceView.Show(true);
    }

    #endregion
}
