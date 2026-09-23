using System;

namespace HealerLike.Render.Environment
{
    [Serializable]
    public struct EnvironmentCounts
    {
        public static readonly EnvironmentCounts Default = new EnvironmentCounts
        {
            boulders = 120,
            cairns = 40,
            monoliths = 3,
            mushroomTrees = 60,
            spiralFerns = 80,
            bladeRosettes = 120,
            sphereClusters = 70
        };

        public int boulders;
        public int cairns;
        public int monoliths;
        public int mushroomTrees;
        public int spiralFerns;
        public int bladeRosettes;
        public int sphereClusters;

        public int this[EnvironmentKind kind]
        {
            get
            {
                switch (kind)
                {
                    case EnvironmentKind.Boulder:
                        return boulders;
                    case EnvironmentKind.Cairn:
                        return cairns;
                    case EnvironmentKind.Monolith:
                        return monoliths;
                    case EnvironmentKind.MushroomTree:
                        return mushroomTrees;
                    case EnvironmentKind.SpiralFern:
                        return spiralFerns;
                    case EnvironmentKind.BladeRosette:
                        return bladeRosettes;
                    default:
                        return sphereClusters;
                }
            }
        }
    }
}
