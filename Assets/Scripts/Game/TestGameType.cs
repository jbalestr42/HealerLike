using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameType : AGameType
{
    [SerializeField] int _debugWave = 0;

    public override void StartGame()
    {
        PlayerInventory playerInventory = UIManager.instance.GetView<GameView>(ViewType.Game).playerInventory;
        foreach (AItemFactory itemFactory in DataManager.instance.items)
        {
            playerInventory.AddItem(itemFactory.GetItem());
            playerInventory.AddItem(itemFactory.GetItem());
        }
    }

    public override bool IsOver()
    {
        return false;
    }

    public override int currentWave => _debugWave;
}
