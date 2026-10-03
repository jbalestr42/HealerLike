using System;
using System.Collections.Generic;

public static class SimulationJobBuilder
{
    // Every wave on every floor, for every character and seed. generateRun gives the reference teams of a run
    // (one per room of its path) for a character and a seed
    public static List<SimulationJob> Build(IReadOnlyList<CharacterData> characters, IReadOnlyList<SimulatedWave> waves, IReadOnlyList<int> floors, int seedCount, Func<CharacterData, int, List<ReferenceTeam>> generateRun)
    {
        List<SimulationJob> jobs = new List<SimulationJob>();
        foreach (CharacterData character in characters)
        {
            for (int seed = 1; seed <= seedCount; seed++)
            {
                List<ReferenceTeam> teams = generateRun(character, seed);
                foreach (int floor in floors)
                {
                    ReferenceTeam team = GetTeamOnFloor(teams, floor);
                    if (team == null)
                    {
                        continue;
                    }

                    foreach (SimulatedWave wave in waves)
                    {
                        jobs.Add(new SimulationJob { character = character, team = team, wave = wave.wave, roomType = wave.roomType, floor = floor, seed = seed });
                    }
                }
            }
        }
        return jobs;
    }

    // Every wave of the wave pools once, played in the room type of its first pool
    public static List<SimulatedWave> GetWaves(GameData data)
    {
        List<SimulatedWave> waves = new List<SimulatedWave>();
        foreach (GameData.WavePool pool in data.wavePools)
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                if (wave != null && !waves.Exists(simulated => simulated.wave == wave))
                {
                    waves.Add(new SimulatedWave { wave = wave, roomType = pool.roomType });
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
