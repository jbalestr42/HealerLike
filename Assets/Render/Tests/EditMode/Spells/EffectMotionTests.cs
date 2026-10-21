using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class EffectMotionTests
{
    static EffectRecipe CreateRecipe(EffectMotionKind motion, EffectTempo tempo = EffectTempo.Once)
    {
        return new EffectRecipe { motion = motion, tempo = tempo, cycleSeconds = 1f };
    }

    static LookPart CreatePart(Primitive primitive, Vector3 position)
    {
        return new LookPart { primitive = primitive, position = position, size = Vector3.one };
    }

    [Test]
    public void Pose_Press_PressesDownMidCycleAndBackAtItsEnd()
    {
        EffectRecipe recipe = CreateRecipe(EffectMotionKind.Press);
        LookPart part = CreatePart(Primitive.Cone, Vector3.up);

        PartPose pressed = EffectMotion.Pose(recipe, part, 0, 0.5f, 0.5f, new MotionState());
        PartPose back = EffectMotion.Pose(recipe, part, 0, 1f, 1f, new MotionState());

        Assert.Less(pressed.position.y, part.position.y);
        Assert.AreEqual(part.position.y, back.position.y, 0.0001f);
    }

    [Test]
    public void Pose_BurstShard_FliesOutThenFalls()
    {
        EffectRecipe recipe = CreateRecipe(EffectMotionKind.Burst);
        LookPart shard = CreatePart(Primitive.Cone, Vector3.right * 0.2f);

        PartPose early = EffectMotion.Pose(recipe, shard, 0, 0.2f, 0.2f, new MotionState());
        PartPose late = EffectMotion.Pose(recipe, shard, 0, 0.9f, 0.9f, new MotionState());

        Assert.Greater(early.position.x, shard.position.x);
        Assert.Less(late.position.y, early.position.y);
        Assert.Less(late.scale.x, early.scale.x);
    }

    [Test]
    public void Pose_Fall_HangsFirstThenDropsTheFallDistance()
    {
        EffectRecipe recipe = CreateRecipe(EffectMotionKind.Fall);
        LookPart drop = CreatePart(Primitive.Sphere, Vector3.zero);
        MotionState state = new MotionState { fallDistance = 2f };

        PartPose hanging = EffectMotion.Pose(recipe, drop, 0, 0.1f, 0.1f, state);
        PartPose landed = EffectMotion.Pose(recipe, drop, 0, 1f / EffectMotion.FallPace, 1f, state);

        Assert.AreEqual(0f, hanging.position.y);
        Assert.AreEqual(-2f, landed.position.y, 0.0001f);
    }

    [Test]
    public void Pose_CloseWhileRemoving_OpensWithTheRemoval()
    {
        EffectRecipe recipe = CreateRecipe(EffectMotionKind.Close, EffectTempo.ForDuration);
        LookPart plate = CreatePart(Primitive.Sphere, Vector3.right);
        MotionState closed = new MotionState { isStatus = true };
        MotionState removed = new MotionState { isStatus = true, isRemoving = true, removal = 1f };

        PartPose held = EffectMotion.Pose(recipe, plate, 0, 0f, 5f, closed);
        PartPose open = EffectMotion.Pose(recipe, plate, 0, 0f, 5f, removed);

        Assert.AreEqual(plate.position.x, held.position.x, 0.0001f);
        Assert.Greater(open.position.x, held.position.x);
    }

    [Test]
    public void Curve_Ends_TouchTheEndpointsAndArchBetween()
    {
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.right * 2f;

        Assert.AreEqual(start, EffectMotion.Curve(start, end, 0f));
        Assert.AreEqual(end, EffectMotion.Curve(start, end, 1f));
        Assert.Greater(EffectMotion.Curve(start, end, 0.5f).y, 0f);
    }

    [Test]
    public void Thread_Middle_StaysOnTheGroundLine()
    {
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.forward * 3f;

        Vector3 middle = EffectMotion.Thread(start, end, 0.3f);

        Assert.AreEqual(0f, middle.y, 0.0001f);
        Assert.Less(Vector3.Distance(end, EffectMotion.Thread(start, end, 1f)), 0.0001f);
    }
}

}
