using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The phase under way of a unit's phased skill, above it (e.g. the Ravager charging its slam): its icon filling up and
// its name
public class SkillPhaseViewTests
{
    readonly List<Object> _objects = new List<Object>();
    readonly Entities.TestUnits _units = new Entities.TestUnits();
    SkillPhaseView _view;
    Image _background;
    Image _icon;
    Image _progress;
    TMP_Text _title;
    Sprite _sprite;
    Entity _unit;

    [SetUp]
    public void SetUp()
    {
        GameObject go = Track(new GameObject("Phase", typeof(RectTransform)));
        _background = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
        _background.transform.SetParent(go.transform, false);
        _icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        _icon.transform.SetParent(go.transform, false);
        _progress = new GameObject("Progress", typeof(RectTransform)).AddComponent<Image>();
        _progress.transform.SetParent(go.transform, false);
        _title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _title.transform.SetParent(go.transform, false);
        _view = go.AddComponent<SkillPhaseView>();
        TestHelpers.SetPrivateField(_view, "_background", _background);
        TestHelpers.SetPrivateField(_view, "_icon", _icon);
        TestHelpers.SetPrivateField(_view, "_progress", _progress);
        TestHelpers.SetPrivateField(_view, "_title", _title);

        Texture2D texture = Track(new Texture2D(2, 2));
        _sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero));
        _unit = _units.Create(100f, 100f, "Ravager");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    // A skill charging a slam for 2s, started (its steps created) or not
    ConfigurableSkill AddSlamSkill(bool started = true)
    {
        DurationSkillStepFactory wait = Track(ScriptableObject.CreateInstance<DurationSkillStepFactory>());
        wait.data = new DurationSkillStepData { duration = new FlatValue { data = new FlatValueData { value = 2f } } };
        PhaseSkillStepFactory slam = Track(ScriptableObject.CreateInstance<PhaseSkillStepFactory>());
        slam.data = new PhaseSkillStepData { title = "Slam", icon = _sprite, skillStepFactories = new List<ASkillStepFactory> { wait } };

        ConfigurableSkill skill = _unit.gameObject.AddComponent<ConfigurableSkill>();
        skill.data = new ConfigurableSkillData
        {
            onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
            skillStepFactories = new List<ASkillStepFactory> { slam },
        };
        if (started)
        {
            TestHelpers.InvokePrivate(skill, "Start");
        }
        _unit.skills.Add(skill);
        return skill;
    }

    [Test]
    public void UnitWithoutPhasedSkill_Hidden()
    {
        _view.Init(_unit);

        Assert.IsFalse(_view.isShown);
        Assert.IsFalse(_background.enabled);
        Assert.IsFalse(_icon.enabled);
        Assert.IsFalse(_progress.enabled);
        Assert.IsFalse(_title.enabled);
    }

    [Test]
    public void PhaseUnderWay_ShowsItsIconAndTitle_FilledByThePartPlayed()
    {
        ConfigurableSkill skill = AddSlamSkill();
        _view.Init(_unit);

        skill.Tick(skill.gameObject, 0.5f);
        _view.Refresh();

        Assert.IsTrue(_view.isShown);
        Assert.IsTrue(_background.enabled);
        Assert.IsTrue(_icon.enabled);
        Assert.AreSame(_sprite, _icon.sprite);
        Assert.AreEqual("Slam", _title.text);
        Assert.AreEqual(0.25f, _progress.fillAmount, 0.0001f);
    }

    // The HUD starts before the skill creates its steps: shown once it has them
    [Test]
    public void SkillStartedAfterTheView_ShownOnceStarted()
    {
        ConfigurableSkill skill = AddSlamSkill(started: false);
        _view.Init(_unit);
        Assert.IsFalse(_view.isShown);

        TestHelpers.InvokePrivate(skill, "Start");
        _view.Refresh();

        Assert.IsTrue(_view.isShown);
        Assert.AreEqual("Slam", _title.text);
    }

    // The unit gets its skills after its model and HUD: the skill is found once the unit has it
    [Test]
    public void SkillGivenToTheUnitAfterTheView_ShownOnceGiven()
    {
        _view.Init(_unit);
        _view.Refresh();
        Assert.IsFalse(_view.isShown);

        ConfigurableSkill skill = AddSlamSkill();
        skill.Tick(skill.gameObject, 0.5f);
        _view.Refresh();

        Assert.IsTrue(_view.isShown);
        Assert.AreEqual("Slam", _title.text);
        Assert.AreEqual(0.25f, _progress.fillAmount, 0.0001f);
    }

    [Test]
    public void FindPhasedSkill_TheFirstSkillWithPhases()
    {
        Assert.IsNull(SkillPhaseView.FindPhasedSkill(_unit));

        ConfigurableSkill skill = AddSlamSkill();

        Assert.AreSame(skill, SkillPhaseView.FindPhasedSkill(_unit));
        Assert.IsNull(SkillPhaseView.FindPhasedSkill(null));
    }
}

}
