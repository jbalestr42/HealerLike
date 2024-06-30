using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Custom/Data/WavePatternData")]
public class WavePatternData : SerializedScriptableObject
{
    [OnValueChanged("CreateData")]
    public int width = 5;

    [OnValueChanged("CreateData")]
    public int height = 5;

    [TableMatrix(HorizontalTitle = "Spawn Matrix", SquareCells = true)]
    public EntityData[,] spawns;

    private void CreateData()
    {
        var tmp = spawns;
        spawns = new EntityData[width, height];
        if (tmp != null)
        {
            for (int i = 0; i < Mathf.Min(tmp.GetLength(0), spawns.GetLength(0)); i++)
            {
                for (int j = 0; j < Mathf.Min(tmp.GetLength(1), spawns.GetLength(1)); j++)
                {
                    spawns[i, j] = tmp[i, j];
                }
            }
        }
    }
}
