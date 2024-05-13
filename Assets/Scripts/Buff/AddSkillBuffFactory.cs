using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/AddSkillBuff")]
public class AddSkillBuffFactory : BuffFactory<AddSkillBuff, AddSkillBuffData> {}

[Serializable]
public class AddSkillBuffData
{
    [CreateDataButton]
    public ASkillFactory skillFactory;
}

public class AddSkillBuff : ABuff<AddSkillBuffData>, IStackableBuff
{
    ASkill _skillInstance;

    public override void Add(GameObject source, GameObject target)
    {
        _skillInstance = data.skillFactory.AddSkill(target);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        GameObject.Destroy(_skillInstance);
    }

    public override bool isStackable => _skillInstance is IStackableBuff;

    public void Stack(GameObject source, GameObject target)
    {
        IStackableBuff stackableSkill = _skillInstance as IStackableBuff;
        stackableSkill.Stack(source, target);
    }

    public void Unstack(GameObject source, GameObject target)
    {
        IStackableBuff stackableSkill = _skillInstance as IStackableBuff;
        stackableSkill.Unstack(source, target);
    }
}