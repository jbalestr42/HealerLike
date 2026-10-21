using System;
using UnityEngine;

[Serializable]
public class DataIconCatalogEntry
{
    public UnityEngine.Object source;
    public Texture2D generated;
    // Optional artwork, takes priority over the generated icon
    public Texture2D artworkOverride;
    [HideInInspector] public string fingerprint;
}
