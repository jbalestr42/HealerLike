using UnityEngine;

public class SelectPlayerItemUpgradeButton : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text _title;
    [SerializeField] TMPro.TMP_Text _description;
    AItem _item;

    public AItem item { get { return _item; } }

    public void Init(AItem item)
    {
        _item = item;
        _title.text = _item.title;
        _description.text = _item.description;
    }

    public void SelectUpgrade()
    {
        UpgradeView upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        upgradeView.OnPlayerItemSelected.Invoke(_item);
    }
}