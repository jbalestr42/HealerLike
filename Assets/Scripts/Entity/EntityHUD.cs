using UnityEngine;

public class EntityHUD : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] ResourceView _resourceView;
    [SerializeField] GameObject _mark;
    [SerializeField] BuffIconBar _buffIconBar;

    public void Init(Entity entity)
    {
        entity.health.OnValueChanged.AddListener(ShowHealth);
        // The health was set before the HUD listened to it: shown right away, not only after the first hit
        ShowHealth(entity.health);
        entity.OnMarkChanged.AddListener(ShowMark);
        _buffIconBar.Init(entity.buffManager);

        _resourceView.Show(true);
    }

    void ShowHealth(ResourceAttribute health)
    {
        _resourceView.SetResource(health.Value, health.Max);
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }
}
