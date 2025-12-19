using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Gameplay Tag")]
public class GameplayTag : ScriptableObject
{
    [SerializeField]
    GameplayTag _parent = null;
    public GameplayTag parent { get { return _parent; } }

    public bool IsDescendantOf(GameplayTag other, int searchLimit = 4)
    {
        GameplayTag ancestor = parent;
        while (searchLimit-- > 0 && ancestor != null)
        {
            if (ancestor == other)
            {
                return true;
            }

            ancestor = ancestor.parent;
        }

        return false;
    }
}