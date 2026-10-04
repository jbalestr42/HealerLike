using System.Collections.Generic;
using UnityEngine;

// A simulation scenario: which characters fight which waves on which floors, and how the fights are played
[CreateAssetMenu(menuName = "Custom/Data/Balance/Simulation Plan")]
public class SimulationPlan : ScriptableObject
{
    [Header("Fights")]
    // Each one plays its character in every fight of the plan (a bot without rule casts nothing)
    public List<HealerBotProfile> healerBots = new List<HealerBotProfile>();
    // Every wave of the wave pools when empty
    public List<WavePatternData> waves = new List<WavePatternData>();
    public List<int> floors = new List<int> { 0, 3, 6, 9 };
    [Min(1)] public int seedCount = 1;
    // Map the reference teams are built on (rewards along a path)
    public MapGenerationSettings mapSettings;

    [Header("Fixed team")]
    // When set, the bots, the floors and the reference teams aren't used: every wave fights the units of this
    // wave placed like a team, next to fixedTeamCharacter casting nothing (e.g. punching bags to measure the
    // damage of the waves)
    public WavePatternData fixedTeam;
    // Without item nor skill, so it changes nothing in the fight
    public CharacterData fixedTeamCharacter;
    // Its units come back to full health instead of dying, so the fight lasts the whole max duration (the
    // damage of the waves) or until the wave dies (their robustness)
    public bool fixedTeamNeverDies = true;

    [Header("Fight")]
    [Min(0.1f)] public float timeScale = 10f;
    // Game time after which a fight is stopped and counted as lost
    [Min(1f)] public float maxDuration = 180f;
    // The same for an elite or a boss
    [Min(1f)] public float eliteMaxDuration = 180f;

    [Header("Formation")]
    // Grid column of the units closest to the enemies (the tanks), the next columns going left
    public int frontColumn = 6;
    [Min(1)] public int rowsPerColumn = 5;

    // The waves of the plan, each in the room type of its wave pool
    public List<SimulatedWave> GetWaves(GameData data)
    {
        List<SimulatedWave> all = SimulationJobBuilder.GetWaves(data);
        return waves.Count > 0 ? all.FindAll(simulated => waves.Contains(simulated.wave)) : all;
    }

    public float GetMaxDuration(MapNodeType roomType)
    {
        return roomType == MapNodeType.Elite || roomType == MapNodeType.Boss ? eliteMaxDuration : maxDuration;
    }

    public List<SimulationJob> BuildJobs(GameData data)
    {
        if (fixedTeam != null)
        {
            return SimulationJobBuilder.BuildForFixedTeam(fixedTeamCharacter, ReferenceTeam.FromWave(fixedTeam), GetWaves(data), seedCount);
        }

        return SimulationJobBuilder.Build(healerBots, GetWaves(data), floors, seedCount,
            (character, seed) => ReferenceTeamGenerator.GenerateRun(mapSettings, character.entities, RewardPools.Create(data, character), seed));
    }
}
