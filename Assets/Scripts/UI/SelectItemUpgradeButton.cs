using UnityEngine;

public class SelectItemUpgradeButton : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text _title;
    [SerializeField] TMPro.TMP_Text _description;
    AItem _item;

    public void Init(AItem item)
    {
        _item = item;
        _title.text = _item.title;
        SetDescription(_item.description);
    }

    // Its status keywords formatted, and explained while the button is hovered
    void SetDescription(string description)
    {
        _description.text = StatusKeywords.Format(description);
        if (GetComponent<HoverTooltipTrigger>() == null)
        {
            StatusKeywordBox.AddHover(gameObject, () => _description.text);
        }
    }

    public void SelectUpgrade()
    {
        UpgradeView upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        upgradeView.OnItemSelected.Invoke(_item);
    }
}