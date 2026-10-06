// One simulated fight: a character played by a healer bot, with its reference team, against a wave, as if met
// on a floor. Without bot, the character casts nothing (a fixed team measuring the waves)
public class SimulationJob
{
    public HealerBotProfile bot;
    public CharacterData character;
    public ReferenceTeam team;
    // When set, the team is placed as drawn in this pattern instead of in formation (e.g. the dummies)
    public WavePatternData teamPattern;
    public WavePatternData wave;
    public MapNodeType roomType;
    public int floor;
    public int seed;
}
