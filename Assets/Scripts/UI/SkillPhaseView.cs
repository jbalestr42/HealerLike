using UnityEngine;
using UnityEngine.UI;

// The phase under way of a unit's phased skill (e.g. the Ravager charging its slam): its icon filling up as it plays,
// and its name. Hidden for a unit without any phase
public class SkillPhaseView : MonoBehaviour
{
    // Optional: behind the icon, shown with it
    [SerializeField] Image _background;
    [SerializeField] Image _icon;
    // Filled image over the icon, showing the part of the phase played
    [SerializeField] Image _progress;
    [SerializeField] TMPro.TMP_Text _title;

    Entity _entity;
    // Looked for until found: the HUD starts before the unit gets its skills
    IPhasedSkill _skill;
    // False while the unit has no phase to show
    public bool isShown { get; private set; }

    public void Init(Entity entity)
    {
        _entity = entity;
        _skill = null;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    // The first skill of the unit going through phases, null when none does
    public static IPhasedSkill FindPhasedSkill(Entity entity)
    {
        if (entity == null)
        {
            return null;
        }
        foreach (ASkill skill in entity.skills)
        {
            if (skill is IPhasedSkill phasedSkill)
            {
                return phasedSkill;
            }
        }
        return null;
    }

    // Shown or hidden by its parts, the view itself staying active: the steps of a skill are only created once it
    // starts, after the HUD
    public void Refresh()
    {
        _skill ??= FindPhasedSkill(_entity);
        PhaseSkillStepData phase = null;
        float progress = 0f;
        bool hasPhase = _skill != null && _skill.TryGetPhase(out phase, out progress);
        isShown = hasPhase;

        if (_background != null)
        {
            _background.enabled = hasPhase;
        }
        if (_icon != null)
        {
            _icon.enabled = hasPhase && phase.icon != null;
            _icon.sprite = hasPhase ? phase.icon : null;
        }
        if (_progress != null)
        {
            _progress.enabled = hasPhase;
            _progress.fillAmount = progress;
        }
        if (_title != null)
        {
            _title.enabled = hasPhase;
            _title.text = hasPhase ? phase.title : string.Empty;
        }
    }
}
