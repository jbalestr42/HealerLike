using UnityEngine;

public class UseConsumableButton : MonoBehaviour
{
    public AConsumableFactory data { get; set; }

    public void UseConsumable()
    {
        Debug.Log("[DEBUG] UseConsumable");
        data.GetConsumable().Use();
    }
}
