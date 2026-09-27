using UnityEngine;
using Sirenix.OdinInspector;
using System;

[Serializable]
public struct EntitySlot
{
    public EntityData entity;
}

[CreateAssetMenu(menuName = "Custom/Data/WavePatternData")]
public class WavePatternData : SerializedScriptableObject
{
    [OnValueChanged("CreateData")]
    public int width = 5;

    [OnValueChanged("CreateData")]
    public int height = 5;

    public EntitySlot[,] slots;

    // World position of a slot when the pattern is centered on center
    public Vector3 GetSlotPosition(Vector3 center, int x, int y)
    {
        return center - new Vector3(width / 2f, 0f, height / 2f) + new Vector3(x, 0f, y);
    }

    private void CreateData()
    {
        var tmp = slots;
        slots = new EntitySlot[width, height];
        if (tmp != null)
        {
            for (int i = 0; i < Mathf.Min(tmp.GetLength(0), slots.GetLength(0)); i++)
            {
                for (int j = 0; j < Mathf.Min(tmp.GetLength(1), slots.GetLength(1)); j++)
                {
                    slots[i, j] = tmp[i, j];
                }
            }
        }
    }
}
