using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // What a studio camera frames when it fits its view
    public interface IPreviewSubject
    {
        Bounds GetSubjectBounds();
    }
}
