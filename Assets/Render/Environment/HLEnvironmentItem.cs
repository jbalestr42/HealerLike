using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum HLEnvironmentKind
    {
        Boulder,
        Cairn,
        Monolith,
        MushroomTree,
        SpiralFern,
        BladeRosette,
        SphereCluster
    }

    public struct HLEnvironmentItem
    {
        public HLEnvironmentKind kind;
        public Vector3 position;
        public float yaw;
        public float scale;
        public float distance01;
        public uint seed;
        public int paletteIndex;
    }
}
