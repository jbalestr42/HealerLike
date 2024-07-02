using Unity.VisualScripting;
using UnityEngine;

public class SelectEntityButton : MonoBehaviour
{
    public EntityData data { get; set; }

    public void SelectEntity()
    {
        InteractionManager.instance.SetInteraction(new EntityGridInteraction(data));
    }

    public void OnPointerEnter()
    {
        ShowToolTip(true);
    }

    public void OnPointerExit()
    {
        ShowToolTip(false);
    }

    void ShowToolTip(bool show)
    {
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.Show(show);
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.SetText(data.description);
    }
}
