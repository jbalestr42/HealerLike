using HealerLike.Render.Stage;

namespace HealerLike.Render
{
    // A view under an entity model, set up by the RenderManager when the entity spawns
    public interface IEntityView
    {
        void Init(Entity entity, RenderManager manager);
    }
}
