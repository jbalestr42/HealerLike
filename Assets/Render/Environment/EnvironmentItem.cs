using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum EnvironmentKind
    {
        Boulder,
        Cairn,
        Monolith,
        MushroomTree,
        SpiralFern,
        BladeRosette,
        SphereCluster
    }

    public struct EnvironmentItem
    {
        public EnvironmentKind kind;
        public Vector3 position;
        public float yaw;
        public float scale;
        public float distance01;
        public uint seed;
        public int paletteIndex;
    }
}
