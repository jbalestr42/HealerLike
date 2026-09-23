using UnityEngine;

public class EntityHUD : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] ResourceView _resourceView;
    [SerializeField] GameObject _mark;
    [SerializeField] BuffIconBar _buffIconBar;

    public void Init(Entity entity)
    {
        entity.health.OnValueChanged.AddListener((ResourceAttribute health) => _resourceView.SetResource(health.Value, health.Max));
        entity.OnMarkChanged.AddListener(ShowMark);
        _buffIconBar.Init(entity.buffManager);

        _resourceView.Show(true);
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }
}
