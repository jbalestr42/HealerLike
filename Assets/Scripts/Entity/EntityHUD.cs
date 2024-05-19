using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHUD : MonoBehaviour, IVisualBehaviour
{
    [SerializeField] UnityEngine.UI.Slider _healthSlider;
    [SerializeField] TMPro.TMP_Text _healthText;
    [SerializeField] GameObject _mark;

    public void Init(Entity entity)
    {
        entity.health.OnValueChanged.AddListener((ResourceAttribute health) => SetHealth(health.Value, health.Max));
        entity.OnMarkChanged.AddListener(ShowMark);

        _healthSlider.gameObject.SetActive(true);
    }

    public void SetHealth(float current, float max)
    {
        _healthSlider.value = current / max;
        _healthText.text = $"{current:F0} / {max:F0}";
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }
}
