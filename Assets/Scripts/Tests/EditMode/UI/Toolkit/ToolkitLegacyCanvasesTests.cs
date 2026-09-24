using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Toolkit
{

public class ToolkitLegacyCanvasesTests
{
    GameObject _go;
    Canvas _screenCanvas;
    GraphicRaycaster _raycaster;
    Canvas _worldCanvas;
    ToolkitGameContext _context;
    ToolkitLegacyCanvases _legacyCanvases;

    static Canvas CreateCanvas(Transform parent, RenderMode renderMode)
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(parent);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = renderMode;
        return canvas;
    }

    [SetUp]
    public void SetUp()
    {
        // Standalone component, never going through UIManager.instance
        _go = new GameObject("UIManager");
        _context = new ToolkitGameContext();
        _context.ui = _go.AddComponent<UIManager>();
        _screenCanvas = CreateCanvas(_go.transform, RenderMode.ScreenSpaceOverlay);
        _raycaster = _screenCanvas.gameObject.AddComponent<GraphicRaycaster>();
        _worldCanvas = CreateCanvas(_go.transform, RenderMode.WorldSpace);
        _legacyCanvases = new ToolkitLegacyCanvases();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Hide_ScreenCanvas_DisablesCanvasAndRaycaster()
    {
        _legacyCanvases.Hide(_context);

        Assert.IsFalse(_screenCanvas.enabled);
        Assert.IsFalse(_raycaster.enabled);
    }

    [Test]
    public void Hide_WorldSpaceCanvas_KeepsItEnabled()
    {
        _legacyCanvases.Hide(_context);

        Assert.IsTrue(_worldCanvas.enabled);
    }

    [Test]
    public void Restore_AfterHide_RestoresPreviousState()
    {
        _raycaster.enabled = false;
        _legacyCanvases.Hide(_context);

        _legacyCanvases.Restore();

        Assert.IsTrue(_screenCanvas.enabled);
        Assert.IsFalse(_raycaster.enabled); // it was already off before Hide
    }
}
}
