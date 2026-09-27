// Characters chosen outside of the scene that uses them, kept while scenes are loaded
public static class CharacterSelection
{
    // Picked on the menu character select screen, played for the whole run
    public static CharacterData selected { get; set; }

    // Played in the sandbox, null plays with every sandbox skill
    public static CharacterData sandboxCharacter { get; set; }
}
