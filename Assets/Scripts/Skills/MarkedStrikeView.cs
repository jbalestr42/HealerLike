using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// What the player sees of a MarkedStrikeSkill: a marker on each marked unit, smaller on the units taking less
// damage, the time left on the main one, arcs going from the entity to the main unit then from unit to unit in
// the order of their part of the strike, and an impact effect and a camera shake when the strike lands. Added by
// the skill when it has visuals.
public class MarkedStrikeView : MonoBehaviour
{
    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    const int ArcSegments = 16;

    // What is shown on one marked unit
    class Shown
    {
        public GameObject target;
        public StrikeMarker marker;
        public LineRenderer arc;
    }

    MarkedStrikeSkill _skill;
    MarkedStrikeSkillData _data;
    readonly List<Shown> _shown = new List<Shown>();
    // Arcs kept for the next marks, hidden while unused
    readonly List<LineRenderer> _arcs = new List<LineRenderer>();
    MaterialPropertyBlock _propertyBlock;
    CinemachineImpulseSource _shakeSource;
    // The strike lands on every marked unit in the same frame: the camera shakes once
    int _lastShakeFrame = -1;

    // The marker of the first marked unit, null when nothing is marked
    public StrikeMarker marker => _shown.Count > 0 ? _shown[0].marker : null;
    // The markers of the marked units, in the order of the marks
    public List<StrikeMarker> markers => _shown.ConvertAll(shown => shown.marker);
    // The arcs toward the marked units, in the order of the marks
    public List<LineRenderer> shownArcs => _shown.ConvertAll(shown => shown.arc);
    public CinemachineImpulseSource shakeSource => _shakeSource;
    public bool isArcShown => _arcs.Exists(arc => arc != null && arc.gameObject.activeSelf);
    public int shownArcCount => _arcs.FindAll(arc => arc != null && arc.gameObject.activeSelf).Count;

    // Size of the marker of a unit taking this part of the strike: its area follows the damage (half the damage,
    // 0.71 of the size), the whole strike keeping the size of the prefab
    public static float GetMarkerScale(float damageMultiplier)
    {
        return Mathf.Sqrt(Mathf.Clamp01(damageMultiplier));
    }

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
        HideMarks();
    }

    void LateUpdate()
    {
        Refresh(Time.unscaledTime);
    }

    // Follows the marks of the skill: shown on the marked units, hidden once the strike landed or was lost
    public void Refresh(float time)
    {
        if (!IsShowingTheMarks())
        {
            HideMarks();
            foreach (StrikeMark mark in _skill.marks)
            {
                // A destroyed unit counts as no target
                if (mark.target != null)
                {
                    ShowMark(mark);
                }
            }
        }

        // The arcs chain the marked units in the order of their part of the strike: from the entity to the main
        // one, then from each unit to the next
        Vector3 from = TargetLines.GetPoint(gameObject);
        foreach (Shown shown in _shown)
        {
            if (shown.marker != null)
            {
                shown.marker.SetRemaining(_skill.remainingDelay);
            }
            Vector3 to = TargetLines.GetPoint(shown.target);
            if (shown.arc != null)
            {
                TargetLines.DrawArc(shown.arc, from, to, _data.arcHeightPerDistance);
                ScrollArc(shown.arc, time);
            }
            from = to;
        }
    }

    // The units shown are the marked units still there, in the same order. The references are compared as such:
    // a destroyed unit compares equal to null for Unity, it would never be seen as gone
    bool IsShowingTheMarks()
    {
        int index = 0;
        foreach (StrikeMark mark in _skill.marks)
        {
            GameObject target = mark.target != null ? mark.target : null;
            if (target == null)
            {
                continue;
            }
            if (index >= _shown.Count || !ReferenceEquals(_shown[index].target, target))
            {
                return false;
            }
            index++;
        }
        return index == _shown.Count;
    }

    void ShowMark(StrikeMark mark)
    {
        Shown shown = new Shown { target = mark.target };
        float scale = GetMarkerScale(mark.damageMultiplier);
        if (_data.markerPrefab != null)
        {
            shown.marker = Instantiate(_data.markerPrefab);
            shown.marker.Attach(mark.target);
            shown.marker.transform.localScale = _data.markerPrefab.transform.localScale * scale;
            shown.marker.SetRemaining(_skill.remainingDelay);
            // A single countdown, on the main unit
            shown.marker.ShowCountdown(_shown.Count == 0);
        }

        if (_data.arcPrefab != null)
        {
            shown.arc = GetFreeArc();
            shown.arc.widthMultiplier = _data.arcPrefab.widthMultiplier * scale;
            shown.arc.gameObject.SetActive(true);
        }
        _shown.Add(shown);
    }

    LineRenderer GetFreeArc()
    {
        LineRenderer arc = _arcs.Find(candidate => candidate != null && !candidate.gameObject.activeSelf);
        if (arc != null)
        {
            return arc;
        }

        arc = Instantiate(_data.arcPrefab, transform);
        arc.useWorldSpace = true;
        arc.positionCount = ArcSegments + 1;
        arc.textureMode = LineTextureMode.Tile;
        arc.textureScale = new Vector2(1f / _data.arcDashLength, 1f);
        arc.startColor = _data.arcColor;
        arc.endColor = _data.arcColor;
        _arcs.Add(arc);
        return arc;
    }

    void HideMarks()
    {
        foreach (Shown shown in _shown)
        {
            // The marker may already be gone with the unit it was on
            if (shown.marker != null)
            {
                // Destroy is not allowed outside the play mode (edit mode tests)
                if (Application.isPlaying)
                {
                    Destroy(shown.marker.gameObject);
                }
                else
                {
                    DestroyImmediate(shown.marker.gameObject);
                }
            }
            if (shown.arc != null)
            {
                shown.arc.gameObject.SetActive(false);
            }
        }
        _shown.Clear();
    }

    void ScrollArc(LineRenderer arc, float time)
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        _propertyBlock.SetVector(BaseMapST, new Vector4(1f, 1f, TargetLines.GetScrollOffset(time, _data.arcScrollSpeed, _data.arcDashLength), 0f));
        arc.SetPropertyBlock(_propertyBlock);
    }

    void ShowImpact(GameObject target)
    {
        if (_data.impactPrefab != null && target != null)
        {
            // Not parented: the effect plays to its end even if the unit dies from the strike
            Instantiate(_data.impactPrefab, TargetLines.GetPoint(target), Quaternion.identity);
        }
        if (_shakeSource != null && _lastShakeFrame != Time.frameCount)
        {
            _lastShakeFrame = Time.frameCount;
            _shakeSource.GenerateImpulseWithForce(_data.impactShakeForce);
        }
    }
}
