using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // The pod an area item drops from a tip at contact, it falls and shrinks away
    public class TipDrop : MonoBehaviour
    {
        public static readonly float Lifetime = 0.3f;
        // In pod sizes, so a larger tip drops further
        public static readonly float Fall = 1.5f;

        Vector3 _start;
        Vector3 _scale;
        float _size;
        bool _isInitialized;

        float _elapsed;

        // The pod an area shot drops at its first contact, in the shot's colour and at its size
        public static void Splash(DeliveryVocabulary vocabulary, PrimitiveMeshes meshes,
            List<AConsumerFactory> consumers, Vector3 point)
        {
            if (!vocabulary || !meshes || !vocabulary.material)
            {
                return;
            }

            TipDrop drop = new GameObject("TipDrop").AddComponent<TipDrop>();
            drop.Init(vocabulary.splashPod, meshes.GetMesh(vocabulary.splashPod.primitive), vocabulary.material,
                vocabulary.ShotColour(consumers), point, vocabulary.bulletSize);
        }

        public void Init(LookPart part, Mesh mesh, Material material, Color colour, Vector3 position, float size)
        {
            PrimitiveMeshes.Geometry(gameObject, mesh, material, colour, part.glow, new MaterialPropertyBlock());
            _size = size;
            _start = position + part.position * size;
            _scale = part.size * size;
            transform.SetPositionAndRotation(_start, Quaternion.Euler(part.euler));
            transform.localScale = _scale;
            _isInitialized = true;
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!_isInitialized)
            {
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(_elapsed / Lifetime);
            transform.position = _start + Vector3.down * (Fall * _size * t * t);
            transform.localScale = _scale * (1f - t);
            if (t < 1f)
            {
                return;
            }

            _isInitialized = false;
            RenderObjects.Release(gameObject);
        }
    }
}
