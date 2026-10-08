// A wave and the type of room it is fought in (an elite wave is played as an elite fight)
public struct SimulatedWave
{
    public WavePatternData wave;
    public MapNodeType roomType;
    // The floors of the pools it is in (from the lowest min to the highest max), when it is in one
    public bool hasPoolFloors;
    public int minFloor;
    public int maxFloor;
}
