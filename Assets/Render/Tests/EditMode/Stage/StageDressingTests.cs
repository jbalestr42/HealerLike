using System.Collections.Generic;
using System.Reflection;
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
    [Test]
    public void FocusAndSafetyWidening_FrameVisibleCombatantsWithoutHiddenCharacterOrigin()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        GameObject healer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        healer.transform.SetParent(_scene.gameGo.transform);
        healer.transform.position = new Vector3(-30f, 1f, 0f);
        healer.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
        healer.GetComponent<Renderer>().enabled = false;
        // Character.Reset runs during AddComponent before its gameplay Init has wired the buff manager.
        TestHelpers.WithLoggingDisabled(() => _scene.player.character = healer.AddComponent<Character>());
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.transform.SetParent(_scene.gameGo.transform);
        enemy.transform.position = new Vector3(5f, 1f, 0f);
        enemy.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
        GameObject ally = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ally.transform.SetParent(_scene.gameGo.transform);
        ally.transform.position = new Vector3(3f, 1f, 0f);
        ally.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
        TestHelpers.SetPrivateField(_scene.entityManager, "_entities",
            new Dictionary<Entity.EntityType, List<GameObject>>
            {
                { Entity.EntityType.Player, new List<GameObject> { ally } },
                { Entity.EntityType.Computer, new List<GameObject> { enemy } }
            });
        BattleFocus focus = _scene.manager.GetComponentInChildren<BattleFocus>(true);
        Assert.IsNotNull(focus, "The render manager carries focus on its nested controls prefab.");
        focus.Focus();
        Pose target = (Pose)typeof(BattleFocus).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(focus);
        Assert.That(Quaternion.Angle(target.rotation, _scene.manager.overviewPose.rotation), Is.LessThan(0.01f));
        Camera camera = _scene.manager.gameCamera;
        // Force the out-of-frame safety path before easing has reached the target.
        camera.transform.SetPositionAndRotation(target.position + Vector3.up * 100f, target.rotation);
        focus.Tick();
        Assert.That(Quaternion.Angle(camera.transform.rotation, target.rotation), Is.LessThan(0.01f));
        Assert.IsTrue(focus.AreAllBodiesVisible());
        Vector3 allyBase = camera.WorldToViewportPoint(Vector3.right * 3f);
        Vector3 enemyBase = camera.WorldToViewportPoint(Vector3.right * 5f);
        Vector3 combatCentre = camera.WorldToViewportPoint(new Vector3(4f, 1f, 0f));
        Assert.That(combatCentre.y, Is.EqualTo(0.44f).Within(0.001f));
        Assert.That(allyBase.y, Is.InRange(0.16f, 0.38f));
        Assert.That(allyBase.y, Is.LessThan(enemyBase.y));
        Assert.That(allyBase.x, Is.EqualTo(enemyBase.x).Within(0.001f));
        Assert.That(healer.transform.position.x, Is.EqualTo(-30f));
        Assert.That(Vector3.Distance(camera.transform.position, new Vector3(4f, 1f, 0f)), Is.LessThan(20f));
    }

}

}
