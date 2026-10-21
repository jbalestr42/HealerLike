namespace HealerLike.Render.Stones
{
    public static class StonePresets
    {
        public static readonly StoneSettings Boulder = Shape(0.58f, 0.85f, 0.9f, 0.14f);
        public static readonly StoneSettings Cairn = Shape(0.55f, 0.65f, 0.9f, 0.12f);
        public static readonly StoneSettings Monolith = Shape(0.52f, 2.8f, 0.8f, 0.10f);

        public static StoneSettings Shape(float size, float elongation, float depth = 0.9f, float roughness = 0.12f,
            int subdivisions = 1)
        {
            StoneSettings settings = new StoneSettings();
            settings.size = size;
            settings.elongation = elongation;
            settings.depthRatio = depth;
            settings.roughness = roughness;
            settings.subdivisions = subdivisions;
            return settings;
        }
    }
}
