using UnityEngine;

namespace HealerLike.Render.Stage
{
    public sealed class StagePlacementCell : System.IDisposable
    {
        readonly GameObject _host;
        readonly LineRenderer _line;
        readonly Material _material;
        readonly float _size;
        public StagePlacementCell(float size)
        {
            _size = size * 0.46f;
            _host = new GameObject("Roster placement cell");
            _line = _host.AddComponent<LineRenderer>();
            _material = new Material(Resources.Load<Shader>("CompactPlacement"));
            _line.sharedMaterial = _material;
            _line.loop = true; _line.positionCount = 4;
            _line.startWidth = _line.endWidth = size * 0.035f;
            _line.startColor = _line.endColor = new Color(0.88f, 1, 0.7f, 0.95f);
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
        }
        public void Show(bool valid, Vector3 point)
        {
            _host.SetActive(valid);
            if (!valid)
            {
                return;
            }

            point.y += 0.04f;
            _line.SetPositions(new[] { point + new Vector3(-_size, 0, -_size),
                point + new Vector3(_size, 0, -_size), point + new Vector3(_size, 0, _size),
                point + new Vector3(-_size, 0, _size) });
        }
        public void Dispose() { RenderObjects.Release(_host); RenderObjects.Release(_material); }
    }
}
