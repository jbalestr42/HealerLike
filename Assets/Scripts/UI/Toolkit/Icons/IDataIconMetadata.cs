using UnityEngine;

// Optional presentation for custom data; unregistered objects use their object or type name.
public interface IDataIconMetadata
{
    string label { get; }
    Sprite icon { get; }
}
