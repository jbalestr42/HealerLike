// Names of the gameplay tags the code relies on, each one a GameplayTag asset registered in the game data
public static class TagNames
{
    // Items equipped by the character
    public const string Player = "Player";
    // Items equipped by the units
    public const string Entity = "Entity";
    // Buffs kept when the battle ends
    public const string Permanent = "Permanent";
    // Units summoned during a battle, removed at its end
    public const string Summon = "Summon";
    // Units the enemies attack first
    public const string Taunt = "Taunt";
    // Items with a strong bonus and its curse, only from events, never regular rewards
    public const string Cursed = "Cursed";
    // Items offered by the Library events, never regular rewards
    public const string Library = "Library";
    // Units a class can recruit, along with the tag of the class (e.g. Druid)
    public const string Reward = "Reward";
}
