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
    // Parent of the roles below, only used to balance the game (e.g. to give the items to the right unit)
    public const string Balance = "Balance";
    // Units that hold the front, and the items making a unit last longer (health, armor, regeneration)
    public const string Tank = "Tank";
    // Units dealing the damage, and the items making a unit hit harder or more often
    public const string Damage = "Damage";
    // Units and items healing or strengthening the allies
    public const string Support = "Support";
    // Units and items only made for the balance simulations (dummies, the team measuring the waves), never met
    // in a run
    public const string Simulation = "Simulation";
}
