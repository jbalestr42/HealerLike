using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

public class ToolkitTimeControlsTests
{
    float _originalSpeed;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitTimeControls _controls;

    [SetUp]
    public void SetUp()
    {
        _originalSpeed = Time.timeScale;
        Time.timeScale = 1f;
        _context = new ToolkitGameContext();
        VisualElement root = new VisualElement();
        Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree(root);
        _view = new ToolkitGameView(root);
        _controls = new ToolkitTimeControls();
        _controls.Init(null, _context, _view);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = _originalSpeed;
        _view.Release();
    }

    [Test]
    public void SetSpeed_WhilePaused_StaysPausedAndResumesAtSelectedSpeed()
    {
        _controls.TogglePause();

        _controls.SetSpeed(2f);

        Assert.IsTrue(_context.isPaused);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(_view.root.Q("speed-fast-button").ClassListContains("is-selected"));
        Assert.IsFalse(_view.root.Q("speed-normal-button").ClassListContains("is-selected"));

        _controls.TogglePause();

        Assert.IsFalse(_context.isPaused);
        Assert.AreEqual(2f, Time.timeScale);
    }

    [Test]
    public void SetSpeed_WhileRunning_ChangesSpeedImmediately()
    {
        _controls.SetSpeed(0.5f);

        Assert.IsFalse(_context.isPaused);
        Assert.AreEqual(0.5f, Time.timeScale);
    }

    [Test]
    public void ResetSpeed_AfterPausedSelection_StartsNextSceneAtNormalSpeed()
    {
        _controls.TogglePause();
        _controls.SetSpeed(2f);

        _controls.ResetSpeed();

        Assert.IsFalse(_context.isPaused);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsTrue(_view.root.Q("speed-normal-button").ClassListContains("is-selected"));
    }
}
}
