using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // Measures the lab ground against the scenario's current sources without owning rendering resources.
    public class GrassLabMeasurements
    {
        static readonly Rect area = GrassLabScene.Area;
        static readonly Vector2[] probes =
        {
            new Vector2(0f, 0.5f), new Vector2(0f, 1.4f), new Vector2(-1.5f, -0.6f), new Vector2(1f, -1.6f)
        };
        readonly IReadOnlyList<GrassLabScene.CreatureBody> _creatures;
        readonly StringBuilder _csv = new StringBuilder("frame,time");
        float _movingSum;
        float _farSum;
        float _farPeak;
        int _frames;

        public GrassLabMeasurements(IReadOnlyList<GrassLabScene.CreatureBody> creatures)
        {
            _creatures = creatures;
            for (int i = 0; i < probes.Length; i++)
            {
                _csv.Append($",lean{i}x,lean{i}z,crush{i}");
            }
            _csv.Append(",moving,far\n");
        }

        public void BeginFrame()
        {
            _sources.Clear();
            _sourceReach.Clear();
        }

        public void Record(GroundSimulation ground, Color[] motion, Color[] crush, int frame, float time)
        {
            Probe(ground, motion, crush, frame, time, _csv);
            Vector2 activity = Activity(ground, motion);
            _movingSum += activity.x;
            _farSum += activity.y;
            _farPeak = Mathf.Max(_farPeak, activity.y);
            _frames++;
            _csv.Length--;
            _csv.Append(',').Append(activity.x.ToString("0.0000", CultureInfo.InvariantCulture))
                .Append(',').Append(activity.y.ToString("0.0000", CultureInfo.InvariantCulture)).Append('\n');
        }

        public void Write(string folder)
        {
            File.WriteAllText(Path.Combine(folder, "probes.csv"), _csv.ToString());
            Debug.Log($"[GrassLabRun] moving {_movingSum / _frames:P1} of the field on average, "
                      + $"far from every source {_farSum / _frames:P1} on average and {_farPeak:P1} at worst");
        }

        static void Probe(GroundSimulation ground, Color[] motion, Color[] crush, int frame, float time,
                          StringBuilder csv)
        {
            csv.Append(frame.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(time.ToString("0.0000", CultureInfo.InvariantCulture));
            foreach (Vector2 probe in probes)
            {
                Color lean = motion != null ? At(ground.volume, motion, probe) : Color.clear;
                Color flat = crush != null ? At(ground.volume, crush, probe) : Color.clear;
                csv.Append(',').Append(lean.r.ToString("0.00000", CultureInfo.InvariantCulture))
                   .Append(',').Append(lean.g.ToString("0.00000", CultureInfo.InvariantCulture))
                   .Append(',').Append(flat.r.ToString("0.00000", CultureInfo.InvariantCulture));
            }

            csv.Append('\n');
        }

        // The share of the field whose grass swings faster than a calm sway, and the share doing so more than
        // Reach from every creature and every zone that moves grass: motion nobody can trace to a source
        static readonly float swingSpeed = 0.5f;
        static readonly float reach = 1.5f;

        Vector2 Activity(GroundSimulation ground, Color[] motion)
        {
            if (ground == null || motion == null)
            {
                return Vector2.zero;
            }

            int inside = 0;
            int moving = 0;
            int far = 0;
            for (float z = area.yMin + 0.05f; z < area.yMax; z += 0.1f)
            {
                for (float x = area.xMin + 0.05f; x < area.xMax; x += 0.1f)
                {
                    Vector2 point = new Vector2(x, z);
                    Color texel = At(ground.volume, motion, point);
                    inside++;
                    if (new Vector2(texel.b, texel.a).magnitude < swingSpeed)
                    {
                        continue;
                    }

                    moving++;
                    if (DistanceToSources(point) > reach)
                    {
                        far++;
                    }
                }
            }

            return new Vector2((float)moving / inside, (float)far / inside);
        }

        // What the scenario plays this frame, as segments with a reach round them
        readonly List<Vector4> _sources = new List<Vector4>();
        readonly List<float> _sourceReach = new List<float>();

        public void Source(Vector3 position, float radius)
        {
            _sources.Add(new Vector4(position.x, position.z, position.x, position.z));
            _sourceReach.Add(radius);
        }

        public void Source(Vector3 from, Vector3 to)
        {
            _sources.Add(new Vector4(from.x, from.z, to.x, to.z));
            _sourceReach.Add(0f);
        }

        float DistanceToSources(Vector2 point)
        {
            float nearest = float.MaxValue;
            foreach (GrassLabScene.CreatureBody creature in _creatures)
            {
                Vector3 position = creature.anchor.position;
                nearest = Mathf.Min(nearest, Vector2.Distance(point, new Vector2(position.x, position.z)));
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                Vector4 source = _sources[i];
                float distance = DistanceToSegment(point, new Vector2(source.x, source.y), new Vector2(source.z, source.w));
                nearest = Mathf.Min(nearest, Mathf.Max(0f, distance - _sourceReach[i]));
            }

            return nearest;
        }

        static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 axis = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, axis) / Mathf.Max(axis.sqrMagnitude, 1e-6f));
            return Vector2.Distance(point, start + axis * t);
        }

        public static Color At(GroundVolume volume, Color[] pixels, Vector2 world)
        {
            Vector2 uv = volume.ToUV(world);
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * volume.width), 0, volume.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * volume.height), 0, volume.height - 1);
            return pixels[y * volume.width + x];
        }

    }
}
