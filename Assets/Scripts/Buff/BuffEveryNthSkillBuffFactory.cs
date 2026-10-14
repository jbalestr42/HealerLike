using System;
using UnityEngine;
using UnityEngine.Events;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/BuffEveryNthSkillBuff")]
public class BuffEveryNthSkillBuffFactory : BuffFactory<BuffEveryNthSkillBuff, BuffEveryNthSkillBuffData> { }

[Serializable]
public class BuffEveryNthSkillBuffData
{
    // Every this many skills used by the character, the last one is used with the buff (e.g. 5: every fifth spell)
    [Min(2)]
    public int count = 5;
    // Given to the character once the skills before the last one are used, taken back by the next skill (e.g. the
    // next spell free)
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Held by the character (e.g. the Rosary, every fifth spell free): the skills used are counted, the buff given once
// count - 1 of them are used, until the next one, then counting again. The count is shown on the icon of the item
public class BuffEveryNthSkillBuff : ABuff<BuffEveryNthSkillBuffData>, ICounterBuff
{
    Character _owner;
    int _used = 0;
    bool _isGiven = false;
    readonly UnityEvent _onCounterChanged = new UnityEvent();

    // Skills used since the last one used with the buff
    public int used => _used;
    public bool isGiven => _isGiven;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Character>();
        if (_owner != null)
        {
            _owner.OnSkillUsed.AddListener(OnSkillUsed);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner == null)
        {
            return;
        }
        _owner.OnSkillUsed.RemoveListener(OnSkillUsed);
        if (_isGiven)
        {
            TakeBack();
        }
    }

    void OnSkillUsed(CharacterSkillData skill)
    {
        // That skill was used with the buff
        if (_isGiven)
        {
            TakeBack();
            _used = 0;
            _onCounterChanged.Invoke();
            return;
        }

        _used++;
        _onCounterChanged.Invoke();
        if (_used >= data.count - 1 && data.buffHandlerFactory != null)
        {
            _isGiven = true;
            _owner.GetComponent<BuffManager>().AddHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
        }
    }

    void TakeBack()
    {
        _isGiven = false;
        _owner.GetComponent<BuffManager>().RemoveHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }

    #region ICounterBuff

    // The skills used toward the next one with the buff (e.g. 4 of 5: the next spell is free)
    public int counter => _used;
    public int counterMax => data.count;
    public UnityEvent OnCounterChanged => _onCounterChanged;

    #endregion
}
