using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // UI events activate the real presenters. World taps use the same gesture path as the mobile adapter.
    public class StageInterfaceRun : AStageRun
    {
        readonly StageInterfaceOutput _output = new StageInterfaceOutput();
        readonly StageInterfaceActions _actions = new StageInterfaceActions();
        StageGameViewSize _size;
        InteractionManager _interaction;
        protected override bool shouldStartGame { get { return false; } }

        protected override void OnFailed(System.Exception error)
        {
            _output.Fail(error.Message);
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            try
            {
                _output.Check(!string.IsNullOrWhiteSpace(_output.manifest.revision)
                    && _output.manifest.revision != "unspecified", "Capture source revision recorded");
                _actions.ui = Object.FindAnyObjectByType<ToolkitGameUI>();
                _actions.ConfigureLegacyInput();
                _interaction = Object.FindAnyObjectByType<InteractionManager>();
                yield return Resize(1080, 1920);
                _output.Check(_actions.ui != null, "RenderStage attached Toolkit interface");
                _output.Check(Object.FindObjectsByType<ToolkitGameUI>(FindObjectsSortMode.None).Length == 1,
                    "Exactly one Toolkit host");
                yield return Capture("01-journey");
                Random.InitState(271828);
                yield return _actions.PointerTap("start-button");
                yield return Wait(1f);
                _output.Check(_actions.legacyModuleReadTouches,
                    "Selected StandaloneInputModule consumed held touch samples for Begin journey");
                _output.Check(LegacyUiReader.GameState(Object.FindAnyObjectByType<GameManager>()) == GameManager.GameState.Running,
                    "Toolkit begin journey started real gameplay");
                yield return _actions.PointerTap("party-button");
                yield return Wait(0.3f);
                yield return Capture("02-party");
                List<Button> cards = _actions.Cards("party-list");
                _output.Check(cards.Count > 0, "Party has deploy choices");
                yield return _actions.SelectCard(cards[0].parent.Q<Button>("card-info"));
                yield return Wait(0.3f);
                _output.Check(_interaction.GetInteraction() == null, "Info opens details without deploying");
                yield return Capture("03-details");
                _actions.Submit("detail-close-button");
                yield return Wait(0.2f);
                _actions.Submit("party-button");
                yield return Wait(0.2f);
                yield return _actions.SelectCardByTouch(_actions.Cards("party-list")[0]);
                yield return Wait(0.3f);
                _output.Check(_interaction.GetInteraction() != null, "Toolkit party card begins deployment");
                yield return Capture("04-targeting");
                yield return _actions.PointerTap("cancel-button");
                yield return Wait(0.2f);
                _output.Check(_interaction.GetInteraction() == null, "Touch cancel ends deployment");
                yield return Deploy(0, Vector3.left * 2f);
                yield return Deploy(1, Vector3.left * 3f + Vector3.back);
                _output.manifest.checks.Add("Deployment uses multi-frame StandaloneInputModule and StageTouchInput.Update; Android OS input is not covered by this capture");
                _actions.Submit("inventory-button");
                yield return Wait(0.3f);
                _output.Check(StageInterfaceOutput.IsVisible(_actions.root.Q("inventory-panel")), "Inventory opens");
                yield return Capture("05-inventory");
                yield return Resize(1170, 2532);
                _actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
                yield return Wait(0.4f);
                yield return Capture("05b-simulated-notch-inventory");
                _actions.ui.safeAreaProvider = null;
                yield return Resize(1080, 1920);
                _actions.Submit("inventory-close-button");
                yield return Wait(0.3f);
                _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("inventory-panel")), "Inventory closes");
                yield return GestureExclusion();
                yield return Deploy(1, Vector3.left * 2f + Vector3.forward * 2f);
                yield return LandscapeControls();
                _actions.Submit("wave-button");
                yield return Wait(2f);
                _output.Check(! _actions.root.Q<Button>("wave-button").enabledSelf, "Toolkit begins encounter");
                yield return Capture("06-battle");
                _actions.Submit("pause-button");
                yield return Wait(0.3f);
                _output.Check(Time.timeScale == 0f, "Pause freezes gameplay");
                yield return Capture("07-pause");
                _actions.Submit("resume-button");
                yield return Wait(0.2f);
                _output.Check(Time.timeScale > 0f, "Resume restores gameplay");
                yield return SpellTap();
                yield return Resize(1170, 2532);
                yield return Capture("08-tall-phone");
                _actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
                _actions.Submit("pause-button");
                yield return Wait(0.4f);
                yield return Capture("08b-simulated-notch-pause");
                _actions.Submit("resume-button");
                _actions.ui.safeAreaProvider = null;
                yield return Wait(0.2f);
                yield return Resize(844, 390);
                yield return Capture("09-phone-landscape");
                yield return Resize(1080, 1920);
                yield return Reward();
                yield return Navigation();
                _output.Check(_actions.scrollActions > 0, "Scrolled cards below the fold before activating reachable controls");
                passed = true;
            }
            finally
            {
                if (_size != null)
                {
                    _size.Dispose();
                }
                _output.Write(passed);
                StagePlay.Finish(this, passed);
            }
        }

        IEnumerator Resize(int width, int height)
        {
            if (_size != null)
            {
                _size.Dispose();
            }
            _size = new StageGameViewSize(width, height);
            yield return Wait(0.8f);
            _output.Check(Screen.width == width && Screen.height == height, "Game view resize " + width + "x" + height);
            _output.Check(Mathf.Abs(_manager.gameCamera.aspect - (float)width / height) < 0.01f,
                "Camera follows actual aspect " + width + "x" + height);
        }

        IEnumerator Capture(string name)
        {
            yield return _output.Capture(_actions.ui, name);
        }

        IEnumerator Deploy(int index, Vector3 offset)
        {
            yield return _actions.PointerTap("party-button");
            yield return Wait(0.2f);
            List<Button> cards = _actions.Cards("party-list").FindAll(button =>
                button.enabledInHierarchy && button.Q<Label>("card-status").text == "Deploy");
            _output.Check(cards.Count > 0, "Deployable party choice remains");
            yield return _actions.SelectCardByTouch(cards[Mathf.Min(index, cards.Count - 1)]);
            yield return Wait(0.2f);
            _output.Check(_interaction.GetInteraction() is EntityGridInteraction,
                "Multi-frame Toolkit touch selects deployment card " + index);
            int before = _manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            Vector3 point = _manager.player.grid.GetNearestWalkablePosition(offset);
            Vector2 screen = _manager.gameCamera.WorldToScreenPoint(point);
            yield return _actions.TouchGesture(screen);
            yield return Wait(0.4f);
            _output.Check(_manager.entityManager.GetEntities(Entity.EntityType.Player).Count > before,
                "First world touch deploys ally " + index);
            _output.Check(_interaction.enabled, "Legacy input restored after multi-frame world touch " + index);
        }

        IEnumerator GestureExclusion()
        {
            _actions.Submit("party-button");
            yield return Wait(0.2f);
            Button card = _actions.Cards("party-list")[0];
            yield return _actions.BringIntoView(card);
            Vector2 start = StageInterfaceActions.ScreenPoint(card);
            Vector2 end = _manager.gameCamera.WorldToScreenPoint(_manager.board.center);
            _actions.touch.ProcessTouch(3, TouchPhase.Began, start);
            _actions.Submit(card);
            _actions.touch.ProcessTouch(3, TouchPhase.Moved, end);
            _actions.touch.ProcessTouch(3, TouchPhase.Ended, end);
            _output.Check(_interaction.GetInteraction() != null, "Gesture beginning over UI cannot deploy on release over board");
            yield return Wait(0.2f);
            yield return _actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
        }

        IEnumerator SpellTap()
        {
            foreach (Button card in _actions.Cards("spell-list"))
            {
                if (!card.enabledInHierarchy)
                {
                    continue;
                }
                yield return _actions.SelectCard(card);
                yield return Wait(0.2f);
                if (_interaction.GetInteraction() != null)
                {
                    _output.Check(StageInterfaceOutput.IsVisible(_actions.root.Q("cancel-button")), "Spell targeting has touch cancel");
                    yield return _actions.PointerTap("cancel-button");
                    yield return Wait(0.2f);
                    _output.Check(_interaction.GetInteraction() == null, "Touch cancel ends spell targeting");
                    yield return _actions.SelectCard(card);
                    yield return Wait(0.2f);
                    AInteraction spell = _interaction.GetInteraction();
                    foreach (Entity entity in _manager.entityManager.GetComponentsInChildren<Entity>())
                    {
                        if (spell == null || !spell.IsValidTarget(entity.gameObject))
                        {
                            continue;
                        }
                        Vector2 point = _manager.gameCamera.WorldToScreenPoint(RenderTargets.Point(entity.gameObject));
                        _actions.WorldTap(point);
                        if (_interaction.GetInteraction() == null)
                        {
                            _output.Check(true, "Toolkit spell casts on first valid world tap");
                            yield break;
                        }
                    }
                    _output.Check(false, "Toolkit spell reaches a valid target");
                }
            }
            _output.Check(false, "A usable targeting spell is available");
        }

        IEnumerator Reward()
        {
            // The first wave runs normally. No direct reward or game-over event is injected.
            float deadline = Time.realtimeSinceStartup + 100f;
            while (!StageInterfaceOutput.IsVisible(_actions.root.Q("upgrade-panel")))
            {
                if (StageInterfaceOutput.IsVisible(_actions.root.Q("gameover-panel")))
                {
                    _output.Check(false, "Party survives first encounter");
                }
                if (Time.realtimeSinceStartup > deadline)
                {
                    _output.Check(false, "First encounter reaches reward in 100 seconds");
                }
                yield return Wait(0.5f);
            }
            yield return Capture("10-reward");
            List<Button> cards = _actions.Cards("upgrade-list");
            _output.Check(cards.Count > 0, "Real wave reward offers choices");
            Button equipment = cards.Find(button => button.Q<Label>("card-status").text.StartsWith("Party equipment"));
            yield return _actions.SelectCard(equipment != null ? equipment : cards[0]);
            yield return Wait(1f);
            _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("upgrade-panel")), "Toolkit reward advances to next preparation");
            if (equipment != null)
            {
                _actions.Submit("inventory-button");
                yield return Wait(0.3f);
                yield return _actions.SelectCard(_actions.Cards("inventory-list")[0]);
                DropdownField target = _actions.root.Q<DropdownField>("inventory-target");
                target.value = target.choices[0];
                yield return Wait(0.3f);
                Entity ally = _manager.entityManager.GetEntities(Entity.EntityType.Player)[0].GetComponent<Entity>();
                int previous = ally.inventoryHandler.items.Count;
                _actions.Submit("inventory-equip-button");
                yield return Wait(0.3f);
                _output.Check(ally.inventoryHandler.items.Count > previous, "Toolkit equipment action transfers earned reward to ally");
                yield return Capture("10b-equipped-reward");
                _actions.Submit("inventory-close-button");
                yield return Wait(0.2f);
            }
            else
            {
                _output.manifest.checks.Add("Equipment transfer not exercised: reward offered healer upgrades only");
            }
            _output.manifest.checks.Add("Wave choice screen not exercised: current Ascension loads waves directly");
        }

        IEnumerator LandscapeControls()
        {
            yield return Resize(844, 390);
            _actions.Submit("party-button");
            yield return Wait(0.3f);
            yield return Capture("09b-landscape-party");
            _actions.Submit("party-close-button");
            yield return Wait(0.2f);
            _actions.Submit("detail-button");
            yield return Wait(0.3f);
            yield return Capture("09c-landscape-details");
            _actions.Submit("detail-close-button");
            yield return Wait(0.2f);
            _actions.Submit("pause-button");
            yield return Wait(0.3f);
            yield return Capture("09d-landscape-pause");
            _actions.Submit("resume-button");
            yield return Resize(1080, 1920);
        }

        IEnumerator Navigation()
        {
            RenderManager original = _manager;
            _actions.Submit("pause-button");
            yield return Wait(0.2f);
            _actions.Submit("menu-button");
            yield return Wait(1f);
            _actions.ui = Object.FindAnyObjectByType<ToolkitGameUI>();
            _actions.ConfigureLegacyInput();
            _output.Check(SceneManager.GetActiveScene().path == StageInterface.MenuPath, "Pause menu returns to Toolkit menu");
            yield return Capture("11-menu");
            yield return _actions.PointerTap("start-button");
            yield return Wait(1.5f);
            _actions.ui = Object.FindAnyObjectByType<ToolkitGameUI>();
            _actions.ConfigureLegacyInput();
            _output.Check(Object.FindAnyObjectByType<RenderManager>() == original && original.entityManager != null,
                "New expedition reuses attached RenderManager");
            _output.Check(Object.FindObjectsByType<ToolkitGameUI>(FindObjectsSortMode.None).Length == 1,
                "New expedition has exactly one Toolkit host");
            yield return _actions.PointerTap("start-button");
            yield return Wait(0.8f);
            _output.Check(_actions.legacyModuleReadTouches,
                "New expedition uses the selected legacy module for touch input");
            yield return Capture("12-new-expedition");
        }
    }
}
