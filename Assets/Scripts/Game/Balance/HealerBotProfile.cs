using System.Collections.Generic;
using UnityEngine;

// How a simulated player plays a character: every decision interval, the first rule whose skill can be used,
// whose conditions are met and which finds a target is cast
[CreateAssetMenu(menuName = "Custom/Data/Balance/Healer Bot Profile")]
public class HealerBotProfile : ScriptableObject
{
    public CharacterData character;
    [Min(0.05f)] public float decisionInterval = 0.25f;
    public List<HealerBotRule> rules = new List<HealerBotRule>();
}
