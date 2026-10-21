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

        Canvas CreateEventCanvas(out GraphicRaycaster raycaster)
        {
            Canvas eventCanvas = CreateCanvas(_go.transform, RenderMode.ScreenSpaceOverlay);
            raycaster = eventCanvas.gameObject.AddComponent<GraphicRaycaster>();
            TestHelpers.WithLoggingDisabled(() => eventCanvas.gameObject.AddComponent<EventView>());
            return eventCanvas;
        }

        [Test]
        public void Hide_EventViewCanvas_KeepsItAndItsRaycasterEnabled()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster eventRaycaster);

            _legacyCanvases.Hide(_context);

            Assert.IsTrue(eventCanvas.enabled, "His screen stays until ToolkitEventPanel has a card to press.");
            Assert.IsTrue(eventRaycaster.enabled);
            Assert.IsFalse(_screenCanvas.enabled, "The other legacy screens are still hidden.");
        }

        [Test]
        public void SetEventScreenVisible_False_HidesHisCanvasAndRaycasterOnly()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster eventRaycaster);
            _legacyCanvases.Hide(_context);

            _legacyCanvases.SetEventScreenVisible(false);

            Assert.IsFalse(eventCanvas.enabled, "ToolkitEventPanel draws the choices in its place.");
            Assert.IsFalse(eventRaycaster.enabled);
            Assert.IsTrue(_worldCanvas.enabled);
        }

        [Test]
        public void SetEventScreenVisible_True_BringsHisCanvasAndRaycasterBackAsTheSceneHadThem()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster eventRaycaster);
            _legacyCanvases.Hide(_context);
            _legacyCanvases.SetEventScreenVisible(false);

            _legacyCanvases.SetEventScreenVisible(true);

            Assert.IsTrue(eventCanvas.enabled, "The fallback: with no Toolkit card his screen is the way out of the room.");
            Assert.IsTrue(eventRaycaster.enabled);
            Assert.IsFalse(_screenCanvas.enabled, "Only the event screen comes back.");
        }

        [Test]
        public void SetEventScreenVisible_CanvasThatWasOffInTheScene_StaysOff()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster _);
            eventCanvas.enabled = false;
            _legacyCanvases.Hide(_context);

            _legacyCanvases.SetEventScreenVisible(true);

            Assert.IsFalse(eventCanvas.enabled);
        }

        [Test]
        public void SetEventScreenVisible_BeforeHide_ChangesNothing()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster eventRaycaster);

            _legacyCanvases.SetEventScreenVisible(false);

            Assert.IsTrue(eventCanvas.enabled);
            Assert.IsTrue(eventRaycaster.enabled);
        }

        [Test]
        public void Restore_AfterThePanelHidHisCanvas_PutsItBackAndALateCallCannotHideItAgain()
        {
            Canvas eventCanvas = CreateEventCanvas(out GraphicRaycaster eventRaycaster);
            _legacyCanvases.Hide(_context);
            _legacyCanvases.SetEventScreenVisible(false);

            _legacyCanvases.Restore();
            _legacyCanvases.SetEventScreenVisible(false);

            Assert.IsTrue(eventCanvas.enabled, "Restore forgets the event canvas, a late call must not hide it again.");
            Assert.IsTrue(eventRaycaster.enabled);
        }

        [Test]
        public void SilenceMenus_LegacyMenuController_IsDisabledAndStaysOffAfterRestore()
        {
            MainMenu menu = _go.AddComponent<MainMenu>();
            ToolkitLegacyCanvases.SilenceMenus();
            Assert.IsFalse(menu.enabled);
            _legacyCanvases.Hide(_context);
            _legacyCanvases.Restore();
            Assert.IsFalse(menu.enabled);
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
