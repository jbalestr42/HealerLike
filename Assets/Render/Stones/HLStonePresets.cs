namespace HealerLike.Render.Stones
{
    public enum HLStonePreset { Boulder, Cairn, Monolith }
    public static class HLStonePresets
    {
        public static HLStoneSettings Boulder => Shape(.58f, .85f, .9f, .14f);
        public static HLStoneSettings Cairn => Shape(.55f, .65f, .9f, .12f);
        public static HLStoneSettings Monolith => Shape(.52f, 2.8f, .8f, .10f);
        public static HLStoneSettings Shape(float size, float elongation, float depth = .9f, float roughness = .12f, int subdivisions = 1)
            => new HLStoneSettings { Size=size, Elongation=elongation, DepthRatio=depth, Roughness=roughness, Subdivisions=subdivisions };
    }
}
