using UnityEngine;

// Optional extension to the existing provider. The host owns these textures, just like creature portraits.
// Returning null preserves the view's authored/catalog fallback for unsupported data.
public interface IToolkitDataIconProvider
{
    Texture2D GetDataIcon(object source);
}
