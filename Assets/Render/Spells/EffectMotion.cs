using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Spells
{
    // Where a shape part stands at one point of its element's motion, in the element's own space
    public struct PartPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    // What of an element's own state its motion reads beside the recipe
    public struct MotionState
    {
        public bool isStatus;
        public bool isRemoving;
        // How far through its removal the element is, from 0 as it begins to 1 once it has closed
        public float removal;
        // How far a drop falls, in the element's own units
        public float fallDistance;
    }

    // The eight motion kinds and the beam's curve: a pure function of the part, the phase of the cycle and the time
    public static class EffectMotion
    {
        // A drop runs through its motion this much faster than the cycle, so it has landed before the cycle ends
        public static readonly float FallPace = 1.15f;

        // Burst: the star pops from 60% to full size over the first 30% of the cycle, the cone shards fly out
        // and up, each a little higher than the one before, fall under gravity and spin
        static readonly float starStartSize = 0.6f;
        static readonly float starPopPhase = 0.3f;
        static readonly float shardOutwardSpeed = 4f;
        static readonly float shardUpSpeed = 5.5f;
        static readonly float shardUpSpeedStep = 0.6f;
        static readonly float shardGravity = 14f;
        static readonly Vector3 shardSpin = new Vector3(180f, 70f, 0f);
        // Rise: each sphere starts a little later, grows from 20% over the first 60%, pops from 78% over 16%
        static readonly float riseHeight = 1.8f;
        static readonly float riseStagger = 0.025f;
        static readonly float riseStartSize = 0.2f;
        static readonly float riseGrowPhase = 0.6f;
        static readonly float risePopStart = 0.78f;
        static readonly float risePopPhase = 0.16f;
        // Grow: emerges over the first 30%, a single run sinks half its height over the last 20%; once held, each
        // tick lifts it from 70% of its height
        static readonly float growEmergePhase = 0.3f;
        static readonly float growExitStart = 0.8f;
        static readonly float growExitPhase = 0.2f;
        static readonly float growHeldCycles = 2f;
        static readonly float growHeldLow = 0.7f;
        // Fall: each drop starts a little later, swells from 30% over its first 20%, falls over its last 60%
        // and shrinks away in the last sixth of the fall
        static readonly float fallStagger = 0.03f;
        static readonly float fallStart = 0.4f;
        static readonly float fallSwellSize = 0.3f;
        static readonly float fallSwellPhase = 0.2f;
        static readonly float fallShrinkRate = 6f;
        // Orbit: the two tilted planes turn opposite ways, in degrees a second, and the tori breathe a little
        static readonly float orbitSpinEven = 34f;
        static readonly float orbitSpinOdd = -28f;
        static readonly float orbitBreath = 0.035f;
        static readonly float orbitBreathRate = 2.4f;
        // Close: the plates start this far open, turned out by the angle, and close over one cycle
        static readonly float closeSpread = 1.3f;
        static readonly float closeAngle = -32f;
        // Press: the cones press this far down and back once a cycle
        static readonly float pressDepth = 0.2f;
        // Shed: each plate is offset in the cycle, appears over the first 15%, falls from 55% to the end,
        // dropping, drifting outward and tipping over
        static readonly float shedOffset = 0.13f;
        static readonly float shedAppearPhase = 0.15f;
        static readonly float shedFallStart = 0.55f;
        static readonly float shedDrop = 0.6f;
        static readonly float shedDrift = 0.25f;
        static readonly float shedTilt = 25f;
        // Beam: the arch rises with the distance up to a limit; a contact thread wavers along its length
        static readonly float beamArchPerUnit = 0.2f;
        static readonly float beamArchMax = 0.7f;
        static readonly float threadWaves = 8f;
        static readonly float threadWaver = 0.035f;
        // Shortest cycle the motion divides by
        static readonly float minCycle = 0.01f;

        public static PartPose Pose(EffectRecipe recipe, LookPart part, int index, float phase, float time,
                                    MotionState state)
        {
            PartPose pose = new PartPose();
            pose.position = part.position;
            pose.rotation = Quaternion.Euler(part.euler);
            pose.scale = part.size;
            switch (recipe.motion)
            {
                case EffectMotionKind.Burst:
                    Burst(recipe, part, index, phase, ref pose);
                    break;
                case EffectMotionKind.Rise:
                    Rise(part, index, phase, ref pose);
                    break;
                case EffectMotionKind.Grow:
                    Grow(recipe, part, phase, time, state, ref pose);
                    break;
                case EffectMotionKind.Fall:
                    Fall(part, index, phase, state, ref pose);
                    break;
                case EffectMotionKind.Orbit:
                    // The tilted plane turns around the body, spinning a torus in its own plane would not show
                    float spin = orbitSpinOdd;
                    if (index % 2 == 0)
                    {
                        spin = orbitSpinEven;
                    }

                    pose.rotation = Quaternion.Euler(0f, time * spin, 0f) * pose.rotation;
                    pose.scale = part.size * (1f + orbitBreath * Mathf.Sin(time * orbitBreathRate + index));
                    break;
                case EffectMotionKind.Close:
                    Close(recipe, part, time, state, ref pose);
                    break;
                case EffectMotionKind.Press:
                    float press = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
                    pose.position = part.position + Vector3.down * (pressDepth * press);
                    break;
                case EffectMotionKind.Shed:
                    Shed(part, index, phase, ref pose);
                    break;
            }

            return pose;
        }

        // A beam arches from start to end
        public static Vector3 Curve(Vector3 start, Vector3 end, float t)
        {
            float arch = Mathf.Min(beamArchMax, Vector3.Distance(start, end) * beamArchPerUnit);
            return Vector3.Lerp(start, end, t) + Vector3.up * (4f * t * (1f - t) * arch);
        }

        // A contact thread runs straight and wavers sideways, still at both ends
        public static Vector3 Thread(Vector3 start, Vector3 end, float t)
        {
            Vector3 side = Vector3.Cross((end - start).normalized, Vector3.up);
            float wave = Mathf.Sin(t * Mathf.PI * threadWaves) * Mathf.Sin(t * Mathf.PI) * threadWaver;
            return Vector3.Lerp(start, end, t) + side * wave;
        }

        // The flat star pops in place, the cones are its shards
        static void Burst(EffectRecipe recipe, LookPart part, int index, float phase, ref PartPose pose)
        {
            float shrink = Mathf.Sqrt(1f - phase);
            if (part.primitive != Primitive.Cone)
            {
                float pop = Mathf.SmoothStep(0f, 1f, phase / starPopPhase);
                pose.scale = part.size * ((starStartSize + (1f - starStartSize) * pop) * shrink);
                return;
            }

            float seconds = phase * recipe.cycleSeconds;
            Vector3 flat = new Vector3(part.position.x, 0f, part.position.z);
            Vector3 velocity = flat * shardOutwardSpeed + Vector3.up * (shardUpSpeed + index * shardUpSpeedStep);
            pose.position = part.position + velocity * seconds + Vector3.down * (shardGravity * seconds * seconds);
            pose.rotation = pose.rotation * Quaternion.Euler(shardSpin * seconds);
            pose.scale = part.size * shrink;
        }

        static void Rise(LookPart part, int index, float phase, ref PartPose pose)
        {
            float own = Mathf.Clamp01(phase * (1f + index * riseStagger));
            float grow = Mathf.SmoothStep(riseStartSize, 1f, Mathf.Clamp01(own / riseGrowPhase));
            float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((own - risePopStart) / risePopPhase));
            pose.position = part.position + Vector3.up * (own * riseHeight);
            pose.scale = part.size * (grow * (1f - pop));
        }

        static void Grow(EffectRecipe recipe, LookPart part, float phase, float time, MotionState state,
                         ref PartPose pose)
        {
            float emerge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase / growEmergePhase));
            // A single run sinks away at its end, a ticking or held one stays up until it grows again
            float exit = 0f;
            if (recipe.tempo == EffectTempo.Once || !state.isStatus)
            {
                exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - growExitStart) / growExitPhase));
            }

            // After its first growth a ticking or held element stays up and each tick lifts it from lower down
            bool isHeld = state.isStatus && recipe.tempo != EffectTempo.Once;
            if (isHeld && time >= growHeldCycles * recipe.cycleSeconds)
            {
                float lifted = part.position.y * (growHeldLow + (1f - growHeldLow) * emerge);
                pose.position = new Vector3(part.position.x, lifted, part.position.z);
                return;
            }

            float height = part.position.y * emerge - part.size.y * 0.5f * exit;
            pose.position = new Vector3(part.position.x, height, part.position.z);
            pose.scale = part.size * (emerge * (1f - exit));
        }

        // A drop swells where it hangs for the first part of the cycle, then falls and shrinks at the end
        static void Fall(LookPart part, int index, float phase, MotionState state, ref PartPose pose)
        {
            float own = Mathf.Clamp01(phase * FallPace - index * fallStagger);
            float fall = Mathf.Clamp01((own - fallStart) / (1f - fallStart));
            pose.position = part.position + Vector3.down * (fall * fall * state.fallDistance);
            float swell = Mathf.SmoothStep(fallSwellSize, 1f, Mathf.Clamp01(own / fallSwellPhase));
            pose.scale = part.size * (swell * Mathf.Clamp01((1f - fall) * fallShrinkRate));
        }

        static void Close(EffectRecipe recipe, LookPart part, float time, MotionState state, ref PartPose pose)
        {
            float closure = Mathf.Clamp01(time / Mathf.Max(minCycle, recipe.cycleSeconds));
            if (state.isRemoving)
            {
                closure = 1f - state.removal;
            }

            pose.rotation = pose.rotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(closeAngle, 0f, closure));
            float spread = Mathf.Lerp(closeSpread, 1f, closure);
            pose.position = new Vector3(part.position.x * spread, part.position.y, part.position.z * spread);
        }

        static void Shed(LookPart part, int index, float phase, ref PartPose pose)
        {
            float own = Mathf.Repeat(phase + index * shedOffset, 1f);
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((own - shedFallStart) / (1f - shedFallStart)));
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(own / shedAppearPhase));
            Vector3 outward = new Vector3(part.position.x, 0f, part.position.z).normalized;
            pose.position = part.position + Vector3.down * (fall * shedDrop) + outward * (fall * shedDrift);
            pose.rotation = pose.rotation * Quaternion.Euler(fall * shedTilt, 0f, 0f);
            pose.scale = part.size * (appear * (1f - fall));
        }
    }
}
