using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectEntityButton : MonoBehaviour
{
    public EntityData data { get; set; }

    public void SelectEntity()
    {
        if (PlayerBehaviour.instance.HasEnoughGold(data.price))
        {
            InteractionManager.instance.SetInteraction(new EntityGridInteraction(data));
        }
    }
}
