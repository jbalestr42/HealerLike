using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public enum HLPrimitive { Sphere, Capsule, Cone, Torus, CylinderSegment }
    [Serializable]
    public struct HLPart
    {
        public string id;
        public int parent;
        public HLPrimitive primitive;
        public Vector3 localPosition, localEuler, dimensions;
        public Color colour;
        public float torusTubeRatio;
        [Min(0)] public float glow;
    }
    [Serializable]
    public struct HLArmDefinition
    {
        public int bodyPart;
        public Vector3 rootLocal;
        public int sourceSocketIndex;
        public int segmentCount;
        public float segmentLength, radius;
        public Vector3[] restJoints;
        public Vector3 bendPole;
        public Color colour;
    }
    [Serializable]
    public struct HLRootDefinition
    {
        public int count;
        public float footRadius, hipHeight, kneeHeight, thickness, angularOffset;
        public Color colour;
    }
    [Serializable]
    public struct HLIdleDefinition
    {
        public float swayDegrees, swayFrequency, breathAmount, breathFrequency;
        public int seed;
    }
    [CreateAssetMenu(menuName = "HL/Creature Recipe")]
    public sealed class HLCreatureRecipe : ScriptableObject
    {
        public HLPart[] parts = Array.Empty<HLPart>();
        public HLArmDefinition[] arms = Array.Empty<HLArmDefinition>();
        public HLRootDefinition roots = new HLRootDefinition
            { count = 4, footRadius = .38f, hipHeight = .18f, kneeHeight = .09f, thickness = .022f, colour = new Color(.18f, .49f, .31f) };
        public HLIdleDefinition idle = new HLIdleDefinition
            { swayDegrees = 2.5f, swayFrequency = .12f, breathAmount = .025f, breathFrequency = .25f, seed = 17 };
        // Recipe socket hints describe new authoring only. Runtime never relocates authored sockets.
        public Vector3 targetLocal;
        public Vector3[] sourceLocal = Array.Empty<Vector3>();
    }
}
