using NUnit.Framework;

namespace Skills
{

public class DurationSkillStepTests
{
    static DurationSkillStep CreateWait(float seconds)
    {
        DurationSkillStep wait = new DurationSkillStep
        {
            data = new DurationSkillStepData { duration = new FlatValue { data = new FlatValueData { value = seconds } } },
        };
        wait.Init();
        return wait;
    }

    [Test]
    public void Update_BeforeTheDuration_IsNotDone_AndSpendsTheWholeUpdate()
    {
        DurationSkillStep wait = CreateWait(0.5f);
        float deltaTime = 0.4f;

        Assert.IsFalse(wait.Update(null, ref deltaTime));
        Assert.AreEqual(0f, deltaTime);
    }

    [Test]
    public void Update_EndingTheDuration_LeavesTheTimeBeyondIt()
    {
        DurationSkillStep wait = CreateWait(0.5f);
        float first = 0.4f;
        wait.Update(null, ref first);

        float deltaTime = 0.3f;
        Assert.IsTrue(wait.Update(null, ref deltaTime));
        Assert.AreEqual(0.2f, deltaTime, 0.0001f);
    }

    [Test]
    public void Reset_RestartsTheDuration()
    {
        DurationSkillStep wait = CreateWait(0.5f);
        float first = 0.4f;
        wait.Update(null, ref first);

        wait.Reset();

        float deltaTime = 0.4f;
        Assert.IsFalse(wait.Update(null, ref deltaTime));
    }
}

}
