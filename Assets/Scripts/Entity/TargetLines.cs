using System.Collections.Generic;
using UnityEngine;

// Arcs from the selected entity to its targets: the ones it attacks in battle, the ones it would aim at
// from where it stands during the placement. Hidden until SelectableEntity shows it, and never shown for an
// entity without attack (e.g. a unit which only buffs its allies).
public class TargetLines : MonoBehaviour
{
    // Look of a line (material, width...), its color is the one of the side of the entity
    [SerializeField] LineRenderer _linePrefab;
    [SerializeField] Color _playerColor = new Color(0f, 1f, 1f);
    [SerializeField] Color _computerColor = new Color(1f, 0f, 1f);
    // Height of the top of the arc for each unit of distance to the target
    [SerializeField] float _arcHeightPerDistance = 0.35f;
    [SerializeField, Min(1)] int _segments = 16;
    // The dashes of the line texture scroll toward the target: length of a dash and speed, in world units
    [SerializeField, Min(0.01f)] float _dashLength = 0.3f;
    [SerializeField] float _scrollSpeed = 0.6f;
    // Random targets would change every frame, the preview is refreshed at this pace
    [SerializeField] float _refreshPeriod = 0.25f;

    readonly List<LineRenderer> _lines = new List<LineRenderer>();
    readonly List<Vector3> _targetPositions = new List<Vector3>();
    Entity _entity;
    MaterialPropertyBlock _propertyBlock;
    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    bool _isShown = false;
    bool _hasAttack = true;
    float _nextRefreshTime = 0f;

    public int visibleLineCount => _lines.FindAll(line => line.gameObject.activeSelf).Count;

    void Awake()
    {
        _entity = GetComponent<Entity>();
    }

    // Entity.Init, right after the spawn, has added the skills of its data by then
    void Start()
    {
        if (_entity != null)
        {
            _hasAttack = HasAttack(_entity.skills);
        }
    }

    // One of the skills attacks the targets
    public static bool HasAttack(List<ASkill> skills)
    {
        foreach (ASkill skill in skills)
        {
            if (skill is IAttackSkill attack && attack.attacksTargets)
            {
                return true;
            }
        }
        return false;
    }

    public void Show(bool isShown)
    {
        _isShown = isShown;
        _nextRefreshTime = 0f;
        if (!isShown)
        {
            SetLines(Vector3.zero, new List<Vector3>());
        }
    }

    void LateUpdate()
    {
        if (_isShown)
        {
            ScrollDashes(Time.unscaledTime);
        }

        if (!_isShown || !_hasAttack || _entity == null || _entity.targetProvider == null || Time.unscaledTime < _nextRefreshTime)
        {
            return;
        }
        _nextRefreshTime = Time.unscaledTime + _refreshPeriod;

        _targetPositions.Clear();
        foreach (GameObject target in _entity.targetProvider.GetPreviewTargets())
        {
            if (target != null)
            {
                _targetPositions.Add(GetPoint(target));
            }
        }
        SetLines(GetPoint(gameObject), _targetPositions);
    }

    public static Vector3 GetPoint(GameObject go)
    {
        Entity entity = go.GetComponent<Entity>();
        return entity != null && entity.targetPoint != null ? entity.targetPoint.transform.position : go.transform.position;
    }

    // One line per target from the given point, the lines left are hidden
    public void SetLines(Vector3 from, List<Vector3> targets)
    {
        while (_lines.Count < targets.Count)
        {
            _lines.Add(CreateLine());
        }

        for (int i = 0; i < _lines.Count; i++)
        {
            bool isUsed = i < targets.Count;
            _lines[i].gameObject.SetActive(isUsed);
            if (isUsed)
            {
                DrawArc(_lines[i], from, targets[i], _arcHeightPerDistance);
            }
        }
    }

    // Moves the texture along every line, toward its target (unscaled: it keeps moving when the game is paused)
    void ScrollDashes(float time)
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        _propertyBlock.SetVector(BaseMapST, new Vector4(1f, 1f, GetScrollOffset(time, _scrollSpeed, _dashLength), 0f));
        foreach (LineRenderer line in _lines)
        {
            if (line.gameObject.activeSelf)
            {
                line.SetPropertyBlock(_propertyBlock);
            }
        }
    }

    // Texture offset along the line: decreasing, so the pattern moves from the entity to its target
    public static float GetScrollOffset(float time, float speed, float dashLength)
    {
        return Mathf.Repeat(-time * speed / dashLength, 1f);
    }

    // Bends the line into an arc going up from the start to the end, higher for a farther end
    public static void DrawArc(LineRenderer line, Vector3 from, Vector3 to, float arcHeightPerDistance)
    {
        int segments = line.positionCount - 1;
        float height = Vector3.Distance(from, to) * arcHeightPerDistance;
        for (int point = 0; point <= segments; point++)
        {
            line.SetPosition(point, GetArcPoint(from, to, (float)point / segments, height));
        }
    }

    // Point of a parabola going up from the start to the end: the straight line raised by height at the middle
    public static Vector3 GetArcPoint(Vector3 from, Vector3 to, float t, float height)
    {
        return Vector3.Lerp(from, to, t) + Vector3.up * (4f * height * t * (1f - t));
    }

    LineRenderer CreateLine()
    {
        LineRenderer line = Instantiate(_linePrefab, transform);
        // From the entity to its target, wherever the entity is
        line.useWorldSpace = true;
        line.positionCount = _segments + 1;
        // One dash every _dashLength world units, whatever the length of the line
        line.textureMode = LineTextureMode.Tile;
        line.textureScale = new Vector2(1f / _dashLength, 1f);
        Color color = _entity != null && _entity.entityType == Entity.EntityType.Computer ? _computerColor : _playerColor;
        line.startColor = color;
        line.endColor = color;
        return line;
    }
}
