namespace HealerLike.Render
{
    // A view that knows its body and head, so a status can keep clear of the head
    public interface IEffectAnchors
    {
        bool TryGetAnchors(out EffectAnchors anchors);
    }
}
