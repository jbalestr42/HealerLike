using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Difficulty tab of the Balance Report: the target threat per floor (DifficultyCurve) drawn over the floors of the
// map, with each wave of the pools drawn at its measured threat on the floors of its pools, then the same waves in
// a table with the floor where the curve reaches their threat
public static class DifficultyCurveView
{
    // A wave of the pools with its measured threat
    public class Wave
    {
        public string name;
        public MapNodeType roomType;
        public float threat;
        public bool upToDate;
        public int minFloor;
        public int maxFloor;
    }

    const string FloorZeroThreatKey = "BalanceReport.Difficulty.FloorZeroThreat";
    const string GrowthPerFloorKey = "BalanceReport.Difficulty.GrowthPerFloor";
    const string CombatManaMinKey = "BalanceReport.Difficulty.CombatManaMin";
    const string CombatManaMaxKey = "BalanceReport.Difficulty.CombatManaMax";
    const string EliteManaMinKey = "BalanceReport.Difficulty.EliteManaMin";
    const string EliteManaMaxKey = "BalanceReport.Difficulty.EliteManaMax";
    const string MapSettingsPath = "Assets/Data/Run/MapGenerationSettings.asset";
    const float GraphHeight = 420f;
    const float LeftMargin = 60f;
    const float RightMargin = 120f;
    const float TopMargin = 15f;
    const float BottomMargin = 25f;

    static readonly Color CombatColor = new Color(0.4f, 0.75f, 1f);
    static readonly Color EliteColor = new Color(1f, 0.65f, 0.25f);
    static readonly Color BossColor = new Color(1f, 0.35f, 0.35f);
    static readonly Color GridColor = new Color(1f, 1f, 1f, 0.08f);

    // Elites and bosses target all the mana instead of a part of it: their threat is that much higher
    public static float eliteFactor => BalanceReport.GetManaTarget(MapNodeType.Elite.ToString()) / BalanceReport.GetManaTarget(MapNodeType.Combat.ToString());

    // The mana targets saved in the editor, applied to every report (also loaded with the editor)
    [InitializeOnLoadMethod]
    public static void LoadManaTargets()
    {
        BalanceReport.ManaTargets defaults = BalanceReport.DefaultManaTargets;
        BalanceReport.manaTargets = new BalanceReport.ManaTargets
        {
            combatMin = EditorPrefs.GetFloat(CombatManaMinKey, defaults.combatMin),
            combatMax = EditorPrefs.GetFloat(CombatManaMaxKey, defaults.combatMax),
            eliteMin = EditorPrefs.GetFloat(EliteManaMinKey, defaults.eliteMin),
            eliteMax = EditorPrefs.GetFloat(EliteManaMaxKey, defaults.eliteMax),
        };
    }

    static void SaveManaTargets(BalanceReport.ManaTargets targets)
    {
        EditorPrefs.SetFloat(CombatManaMinKey, targets.combatMin);
        EditorPrefs.SetFloat(CombatManaMaxKey, targets.combatMax);
        EditorPrefs.SetFloat(EliteManaMinKey, targets.eliteMin);
        EditorPrefs.SetFloat(EliteManaMaxKey, targets.eliteMax);
        LoadManaTargets();
    }

