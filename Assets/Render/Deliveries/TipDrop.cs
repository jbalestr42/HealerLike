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
        public float elapsed { get { return _elapsed; } }

        public void Init(LookPart part, Mesh mesh, Material material, Color colour, Vector3 position, float size)
        {
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(RenderObjects.BaseColorId, PrimitiveMeshes.Brighten(colour, part.glow));
            renderer.SetPropertyBlock(block);
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
