using Unity.Cinemachine;
using UnityEngine;

// What the player sees of a MarkedStrikeSkill: a marker on the marked unit with the time left, an arc from
// the entity to it, and an impact effect and a camera shake when the strike lands. Added by the skill when it has visuals.
public class MarkedStrikeView : MonoBehaviour
{
    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    const int ArcSegments = 16;

    MarkedStrikeSkill _skill;
    MarkedStrikeSkillData _data;
    GameObject _shownTarget;
    StrikeMarker _marker;
    LineRenderer _arc;
    MaterialPropertyBlock _propertyBlock;
    CinemachineImpulseSource _shakeSource;

    public StrikeMarker marker => _marker;
    public CinemachineImpulseSource shakeSource => _shakeSource;
    public bool isArcShown => _arc != null && _arc.gameObject.activeSelf;

    public void Init(MarkedStrikeSkill skill)
    {
        _skill = skill;
        _data = skill.data;
        _skill.OnStrike.AddListener(ShowImpact);

        if (_data.impactShakeForce > 0f)
        {
            // A short bump, felt by the impulse listener of the game camera
            _shakeSource = gameObject.AddComponent<CinemachineImpulseSource>();
            _shakeSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _shakeSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _shakeSource.ImpulseDefinition.ImpulseDuration = _data.impactShakeDuration;
            _shakeSource.DefaultVelocity = Vector3.down;
        }
    }

    void OnDestroy()
    {
        if (_skill != null)
        {
            _skill.OnStrike.RemoveListener(ShowImpact);
        }
        HideMark();
    }

    void LateUpdate()
    {
        Refresh(Time.unscaledTime);
    }

    // Follows the mark of the skill: shown on the marked unit, hidden once the strike landed or was lost
    public void Refresh(float time)
    {
        // A destroyed unit counts as no target, and the references are compared as such: a destroyed
        // unit compares equal to null for Unity, it would never be seen as gone
        GameObject target = _skill.markedTarget != null ? _skill.markedTarget : null;
        if (!ReferenceEquals(target, _shownTarget))
        {
            HideMark();
            if (target != null)
            {
                ShowMark(target);
            }
            _shownTarget = target;
        }

        if (target == null)
        {
            return;
        }

        if (_marker != null)
        {
            _marker.SetRemaining(_skill.remainingDelay);
        }
        if (_arc != null)
        {
            TargetLines.DrawArc(_arc, TargetLines.GetPoint(gameObject), TargetLines.GetPoint(target), _data.arcHeightPerDistance);
            ScrollArc(time);
        }
    }

    void ShowMark(GameObject target)
    {
        if (_data.markerPrefab != null)
        {
            _marker = Instantiate(_data.markerPrefab);
            _marker.Attach(target);
            _marker.SetRemaining(_skill.remainingDelay);
        }

        if (_data.arcPrefab != null)
        {
            if (_arc == null)
            {
                _arc = Instantiate(_data.arcPrefab, transform);
                _arc.useWorldSpace = true;
                _arc.positionCount = ArcSegments + 1;
                _arc.textureMode = LineTextureMode.Tile;
                _arc.textureScale = new Vector2(1f / _data.arcDashLength, 1f);
                _arc.startColor = _data.arcColor;
                _arc.endColor = _data.arcColor;
            }
            _arc.gameObject.SetActive(true);
        }
    }

    void HideMark()
    {
        // The marker may already be gone with the unit it was on
        if (_marker != null)
        {
            // Destroy is not allowed outside the play mode (edit mode tests)
            if (Application.isPlaying)
            {
                Destroy(_marker.gameObject);
            }
            else
            {
                DestroyImmediate(_marker.gameObject);
            }
        }
        _marker = null;
        if (_arc != null)
        {
            _arc.gameObject.SetActive(false);
        }
    }

    void ScrollArc(float time)
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        _propertyBlock.SetVector(BaseMapST, new Vector4(1f, 1f, TargetLines.GetScrollOffset(time, _data.arcScrollSpeed, _data.arcDashLength), 0f));
        _arc.SetPropertyBlock(_propertyBlock);
    }

    void ShowImpact(GameObject target)
    {
        if (_data.impactPrefab != null && target != null)
        {
            // Not parented: the effect plays to its end even if the unit dies from the strike
            Instantiate(_data.impactPrefab, TargetLines.GetPoint(target), Quaternion.identity);
        }
        if (_shakeSource != null)
        {
            _shakeSource.GenerateImpulseWithForce(_data.impactShakeForce);
        }
    }
}