    // A min-max slider in percent of the max mana
    static void ManaRangeField(string label, ref float min, ref float max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{label}: {min:P0} to {max:P0}", GUILayout.Width(250f));
        EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, 1.5f, GUILayout.Width(300f));
        EditorGUILayout.EndHorizontal();
        min = Mathf.Round(min * 100f) / 100f;
        max = Mathf.Round(max * 100f) / 100f;
    }

    public static DifficultyCurve LoadCurve()
    {
        return new DifficultyCurve(EditorPrefs.GetFloat(FloorZeroThreatKey, 500f), EditorPrefs.GetFloat(GrowthPerFloorKey, 0.15f));
    }

    // Floors of the map before the boss, the boss being fought right after the last one
    static int LoadFloorCount()
    {
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(MapSettingsPath);
        return settings != null ? settings.floorCount : 10;
    }

    public static void Draw(List<Wave> waves, float width)
    {
        DifficultyCurve curve = LoadCurve();
        int floorCount = LoadFloorCount();

        EditorGUILayout.LabelField($"Target threat per floor: floor 0 threat x (1 + growth)^floor. Combat on the blue curve; elite and boss on the orange one, x{eliteFactor:0.##} (the middle of their mana target, {BalanceReport.GetManaTarget(MapNodeType.Elite.ToString()):P0}, against {BalanceReport.GetManaTarget(MapNodeType.Combat.ToString()):P0} for a combat). Each wave is drawn at its measured threat (Tools > Scores) over the floors of its pools, * when out of date.", EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();
        float floorZeroThreat = EditorGUILayout.FloatField("Floor 0 threat", curve.floorZeroThreat, GUILayout.Width(300f));
        float growth = EditorGUILayout.Slider("Growth per floor (%)", curve.growthPerFloor * 100f, 0f, 50f, GUILayout.Width(400f)) / 100f;
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetFloat(FloorZeroThreatKey, Mathf.Max(1f, floorZeroThreat));
            EditorPrefs.SetFloat(GrowthPerFloorKey, Mathf.Max(0f, growth));
            curve = LoadCurve();
        }

        EditorGUILayout.LabelField("Mana a fight should take, part of the max mana (40% of it comes back before each fight): out of it, the cells of the Waves tab are blue (too easy) or orange (too hard)", EditorStyles.wordWrappedMiniLabel);
        BalanceReport.ManaTargets targets = BalanceReport.manaTargets;
        EditorGUI.BeginChangeCheck();
        ManaRangeField("Combat", ref targets.combatMin, ref targets.combatMax);
        ManaRangeField("Elite and boss", ref targets.eliteMin, ref targets.eliteMax);
        if (EditorGUI.EndChangeCheck())
        {
            SaveManaTargets(targets);
        }

        EditorGUILayout.LabelField($"Floor {floorCount - 1}: {curve.GetThreat(floorCount - 1):0} (elite {curve.GetThreat(floorCount - 1) * eliteFactor:0}), boss: {curve.GetThreat(floorCount) * eliteFactor:0}", EditorStyles.miniLabel);

        Rect rect = GUILayoutUtility.GetRect(width, GraphHeight, GUILayout.Width(width));
        if (Event.current.type == EventType.Repaint)
        {
            DrawGraph(rect, curve, floorCount, waves);
        }
        GUILayout.Space(10f);
        DrawTable(curve, waves);
    }

    static void DrawGraph(Rect rect, DifficultyCurve curve, int floorCount, List<Wave> waves)
    {
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));
        Rect plot = new Rect(rect.x + LeftMargin, rect.y + TopMargin, rect.width - LeftMargin - RightMargin, rect.height - TopMargin - BottomMargin);
        float maxThreat = Mathf.Max(curve.GetThreat(floorCount) * eliteFactor, waves.Count > 0 ? waves.Max(wave => wave.threat) : 0f) * 1.1f;

        Vector2 Point(float floor, float threat)
        {
            return new Vector2(plot.x + floor / floorCount * plot.width, plot.yMax - threat / maxThreat * plot.height);
        }

        // Threat grid and floors
        float step = GetNiceStep(maxThreat / 6f);
        for (float threat = 0f; threat <= maxThreat; threat += step)
        {
            float y = Point(0f, threat).y;
            EditorGUI.DrawRect(new Rect(plot.x, y, plot.width, 1f), GridColor);
            GUI.Label(new Rect(rect.x, y - 8f, LeftMargin - 5f, 16f), $"{threat:0}", EditorStyles.centeredGreyMiniLabel);
        }
        for (int floor = 0; floor <= floorCount; floor++)
        {
            float x = Point(floor, 0f).x;
            EditorGUI.DrawRect(new Rect(x, plot.y, 1f, plot.height), GridColor);
            GUI.Label(new Rect(x - 25f, plot.yMax + 4f, 50f, 16f), floor == floorCount ? "Boss" : $"Floor {floor}", EditorStyles.centeredGreyMiniLabel);
        }

        DrawCurve(curve, 1f, CombatColor, floorCount, Point);
        DrawCurve(curve, eliteFactor, EliteColor, floorCount, Point);

        foreach (Wave wave in waves)
        {
            Color color = GetColor(wave.roomType);
            // The boss is fought after the last floor, whatever the floors of its pool
            int from = wave.roomType == MapNodeType.Boss ? floorCount : Mathf.Clamp(wave.minFloor, 0, floorCount);
            int to = wave.roomType == MapNodeType.Boss ? floorCount : Mathf.Clamp(wave.maxFloor, 0, floorCount - 1);
            Vector2 start = Point(from, wave.threat);
            Vector2 end = Point(Mathf.Max(from, to), wave.threat);
            Handles.color = color;
            Handles.DrawAAPolyLine(5f, start, end);
            EditorGUI.DrawRect(new Rect(start.x - 3f, start.y - 3f, 6f, 6f), color);
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
            GUI.Label(new Rect(end.x + 4f, end.y - 8f, 200f, 16f), $"{wave.name.Replace("Wave_", "")}{(wave.upToDate ? "" : "*")}", style);
        }
    }

    static void DrawCurve(DifficultyCurve curve, float factor, Color color, int floorCount, System.Func<float, float, Vector2> point)
    {
        List<Vector3> points = new List<Vector3>();
        for (float floor = 0f; floor <= floorCount + 0.001f; floor += 0.1f)
        {
            points.Add(point(floor, curve.GetThreat(floor) * factor));
        }
        Handles.color = color;
        Handles.DrawAAPolyLine(3f, points.ToArray());
    }

    static void DrawTable(DifficultyCurve curve, List<Wave> waves)
    {
        Row(EditorStyles.boldLabel, "Wave", "Room", "Threat", "Pools", "Target on pools", "Curve floor");
        foreach (Wave wave in waves.OrderBy(wave => wave.threat))
        {
            float factor = wave.roomType == MapNodeType.Combat ? 1f : eliteFactor;
            string pools = wave.minFloor == wave.maxFloor ? $"{wave.minFloor}" : $"{wave.minFloor}-{wave.maxFloor}";
            string target = wave.roomType == MapNodeType.Boss
                ? $"{curve.GetThreat(LoadFloorCount()) * factor:0}"
                : $"{curve.GetThreat(wave.minFloor) * factor:0}-{curve.GetThreat(wave.maxFloor) * factor:0}";
            float floor = curve.GetFloor(wave.threat / factor);
            Row(EditorStyles.label, wave.name, wave.roomType.ToString(), $"{wave.threat:0}{(wave.upToDate ? "" : "*")}", pools, target, floor < 0f ? "below 0" : $"{floor:0.0}");
        }
    }

    static void Row(GUIStyle style, params string[] columns)
    {
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < columns.Length; i++)
        {
            GUILayout.Label(columns[i], style, GUILayout.Width(i == 0 ? 170f : 100f));
        }
        EditorGUILayout.EndHorizontal();
    }

    static Color GetColor(MapNodeType roomType)
    {
        return roomType == MapNodeType.Boss ? BossColor : roomType == MapNodeType.Elite ? EliteColor : CombatColor;
    }

    // 1, 2 or 5 times a power of 10, the smallest above the value
    static float GetNiceStep(float value)
    {
        float power = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(value, 1f))));
        foreach (float multiple in new[] { 1f, 2f, 5f, 10f })
        {
            if (power * multiple >= value)
            {
                return power * multiple;
            }
        }
        return power * 10f;
    }
}
