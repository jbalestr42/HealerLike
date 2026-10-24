using UnityEngine;

// The outline of the cell a dragged party card would be placed on
public sealed class ToolkitPlacementCell : System.IDisposable
{
    readonly GameObject _host;
    readonly LineRenderer _line;
    readonly Material _material;
    readonly float _size;
    public ToolkitPlacementCell(float size)
    {
        _size = size * 0.46f;
        _host = new GameObject("Roster placement cell");
        _line = _host.AddComponent<LineRenderer>();
        _material = new Material(Resources.Load<Shader>("UI/Toolkit/CompactPlacement"));
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

    // Destroy only works in play mode, the edit mode tests need DestroyImmediate
    static void Release(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    #region IDisposable

    public void Dispose() { Release(_host); Release(_material); }

    #endregion
}
