using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectUpgradeButton : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text _title;
    AItem _item;

    public void Init(AItem item)
    {
        _item = item;
        _title.text = _item.title;
    }

    public void SelectUpgrade()
    {
        UpgradeView upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        upgradeView.OnItemSelected.Invoke(_item);
    }
}