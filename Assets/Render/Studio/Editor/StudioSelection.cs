using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{
    // A window borrows its selected asset and owns only the SerializedObject used to edit it.
    public class StudioSelection<AssetType> : IDisposable where AssetType : Object
    {
        AssetType _asset;
        SerializedObject _serialized;

        public AssetType asset { get { return _asset; } }
        public SerializedObject serialized { get { return _serialized; } }

        public void Select(AssetType asset)
        {
            if (_serialized != null)
            {
                _serialized.ApplyModifiedProperties();
            }

            Dispose();
            _asset = asset;
            if (asset != null)
            {
                _serialized = new SerializedObject(asset);
            }
        }

        public void Dispose()
        {
            if (_serialized != null)
            {
                _serialized.Dispose();
            }

            _serialized = null;
            _asset = null;
        }
    }
}
