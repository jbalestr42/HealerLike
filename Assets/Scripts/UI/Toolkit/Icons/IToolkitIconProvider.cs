using System;
using UnityEngine;

// Optional presentation supplied by the host. The provider owns its textures; the view only borrows them.
public interface IToolkitIconProvider
{
    event Action Changed;
    Texture2D GetCreatureIcon(EntityData data, Entity.EntityType side);
}
