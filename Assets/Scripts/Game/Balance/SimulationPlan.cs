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
    // Each wave only on the floors of its pools, give or take poolFloorMargin (far quicker than every floor)
    public bool onlyPoolFloors;
    [Min(0)] public int poolFloorMargin = 1;
    [Min(1)] public int seedCount = 1;
    // Map the reference teams are built on (rewards along a path)
    public MapGenerationSettings mapSettings;

    [Header("Fixed team")]
    // When set, the bots, the floors and the reference teams aren't used: every wave fights the units of this
    // wave placed as drawn in it, next to fixedTeamCharacter casting nothing (e.g. dummies to measure the damage
    // of the waves). Units tagged Simulation never die
    public WavePatternData fixedTeam;
    // Without item nor skill, so it changes nothing in the fight
    public CharacterData fixedTeamCharacter;
    // The units of the fixed team tagged Simulation die like the others (e.g. the dummies, to measure how long
    // they survive). The enemies tagged Simulation still never die
    public bool fixedTeamDies;

    [Header("Items")]
    // With a fixed team: every wave is played once without item, then once with each reward item, each unit item
    // held by every unit of the fixed team in turn (e.g. to score the items)
    public bool eachRewardItem;
    // With eachRewardItem, only these reward items when not empty (e.g. one item measured again), the fights
    // without item still played
    public List<AItemFactory> onlyItems = new List<AItemFactory>();
    // With eachRewardItem, the fights without item aren't played, taken from a previous measure instead
    public bool skipFightsWithoutItem;

    [Header("Initial teams")]
    // When set (and no fixed team), the floors and the reference teams aren't used: every wave fights the starting
    // units of the character of each bot, as a run starts (e.g. to score the characters)
    public bool initialTeams;
    // Whether the bots cast their skills in the fights of the initial teams
    public bool castSpells = true;

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

    // The waves of the plan, each in the room type of its wave pool. A listed wave in no pool (e.g. the dummies
    // measuring a team) is played as a combat
    public List<SimulatedWave> GetWaves(GameData data)
    {
        List<SimulatedWave> all = SimulationJobBuilder.GetWaves(data);
        if (waves.Count == 0)
        {
            return all;
        }

        List<SimulatedWave> listed = new List<SimulatedWave>();
        foreach (WavePatternData wave in waves)
        {
            if (wave != null)
            {
                int pooled = all.FindIndex(simulated => simulated.wave == wave);
                listed.Add(pooled >= 0 ? all[pooled] : new SimulatedWave { wave = wave, roomType = MapNodeType.Combat });
            }
        }
        return listed;
    }

    // The margin given to the job builder, negative to play every floor
    int floorMargin => onlyPoolFloors ? poolFloorMargin : -1;

    // How many fights the plan makes with the bots, the floors and the reference teams (not for a fixed team or
    // the initial teams)
    public int CountJobs(GameData data)
    {
        return SimulationJobBuilder.CountJobs(healerBots, GetWaves(data), floors, seedCount, floorMargin);
    }

    // A copy of the plan, under the same name, measuring only the target: a wave instead of every wave of the pools,
    // the bots of a character, or a reward item. What doesn't play the target's kind is copied whole (e.g. the
    // balance team against the dummies, listing its own wave and no bot). With skipFightsWithoutItem, an item is
    // played without the fights without item (taken from a previous measure)
    public SimulationPlan CopyFor(Object target, bool skipFightsWithoutItem = false)
    {
        SimulationPlan copy = Instantiate(this);
        copy.name = name;
        if (target is WavePatternData wave && waves.Count == 0)
        {
            copy.waves = new List<WavePatternData> { wave };
        }
        else if (target is CharacterData character && healerBots.Count > 0)
        {
            copy.healerBots = healerBots.FindAll(bot => bot != null && bot.character == character);
        }
        else if (target is AItemFactory item && eachRewardItem)
        {
            copy.onlyItems = new List<AItemFactory> { item };
            copy.skipFightsWithoutItem = skipFightsWithoutItem;
        }
        return copy;
    }

    // Whether the reward item is measured by eachRewardItem
    bool IsMeasured(AItemFactory item)
    {
        return onlyItems.Count == 0 || onlyItems.Contains(item);
    }

    public float GetMaxDuration(MapNodeType roomType)
    {
        return roomType == MapNodeType.Elite || roomType == MapNodeType.Boss ? eliteMaxDuration : maxDuration;
    }

    public List<SimulationJob> BuildJobs(GameData data)
    {
        if (fixedTeam != null && eachRewardItem)
        {
            RewardPools rewards = RewardPools.Create(data, null);
            List<AItemFactory> unitItems = rewards.unitItems.FindAll(IsMeasured);
            List<AItemFactory> playerItems = rewards.playerItems.FindAll(IsMeasured);
            return SimulationJobBuilder.BuildForEachItem(fixedTeamCharacter, ReferenceTeam.FromWave(fixedTeam), GetWaves(data), seedCount, unitItems, playerItems, fixedTeam, !skipFightsWithoutItem);
        }
        if (fixedTeam != null)
        {
            return SimulationJobBuilder.BuildForFixedTeam(fixedTeamCharacter, ReferenceTeam.FromWave(fixedTeam), GetWaves(data), seedCount, fixedTeam);
        }
        if (initialTeams)
        {
            return SimulationJobBuilder.BuildForInitialTeams(healerBots, GetWaves(data), seedCount, castSpells);
        }

        return SimulationJobBuilder.Build(healerBots, GetWaves(data), floors, seedCount,
            (character, seed) => ReferenceTeamGenerator.GenerateRun(mapSettings, character.entities, RewardPools.Create(data, character), seed), floorMargin);
    }
}
