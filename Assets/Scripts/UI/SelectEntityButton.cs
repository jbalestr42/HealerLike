using UnityEngine;

public class SelectEntityButton : MonoBehaviour
{
    public EntityData data { get; set; }

    public void SelectEntity()
    {
        InteractionManager.instance.SetInteraction(new EntityGridInteraction(data));
    }
}
