using System.Collections.Generic;
using UnityEngine;

// The fights left to simulate. Static so it outlives the scene reload between two fights (play mode only)
public static class SimulationQueue
{
    static List<SimulationJob> _jobs = new List<SimulationJob>();
    static int _next;

    // The plan the fights were built from, kept for the scenes reloaded between two fights
    public static SimulationPlan plan { get; private set; }
    public static string outputPath { get; private set; }
    public static string runId { get; private set; }
    public static bool isRunning => _next < _jobs.Count;
    public static int count => _jobs.Count;
    // Index of the current fight
    public static int index => _next;
    public static SimulationJob current => isRunning ? _jobs[_next] : null;

    public static void Start(SimulationPlan simulationPlan, List<SimulationJob> jobs, string path, string id)
    {
        plan = simulationPlan;
        _jobs = new List<SimulationJob>(jobs);
        _next = 0;
        outputPath = path;
        runId = id;
    }

    public static void Advance()
    {
        if (isRunning)
        {
            _next++;
        }
    }

    // Each play session starts empty, even when the domain isn't reloaded on entering play mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        plan = null;
        _jobs.Clear();
        _next = 0;
    }
}
