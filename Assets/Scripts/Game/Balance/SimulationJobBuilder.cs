using System;
using System.Collections.Generic;

public static class SimulationJobBuilder
{
    // Every wave on every floor (only the floors of its pools, give or take poolFloorMargin, when it isn't
    // negative), for every bot (its character played its way) and seed. generateRun gives the reference teams of a
    // run (one per room of its path) for a character and a seed. A bot without character is skipped
    public static List<SimulationJob> Build(IReadOnlyList<HealerBotProfile> bots, IReadOnlyList<SimulatedWave> waves, IReadOnlyList<int> floors, int seedCount, Func<CharacterData, int, List<ReferenceTeam>> generateRun, int poolFloorMargin = -1)
    {
        List<SimulationJob> jobs = new List<SimulationJob>();
        foreach (HealerBotProfile bot in bots)
        {
            if (bot == null || bot.character == null)
            {
                continue;
            }

            for (int seed = 1; seed <= seedCount; seed++)
            {
                List<ReferenceTeam> teams = generateRun(bot.character, seed);
                foreach (int floor in floors)
                {
                    ReferenceTeam team = GetTeamOnFloor(teams, floor);
                    if (team == null)
                    {
                        continue;
                    }

                    foreach (SimulatedWave wave in waves)
                    {
                        if (IsPlayedOnFloor(wave, floor, poolFloorMargin))
                        {
                            jobs.Add(new SimulationJob { bot = bot, character = bot.character, team = team, wave = wave.wave, roomType = wave.roomType, floor = floor, seed = seed });
                        }
                    }
                }
            }
        }
        return jobs;
    }

    // Every wave against the starting units of the character of every bot (its state when a run starts: no reward,
    // its own items and skills), for every seed. Without castSpells the character casts nothing. A bot without
    // character is skipped
    public static List<SimulationJob> BuildForInitialTeams(IReadOnlyList<HealerBotProfile> bots, IReadOnlyList<SimulatedWave> waves, int seedCount, bool castSpells)
    {
        List<SimulationJob> jobs = new List<SimulationJob>();
        foreach (HealerBotProfile bot in bots)
        {
            if (bot == null || bot.character == null)
            {
                continue;
            }

            ReferenceTeam team = new ReferenceTeam { units = bot.character.entities.FindAll(unit => unit != null) };
            for (int seed = 1; seed <= seedCount; seed++)
            {
                foreach (SimulatedWave wave in waves)
                {
                    jobs.Add(new SimulationJob { bot = castSpells ? bot : null, character = bot.character, team = team, wave = wave.wave, roomType = wave.roomType, floor = 0, seed = seed });
                }
            }
        }
        return jobs;
    }

    // Every wave against the same team, for every seed, next to a character without bot
    public static List<SimulationJob> BuildForFixedTeam(CharacterData character, ReferenceTeam team, IReadOnlyList<SimulatedWave> waves, int seedCount, WavePatternData teamPattern = null)
    {
        List<SimulationJob> jobs = new List<SimulationJob>();
        if (character == null || team == null)
        {
            return jobs;
        }

        for (int seed = 1; seed <= seedCount; seed++)
        {
            foreach (SimulatedWave wave in waves)
            {
                jobs.Add(new SimulationJob { character = character, team = team, teamPattern = teamPattern, wave = wave.wave, roomType = wave.roomType, floor = team.floor, seed = seed });
            }
        }
        return jobs;
    }

