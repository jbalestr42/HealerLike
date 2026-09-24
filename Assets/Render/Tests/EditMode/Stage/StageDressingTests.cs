using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{

public class StageDressingTests
{
    StageSceneFixture _scene;
    StageDressing _dressing;

    [SetUp]
    public void SetUp()
    {
        _scene = new StageSceneFixture();
        _scene.Create();
        _dressing = _scene.manager.GetComponent<StageDressing>();
    }

    [TearDown]
    public void TearDown()
    {
        _scene.Destroy();
    }

    [Test]
    public void SetLighting_GameScene_LightsWithTheKeyLightAlone()
    {
        Light keyLight = _scene.manager.keyLight.keyLight;

        _dressing.SetLighting(_scene.gameGo.scene, keyLight);

        Assert.IsFalse(_scene.sunGo.GetComponent<Light>().enabled);
        Assert.AreSame(keyLight, RenderSettings.sun);
        Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
    }

    [Test]
    public void SetBoard_GameScene_DressesTheGroundAndHidesTheDecoration()
    {
        Bounds board = _dressing.SetBoard(_scene.gameGo.scene, _scene.player.grid);

        Assert.AreEqual("StageGround", _scene.ground.sharedMaterial.name);
        Assert.AreSame(_scene.ground, _dressing.boardGround);
        Assert.IsFalse(_scene.decorationGo.GetComponent<Renderer>().enabled);
        Assert.IsFalse(_scene.farGroundGo.GetComponent<Renderer>().enabled);
        Assert.IsTrue(_scene.ground.enabled); // the board ground shares the name and stays
        Assert.AreEqual(16f, board.size.x);
        Assert.AreEqual(_scene.ground.bounds.max.y, board.center.y);
    }

    [Test]
    public void RestorePipeline_Swapped_GivesThePreviousPipelineBack()
    {
        RenderPipelineAsset previous = QualitySettings.renderPipeline;
        RenderPipelineAsset stagePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Render/Stage/Settings/StagePipeline.asset");
        _dressing.SwapPipeline();
        Assert.AreSame(stagePipeline, QualitySettings.renderPipeline);

        _dressing.RestorePipeline();

        Assert.AreSame(previous, QualitySettings.renderPipeline);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Frame_Orientation_PutsTheCameraOnItsOverview(bool isLandscape)
    {
        Bounds board = _dressing.SetBoard(_scene.gameGo.scene, _scene.player.grid);
        _dressing.FrameBoard(board);
        Camera gameCamera = _scene.cameraGo.GetComponent<Camera>();

        _dressing.Frame(gameCamera, isLandscape);

        Pose pose = _dressing.OverviewPose(isLandscape);
        Assert.AreEqual(pose.position, gameCamera.transform.position);
        Assert.AreEqual(isLandscape, gameCamera.aspect > 1f);
    }
}

}
