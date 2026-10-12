using UnityEngine;

// How the items with a tag stand out in the inventories: an outline on their icon and a label under their name
// in the tooltip. Listed in the ItemStyleSet of the GameData, so a new kind of item needs no code
[CreateAssetMenu(menuName = "Custom/Data/Item Style")]
public class ItemStyle : ScriptableObject
{
    // The items with this tag, or with a child of it, get this style
    public GameplayTag tag;
    public Color outlineColor = Color.white;
    // Under the name in the tooltip, in the outline color; none when empty
    public string label;
    // The highest one wins when an item has several styled tags (e.g. a cursed ascension malus)
    public int priority;
}
