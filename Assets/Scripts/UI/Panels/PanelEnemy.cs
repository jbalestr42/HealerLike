using UnityEngine;
using UnityEngine.UI;

public class PanelEnemy : APanel
{
    [SerializeField] Text _speedText;
    [SerializeField] Text _healthText;
    [SerializeField] Text _flatArmorText;
    [SerializeField] Text _percentArmorText;
    [SerializeField] Text _vulnerabilityText;

    public override void UpdateUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            Enemy enemy = selectedObject.GetComponent<Enemy>();
            AttributeManager attributeManager = enemy.GetComponent<AttributeManager>();
            _healthText.text = $"Health: {enemy.health.Value + " / " + attributeManager.Get(AttributeType.HealthMax).Value.ToString("F0")}";
            _speedText.text = $"Speed: {attributeManager.Get(AttributeType.Speed).Value.ToString("F2")}";
            _flatArmorText.text = $"Flat Armor: {attributeManager.Get(AttributeType.FlatArmor).Value.ToString("F0")}";
            _percentArmorText.text = $"Percent Armor: {attributeManager.Get(AttributeType.PercentArmor).Value.ToString("F2")}";
            _vulnerabilityText.text = $"Vulnerability: {attributeManager.Get(AttributeType.Vulnerability).Value.ToString("F2")}";
        }
    }
}
