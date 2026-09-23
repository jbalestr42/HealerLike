using System;

namespace HealerLike.Render.Environment
{
    [Serializable]
    public struct HLEnvironmentCounts
    {
        public static readonly HLEnvironmentCounts Default = new HLEnvironmentCounts
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

        public int this[HLEnvironmentKind kind]
        {
            get
            {
                switch (kind)
                {
                    case HLEnvironmentKind.Boulder:
                        return boulders;
                    case HLEnvironmentKind.Cairn:
                        return cairns;
                    case HLEnvironmentKind.Monolith:
                        return monoliths;
                    case HLEnvironmentKind.MushroomTree:
                        return mushroomTrees;
                    case HLEnvironmentKind.SpiralFern:
                        return spiralFerns;
                    case HLEnvironmentKind.BladeRosette:
                        return bladeRosettes;
                    default:
                        return sphereClusters;
                }
            }
        }
    }
}
