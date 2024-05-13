using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHUD : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Slider _healthSlider;
    [SerializeField] TMPro.TMP_Text _healthText;
    [SerializeField] GameObject _mark;

    void Start()
    {
        GetComponent<Entity>().health.OnValueChanged.AddListener((ResourceAttribute health) => SetHealth(health.Value, health.Max));
        GetComponent<Entity>().OnMarkChanged.AddListener(ShowMark);
    }

    public void SetHealth(float current, float max)
    {
        _healthSlider.value = current / max;
        _healthText.text = $"{current.ToString("F0")} / {max.ToString("F0")}";
    }

    public void ShowMark(bool show)
    {
        _mark.SetActive(show);
    }
}
