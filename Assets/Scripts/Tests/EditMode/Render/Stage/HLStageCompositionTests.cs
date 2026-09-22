using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    public class HLStageCompositionTests
    {
        [TestCase(9f/16f,52f)] [TestCase(16f/9f,46f)]
        public void EveryPlayableCornerFitsOutsideHudAndFog(float aspect,float pitch)
        {
            var go=new GameObject("composition camera");
            try {
                var camera=go.AddComponent<Camera>(); camera.fieldOfView=40; camera.aspect=aspect;
                var board=new Bounds(new Vector3(0,.505f,0),new Vector3(16,0,16));
                var pose=HLStageCalibration.PlayableFrame(board,pitch,40,aspect,.48f);
                go.transform.SetPositionAndRotation(pose.position,pose.rotation);
                var fog=HLStageCalibration.BackgroundFog(pose.position,board);
                for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) {
                    var point=board.center+Vector3.Scale(board.extents,new Vector3(x,0,z));
                    var v=camera.WorldToViewportPoint(point);
                    Assert.That(v.x,Is.InRange(.014f,.986f)); Assert.That(v.y,Is.InRange(.119f,.841f));
                    Assert.That(Vector3.Distance(pose.position,point),Is.LessThan(fog.x));
                }
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void RenderDestinationsStayInsideTheRenderScenePair()
        {
            Assert.AreEqual("Assets/Render/Stage/HLRenderLook.unity",HLStageSceneLoader.ScenePath);
            Assert.AreEqual("Assets/Render/Stage/HLStageMenu.unity",HLStageSceneLoader.MenuPath);
            Assert.IsTrue(HLStageSceneLoader.UseEditorPath(true,-1));
            Assert.IsFalse(HLStageSceneLoader.UseEditorPath(false,-1));
        }
    }
}
