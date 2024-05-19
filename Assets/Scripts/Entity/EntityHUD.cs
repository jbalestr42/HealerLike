using UnityEngine;

public class EntityHUD : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] ResourceView _resourceView;
    [SerializeField] GameObject _mark;

    public void Init(Entity entity)
    {
        entity.health.OnValueChanged.AddListener((ResourceAttribute health) => _resourceView.SetResource(health.Value, health.Max));

        _resourceView.Show(true);
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }
}
