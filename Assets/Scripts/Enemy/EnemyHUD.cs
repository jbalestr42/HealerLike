using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHUD : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Slider _healthSlider;
    [SerializeField] TMPro.TMP_Text _healthText;
    [SerializeField] GameObject _mark;

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
