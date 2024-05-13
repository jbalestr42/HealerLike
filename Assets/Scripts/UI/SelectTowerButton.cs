using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectTowerButton : MonoBehaviour
{
    public TowerData data { get; set; }
    public TowerRange range { get; set; }

    public void SelectTower()
    {
        if (PlayerBehaviour.instance.HasEnoughGold(data.price))
        {
            InteractionManager.instance.SetInteraction(new GridInteraction(data, range));
        }
    }
}
