using System.Collections;
namespace HealerLike.Render.Stones
{
    // Authored last in the existing generator list. Does not spawn objects or consume gameplay random values.
    public sealed class HLStoneGenerationFence : GridGeneratorSystem
    {
        HLStoneGridEntry entry;
        public void Bind(HLStoneGridEntry owner) { entry=owner; }
        public override IEnumerator Spawn(GridGenerator generator)
        {
            entry?.CompleteGeneration(); yield break;
        }
    }
}
