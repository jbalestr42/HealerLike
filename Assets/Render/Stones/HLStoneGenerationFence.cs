using System.Collections;

namespace HealerLike.Render.Stones
{
    // Authored last in the existing generator list. Does not spawn objects or consume gameplay random values.
    public class HLStoneGenerationFence : GridGeneratorSystem
    {
        HLStoneGridEntry _entry;

        public void Bind(HLStoneGridEntry owner)
        {
            _entry = owner;
        }

        public override IEnumerator Spawn(GridGenerator generator)
        {
            _entry?.CompleteGeneration();
            yield break;
        }
    }
}
