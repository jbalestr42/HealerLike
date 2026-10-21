using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StageCaptureInputOwnershipTests
    {
        Scene _scene;
        StageInterfaceActions _actions;
        StandaloneInputModule _module;
        BaseInput _previous;
        StageTouchInput _touch;
        Touch[] _touches;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            GameObject input = new GameObject("Capture event fixture");
            SceneManager.MoveGameObjectToScene(input, _scene);
            input.AddComponent<EventSystem>();
            _module = input.AddComponent<StandaloneInputModule>();
            _previous = input.AddComponent<BaseInput>();
            _module.inputOverride = _previous;
            GameObject host = new GameObject("Capture UI fixture");
            host.SetActive(false);
            SceneManager.MoveGameObjectToScene(host, _scene);
            ToolkitGameUI ui = host.AddComponent<ToolkitGameUI>();
            _touch = host.AddComponent<StageTouchInput>();
            _touches = new[] { new Touch { fingerId = 8, phase = TouchPhase.Stationary } };
            _touch.captureTouches = _touches;
            _actions = new StageInterfaceActions { ui = ui };
        }

        [TearDown]
        public void TearDown()
        {
            if (_actions != null)
            {
                _actions.Dispose();
            }

            if (_scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_scene);
            }
        }

        [Test]
        public void ReconfigureThenDispose_RestoresBorrowedInputAndTouches()
        {
            _actions.ConfigureLegacyInput();
            StageCaptureInput first = _actions.captureInput;
            _actions.ConfigureLegacyInput();
            Assert.IsFalse(first);
            Assert.AreSame(_actions.captureInput, _module.inputOverride);
            Assert.AreEqual(1, _actions.ui.GetComponents<StageCaptureInput>().Length);
            _touch.captureTouches = new Touch[0];
            _actions.Dispose();
            _actions.Dispose();
            Assert.AreSame(_previous, _module.inputOverride);
            Assert.AreSame(_touches, _touch.captureTouches);
        }

        [Test]
        public void Dispose_DoesNotReplaceAnotherOwnersInputOverride()
        {
            _actions.ConfigureLegacyInput();
            BaseInput replacement = _module.gameObject.AddComponent<BaseInput>();
            _module.inputOverride = replacement;
            _actions.Dispose();
            Assert.AreSame(replacement, _module.inputOverride);
        }

        [Test]
        public void HeldTouch_UsesCurrentCaptureAndRestoresBorrowedSamples()
        {
            _actions.ConfigureLegacyInput();
            Touch[] samples = new[] { new Touch { fingerId = 5 } };
            _actions.captureInput.samples = samples;
            using (StagePresentationTouch finger = new StagePresentationTouch(_actions))
            {
                Assert.IsTrue(finger.Frame(TouchPhase.Began, Vector2.right).MoveNext());
                Assert.AreSame(_actions.captureInput.samples, _touch.captureTouches);
            }

            Assert.AreSame(_touches, _touch.captureTouches);
            Assert.AreSame(samples, _actions.captureInput.samples);
        }
    }
}
