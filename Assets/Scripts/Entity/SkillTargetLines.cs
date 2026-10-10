using System.Collections.Generic;
using UnityEngine;

// A solid arc growing from the entity to the unit one of its skills is about to be used on: it starts a moment
// before the use and reaches the unit when the skill goes off. Shown in battle for every entity, unlike the dashed
// TargetLines of the selected one.
public class SkillTargetLines : MonoBehaviour
{
    // Look of a line (material, width...), its color tells whether the skill is used on an ally or an opponent
    [SerializeField] LineRenderer _linePrefab;
    [SerializeField] Color _allyColor = new Color(0.55f, 1f, 0.45f);
    [SerializeField] Color _opponentColor = new Color(1f, 0.55f, 0.2f);
    // Battle time before the use when the line starts growing
    [SerializeField, Min(0.01f)] float _warningTime = 1.5f;
    // Height of the top of the arc for each unit of distance to the target
    [SerializeField] float _arcHeightPerDistance = 0.35f;
    [SerializeField, Min(1)] int _segments = 16;

    readonly List<LineRenderer> _lines = new List<LineRenderer>();
    readonly List<ISkillTargetPreview> _skills = new List<ISkillTargetPreview>();
    Entity _entity;

    public int visibleLineCount => _lines.FindAll(line => line.gameObject.activeSelf).Count;

    void Awake()
    {
        _entity = GetComponent<Entity>();
    }

    // Entity.Init, right after the spawn, has added the skills of its data by then (not the ones an item or a buff
    // adds later)
    void Start()
    {
        if (_entity == null)
        {
            return;
        }
        foreach (ASkill skill in _entity.skills)
        {
            if (skill is ISkillTargetPreview preview)
            {
                _skills.Add(preview);
            }
        }
    }

    void LateUpdate()
    {
        int shown = 0;
        foreach (ISkillTargetPreview skill in _skills)
        {
            // Off outside of a battle, gone once destroyed
            ASkill component = skill as ASkill;
            if (component == null || !component.isEnabled || skill.timeBeforeUse > _warningTime)
            {
                continue;
            }

            GameObject target = skill.GetUpcomingTarget();
            if (target == null)
            {
                continue;
            }

            Entity targetEntity = target.GetComponent<Entity>();
            bool isAlly = _entity != null && targetEntity != null && targetEntity.entityType == _entity.entityType;
            SetLine(shown, TargetLines.GetPoint(gameObject), TargetLines.GetPoint(target), GetProgress(skill.timeBeforeUse, _warningTime), isAlly ? _allyColor : _opponentColor);
            shown++;
        }
        HideFrom(shown);
    }

    // Part of the way to the target the line has grown: 0 when the warning starts, 1 when the skill is used
    public static float GetProgress(float timeBeforeUse, float warningTime)
    {
        return warningTime <= 0f ? 1f : Mathf.Clamp01(1f - timeBeforeUse / warningTime);
    }

    // The line of the given index, from the entity along the arc to the target, up to progress of the way
    public void SetLine(int index, Vector3 from, Vector3 to, float progress, Color color)
    {
        while (_lines.Count <= index)
        {
            _lines.Add(CreateLine());
        }

        LineRenderer line = _lines[index];
        line.gameObject.SetActive(true);
        line.startColor = color;
        line.endColor = color;
        float height = Vector3.Distance(from, to) * _arcHeightPerDistance;
        int segments = line.positionCount - 1;
        for (int point = 0; point <= segments; point++)
        {
            line.SetPosition(point, TargetLines.GetArcPoint(from, to, progress * point / segments, height));
        }
    }

    // Hides the lines from this index on
    public void HideFrom(int index)
    {
        for (int i = index; i < _lines.Count; i++)
        {
            _lines[i].gameObject.SetActive(false);
        }
    }

    LineRenderer CreateLine()
    {
        LineRenderer line = Instantiate(_linePrefab, transform);
        // From the entity to its target, wherever the entity is
        line.useWorldSpace = true;
        line.positionCount = _segments + 1;
        return line;
    }
}
