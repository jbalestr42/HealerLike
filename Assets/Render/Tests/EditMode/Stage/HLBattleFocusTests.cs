using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Stage;
using HealerLike.Render.Environment;
public class HLBattleFocusTests
{
    [TestCase(.5625f,4,3,3)] [TestCase(1.777778f,4,3,3)]
    [TestCase(.5625f,18,5,12)] [TestCase(1.777778f,18,5,12)]
    [TestCase(.5625f,2,7,2)] [TestCase(1.777778f,2,7,2)]
    public void FitsEveryBodyCornerWithHudMargin(float aspect,float width,float height,float depth)
    {
        var b=new Bounds(new Vector3(7,2,-4),new Vector3(width,height,depth));
        var pose=HLBattleFocusBounds.Fit(b,52,40,aspect);
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
        {
            var world=b.center+Vector3.Scale(b.extents,new Vector3(x,y,z));
            var point=HLEnvironmentForeground.ToViewport(world,pose.position,pose.rotation,40,aspect);
            Assert.That(point.x,Is.InRange(.0799f,.9201f)); Assert.That(point.y,Is.InRange(.1999f,.8201f));
        }
    }
    [Test] public void SpreadWidensViewAndTranslationOnlyMovesPose()
    {
        var small=new Bounds(Vector3.zero,new Vector3(3,3,3));
        var wide=new Bounds(Vector3.zero,new Vector3(18,3,12));
        var a=HLBattleFocusBounds.Fit(small,52,40,.5625f); var b=HLBattleFocusBounds.Fit(wide,52,40,.5625f);
        Assert.That(b.position.magnitude,Is.GreaterThan(a.position.magnitude*2));
        var offset=new Vector3(13,4,-7); small.center+=offset;
        Assert.That(Vector3.Distance(HLBattleFocusBounds.Fit(small,52,40,.5625f).position,a.position+offset),Is.LessThan(.0001f));
    }
}