    // Every wave against the same team, once without item then once with each item, to measure the items one by
    // one. Each unit item is held by every unit of the team in turn, a fight each, and each item of the character
    // is played as many times. The fights of a turn share their seed with the fight without item of that turn, so
    // an item changing nothing plays the same fight as it. Without withoutItemFights, the fights without item aren't
    // played (e.g. taken from a previous measure)
    public static List<SimulationJob> BuildForEachItem(CharacterData character, ReferenceTeam team, IReadOnlyList<SimulatedWave> waves, int seedCount, IReadOnlyList<AItemFactory> unitItems, IReadOnlyList<AItemFactory> playerItems, WavePatternData teamPattern = null, bool withoutItemFights = true)
    {
        List<SimulationJob> jobs = new List<SimulationJob>();
        if (character == null || team == null)
        {
            return jobs;
        }

        int turns = System.Math.Max(1, team.units.Count);
        for (int seedIndex = 0; seedIndex < seedCount; seedIndex++)
        {
            for (int turn = 0; turn < turns; turn++)
            {
                int seed = seedIndex * turns + turn + 1;
                foreach (SimulatedWave wave in waves)
                {
                    if (withoutItemFights)
                    {
                        jobs.Add(new SimulationJob { character = character, team = team, teamPattern = teamPattern, wave = wave.wave, roomType = wave.roomType, floor = team.floor, seed = seed });
                    }
                    if (turn < team.units.Count)
                    {
                        foreach (AItemFactory item in unitItems)
                        {
                            ReferenceTeam holding = team.Copy(team.floor);
                            holding.unitItems.Add(new ReferenceTeam.UnitItem { unit = turn, item = item });
                            jobs.Add(new SimulationJob { character = character, team = holding, teamPattern = teamPattern, wave = wave.wave, roomType = wave.roomType, floor = team.floor, seed = seed, item = item, itemHolder = turn });
                        }
                    }
                    foreach (AItemFactory item in playerItems)
                    {
                        ReferenceTeam holding = team.Copy(team.floor);
                        holding.playerItems.Add(item);
                        jobs.Add(new SimulationJob { character = character, team = holding, teamPattern = teamPattern, wave = wave.wave, roomType = wave.roomType, floor = team.floor, seed = seed, item = item });
                    }
                }
            }
        }
        return jobs;
    }

    // Whether the wave is played on the floor: every floor with a negative margin or for a wave in no pool, else
    // the floors of its pools give or take the margin
    public static bool IsPlayedOnFloor(SimulatedWave wave, int floor, int poolFloorMargin)
    {
        if (poolFloorMargin < 0 || !wave.hasPoolFloors)
        {
            return true;
        }
        return floor >= wave.minFloor - poolFloorMargin && floor <= wave.maxFloor + poolFloorMargin;
    }

    // How many fights Build makes, without generating the teams: every floor is supposed to have one
    public static int CountJobs(IReadOnlyList<HealerBotProfile> bots, IReadOnlyList<SimulatedWave> waves, IReadOnlyList<int> floors, int seedCount, int poolFloorMargin = -1)
    {
        int botCount = 0;
        foreach (HealerBotProfile bot in bots)
        {
            if (bot != null && bot.character != null)
            {
                botCount++;
            }
        }

        int fightsPerRun = 0;
        foreach (int floor in floors)
        {
            foreach (SimulatedWave wave in waves)
            {
                if (IsPlayedOnFloor(wave, floor, poolFloorMargin))
                {
                    fightsPerRun++;
                }
            }
        }
        return botCount * seedCount * fightsPerRun;
    }

    // Every wave of the wave pools once, played in the room type of its first pool, over the floors of all its pools
    public static List<SimulatedWave> GetWaves(GameData data)
    {
        List<SimulatedWave> waves = new List<SimulatedWave>();
        foreach (GameData.WavePool pool in data.wavePools)
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                if (wave == null)
                {
                    continue;
                }

                int index = waves.FindIndex(simulated => simulated.wave == wave);
                if (index < 0)
                {
                    waves.Add(new SimulatedWave { wave = wave, roomType = pool.roomType, hasPoolFloors = true, minFloor = pool.minFloor, maxFloor = pool.maxFloor });
                }
                else
                {
                    SimulatedWave simulated = waves[index];
                    simulated.minFloor = System.Math.Min(simulated.minFloor, pool.minFloor);
                    simulated.maxFloor = System.Math.Max(simulated.maxFloor, pool.maxFloor);
                    waves[index] = simulated;
                }
            }
        }
        return waves;
    }

    // The team entering the floor, or the last one before it when the path has no room on that floor
    public static ReferenceTeam GetTeamOnFloor(IReadOnlyList<ReferenceTeam> teams, int floor)
    {
        ReferenceTeam found = null;
        foreach (ReferenceTeam team in teams)
        {
            if (team.floor <= floor)
            {
                found = team;
            }
        }
        return found;
    }
}
