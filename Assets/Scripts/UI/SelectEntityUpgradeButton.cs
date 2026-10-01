using UnityEngine;

// A unit offered by a reward: once picked, it joins the units the player can place on the grid
public class SelectEntityUpgradeButton : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text _title;
    [SerializeField] TMPro.TMP_Text _description;
    EntityData _entity;

    public EntityData entity => _entity;

    public void Init(EntityData entity)
    {
        _entity = entity;
        _title.text = GetTitle(entity);
        _description.text = CharacterCardText.GetUnitDetails(entity);
    }

    // "New unit: Zealot", so a unit can't be mistaken for an item
    public static string GetTitle(EntityData entity)
    {
        return $"New unit: {entity.title}";
    }

    public void SelectUpgrade()
    {
        UpgradeView upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        upgradeView.OnEntitySelected.Invoke(_entity);
    }
}
