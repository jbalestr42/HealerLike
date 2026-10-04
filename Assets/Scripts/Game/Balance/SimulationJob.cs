// One simulated fight: a character played by a healer bot, with its reference team, against a wave, as if met
// on a floor
public class SimulationJob
{
    public HealerBotProfile bot;
    public CharacterData character => bot != null ? bot.character : null;
    public ReferenceTeam team;
    public WavePatternData wave;
    public MapNodeType roomType;
    public int floor;
    public int seed;
}
