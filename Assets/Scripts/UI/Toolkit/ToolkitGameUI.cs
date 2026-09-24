using System;
using System.Collections.Generic;
using System.Linq;
using HealerLike.UI.Toolkit.Integration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HealerLike.UI.Toolkit
{
    /// <summary>
    /// Opt-in migration adapter. Legacy presenters remain alive as gameplay dependencies;
    /// their canvases are suppressed while this document is enabled. No scene is changed automatically.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitGameUI : MonoBehaviour
    {
        [SerializeField] string _gameplayScene = "Main";
        [SerializeField] string _menuScene = "MenuScene";
        [SerializeField] bool _hideLegacyCanvases = true;
        [SerializeField] VisualTreeAsset _layout;
        UIDocument _document;
        ToolkitGameView _view;
        UIManager _ui;
        GameManager _game;
        PlayerBehaviour _player;
        EntityManager _entities;
        InteractionManager _interaction;
        GameView _legacy;
        AscensionGameType _ascension;
        bool _menu, _inventoryOpen, _paused;
        float _previousSpeed = 1f, _nextRefresh;
        Entity _selectedEntity;
        bool _inspecting, _hadSelectedEntity, _interactionActive;
        GameObject _lastWorldSelection;
        AItem _selectedItem;
        InventoryHandler _selectedItemOwner;
        Button _equipButton;
        Button _inventoryEquipButton;
        DropdownField _inventoryTarget;
        DropdownField _inventorySlot;
        List<int> _availableSlots = new List<int>();
        readonly List<Entity> _inventoryTargets = new List<Entity>();
        DropdownField _targeting;
        readonly Dictionary<Canvas, bool> _canvases = new Dictionary<Canvas, bool>();
        readonly Dictionary<UnityEngine.UI.GraphicRaycaster, bool> _raycasters = new Dictionary<UnityEngine.UI.GraphicRaycaster, bool>();
        public void ConfigureScenes(string gameplayScene, string menuScene)
        {
            _gameplayScene = gameplayScene;
            _menuScene = menuScene;
        }

        void Start() => Initialize();
        void OnEnable() { if (_view != null) Initialize(); }

        void Initialize()
        {
            LegacyUiReader.ValidateContract();
            _document = GetComponent<UIDocument>();
            if (_document.panelSettings == null)
            {
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.name = "Toolkit runtime panel";
                settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Toolkit/RuntimeTheme");
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                _document.panelSettings = settings;
                _ownedPanel = settings;
            }
            var layout = _layout != null ? _layout : Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
            if (layout == null)
            {
                Debug.LogError("ToolkitGameUI requires Resources/UI/Toolkit/GameUI.uxml", this);
                enabled = false;
                return;
            }
            _document.rootVisualElement.Clear();
            _document.rootVisualElement.style.flexGrow = 1;
            layout.CloneTree(_document.rootVisualElement);
            // CloneTree may introduce full-screen TemplateContainers; keep the board pickable.
            _document.rootVisualElement.Query<TemplateContainer>().ForEach(container => container.pickingMode = PickingMode.Ignore);
            _view = new ToolkitGameView(_document.rootVisualElement);
            _ui = FindAnyObjectByType<UIManager>();
            _game = FindAnyObjectByType<GameManager>();
            _player = FindAnyObjectByType<PlayerBehaviour>();
            _entities = FindAnyObjectByType<EntityManager>();
            _interaction = FindAnyObjectByType<InteractionManager>();
            _ascension = FindAnyObjectByType<AscensionGameType>();
            _menu = _game == null;
            if (_ui != null) _legacy = _ui.GetView<GameView>(ViewType.Game);
            _view.Inspect = model => { _inspecting = true; _view.Detail(model); };
            _view.InspectEnded = () => { _inspecting = false; if (_selectedEntity != null && _selectedItem == null) RefreshEntityDetail(); };
            _document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(evt =>
                _document.rootVisualElement.Q("hud-root")?.EnableInClassList("is-compact", evt.newRect.width < 1100f));
            _view.Bind("wave-button", StartOrAdvance);
            _view.Bind("start-button", StartOrAdvance);
            _view.Bind("inventory-button", () => { _inventoryOpen = !_inventoryOpen; Refresh(); });
            _view.Bind("inventory-close-button", () => { _inventoryOpen = false; Refresh(); });
            _view.Bind("pause-button", TogglePause);
            _view.Bind("resume-button", TogglePause);
            _view.Bind("restart-button", () => LoadScene(_menuScene));
            _view.Bind("speed-slow-button", () => SetSpeed(.5f));
            _view.Bind("speed-normal-button", () => SetSpeed(1f));
            _view.Bind("speed-fast-button", () => SetSpeed(2f));
            var mark = _document.rootVisualElement.Q<Toggle>("mark-entity-toggle");
            mark?.RegisterValueChangedCallback(evt => { if (_legacy != null) _legacy.gameHUD.markEntityToggle.isOn = evt.newValue; });
            BuildDetailActions();
            HideLegacy();
            Refresh();
        }

        PanelSettings _ownedPanel;
        void OnDestroy() { if (_ownedPanel != null) Destroy(_ownedPanel); }

        void OnDisable()
        {
            foreach (var pair in _canvases) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in _raycasters) if (pair.Key != null) pair.Key.enabled = pair.Value;
            _canvases.Clear();
            _raycasters.Clear();
            if (_paused) { Time.timeScale = _previousSpeed; _paused = false; }
            if (_document != null) _document.rootVisualElement?.Clear();
        }

        void HideLegacy()
        {
            if (!_hideLegacyCanvases) return;
            // Scope to legacy screen views, preserving world-space health bars and floating combat text.
            var roots = _ui != null ? new[] { _ui.gameObject } : FindObjectsByType<MainMenu>(FindObjectsSortMode.None).Select(x => x.gameObject).ToArray();
            foreach (var root in roots)
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.renderMode == RenderMode.WorldSpace) continue;
                    if (!_canvases.ContainsKey(canvas)) _canvases.Add(canvas, canvas.enabled);
                    canvas.enabled = false;
                    foreach (var raycaster in canvas.GetComponents<UnityEngine.UI.GraphicRaycaster>())
                    {
                        if (!_raycasters.ContainsKey(raycaster)) _raycasters.Add(raycaster, raycaster.enabled);
                        raycaster.enabled = false;
                    }
                }
            }
            // Some scenes keep the menu controller beneath its screen Canvas.
            if (_menu)
                foreach (var menu in FindObjectsByType<MainMenu>(FindObjectsSortMode.None))
                {
                    var canvas = menu.GetComponentInParent<Canvas>();
                    if (canvas != null && !_canvases.ContainsKey(canvas)) { _canvases.Add(canvas, canvas.enabled); canvas.enabled = false;
                        foreach (var raycaster in canvas.GetComponents<UnityEngine.UI.GraphicRaycaster>())
                        {
                            if (!_raycasters.ContainsKey(raycaster)) _raycasters.Add(raycaster, raycaster.enabled);
                            raycaster.enabled = false;
                        }
                    }
                }
        }

        void Update()
        {
            if (_view == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_inventoryOpen) _inventoryOpen = false;
                else if (_interactionActive || (_interaction != null && _interaction.GetInteraction() != null))
                    _interaction?.CancelInteraction();
                else TogglePause();
            }
            _interactionActive = _interaction != null && _interaction.GetInteraction() != null;
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + .1f;
                Refresh();
            }
        }

        void StartOrAdvance()
        {
            if (_menu) { LoadScene(_gameplayScene); return; }
            if (_paused || _legacy == null) return;
            var hud = _legacy.gameHUD;
            if (LegacyUiReader.GameState(_game) == GameManager.GameState.None && hud.startGameButton.interactable)
                hud.startGameButton.onClick.Invoke();
            else if (hud.nextWaveButton.interactable)
                hud.nextWaveButton.onClick.Invoke();
        }

        void LoadScene(string scene)
        {
            Time.timeScale = 1f;
            _paused = false;
            if (!ToolkitSceneNavigation.TryLoad(scene))
                _view.Text("status-label", $"Scene '{scene}' is not included in this player build.");
        }

        void TogglePause()
        {
            if (_menu) return;
            _paused = !_paused;
            if (_paused) { _previousSpeed = Time.timeScale; Time.timeScale = 0f; }
            else Time.timeScale = _previousSpeed > 0f ? _previousSpeed : 1f;
            Refresh();
        }

        void SetSpeed(float speed)
        {
            _paused = false;
            _previousSpeed = speed;
            Time.timeScale = speed;
            Refresh();
        }

        void Refresh()
        {
            _view.Visible("pause-panel", _paused);
            _view.Visible("inventory-panel", _inventoryOpen && !_paused);
            _view.Visible("selection-panel", !_menu && _ui != null && LegacyUiReader.CurrentView(_ui) == ViewType.Wave);
            _view.Visible("upgrade-panel", !_menu && _ui != null && LegacyUiReader.CurrentView(_ui) == ViewType.Upgrade);
            _view.Visible("gameover-panel", !_menu && _ui != null && LegacyUiReader.CurrentView(_ui) == ViewType.GameOver);
            if (_menu)
            {
                _document.rootVisualElement.Q("hud-root")?.AddToClassList("is-menu");
                _view.Visible("menu-panel", true);
                _view.Visible("spell-section", false);
                _view.Visible("inventory-button", false);
                _view.Visible("pause-button", false);
                _document.rootVisualElement.Q(className: "speed-controls")?.AddToClassList("is-hidden");
                _view.Visible("mark-entity-toggle", false);
                _view.Visible("currency-label", false);
                _view.Visible("party-panel", false);
                _view.Visible("detail-panel", false);
                _view.Visible("wave-button", false);
                _view.Visible("start-button", true);
                _view.Text("phase-label", "WELCOME TO HEALERLIKE");
                _view.Text("wave-label", "Build your party. Keep them alive.");
                _view.Text("status-label", "Choose Start expedition to begin.");
                _view.Button("start-button", "Start expedition", true);
                _view.Button("inventory-button", null, false);
                _view.Button("pause-button", null, false);
                return;
            }
            if (_legacy == null || _game == null || _player == null) return;
            bool start = LegacyUiReader.GameState(_game) == GameManager.GameState.None;
            bool preparing = _ascension == null || LegacyUiReader.AscensionState(_ascension) == AscensionGameType.State.WaitForRoundToStart;
            bool overlay = LegacyUiReader.CurrentView(_ui) != ViewType.Game;
            _view.Visible("start-button", start);
            _view.Visible("wave-button", !start);
            _view.Button("start-button", "Start expedition", !_paused && !overlay && _legacy.gameHUD.startGameButton.interactable);
            _view.Text("currency-label", $"{_player.gold} gold");
            _view.Text("wave-label", _ascension != null ? $"Wave {_ascension.currentRound}" : "Expedition");
            _view.Text("phase-label", start ? "READY" : preparing ? "PREPARATION" : "IN BATTLE");
            _view.Text("status-label", _paused ? "Paused" : _interaction != null && _interaction.GetInteraction() != null ? "Click a valid target. Escape cancels." : start ? "Start your expedition" : preparing ? "Deploy your party, distribute equipment, then start the wave." : $"Combat · {Time.timeScale:0.#}× speed");
            _view.Button("wave-button", start ? "Start expedition" : "Start wave", !_paused && !overlay && (start ? _legacy.gameHUD.startGameButton.interactable : preparing && _legacy.gameHUD.nextWaveButton.interactable));
            _view.Button("inventory-button", null, !start && preparing && !overlay);
            if (!preparing || overlay) { _inventoryOpen = false; _view.Visible("inventory-panel", false); }
            if (_player.character != null && _player.character.mana != null)
                _view.Resource("mana-bar", _player.character.mana.Value, _player.character.mana.Max);
            RefreshParty(preparing && !start && !overlay);
            RefreshSpells(!start && !overlay && !_paused);
            RefreshChoices();
            if (LegacyUiReader.SelectedObject(_legacy) != _lastWorldSelection)
            {
                _lastWorldSelection = LegacyUiReader.SelectedObject(_legacy);
                _selectedEntity = _lastWorldSelection != null ? _lastWorldSelection.GetComponent<Entity>() : null;
                RefreshEntityDetail();
            }
            if (_selectedEntity == null)
            {
                if (_hadSelectedEntity && !_inspecting)
                    _view.Detail(new ToolkitCardModel { Title = "Choose a creature", Description = "Select a deployed ally to inspect its stats and equipment." });
                _hadSelectedEntity = false;
                _targeting?.SetEnabled(false);
            }
            else
            {
                _hadSelectedEntity = true;
                if (!_inspecting && _selectedItem == null) RefreshEntityDetail();
            }
            RefreshEquipmentAction(preparing && !overlay);
            if (_inventoryOpen) RefreshInventory();
        }

        void RefreshParty(bool deployEnabled)
        {
            var models = new List<ToolkitCardModel>();
            if (_legacy.entityInventory != null)
                foreach (var choice in LegacyUiReader.AvailableEntities(_legacy.entityInventory))
                {
                    if (choice == null || choice.data == null) continue;
                    var data = choice.data;
                    models.Add(new ToolkitCardModel { Key = $"deploy-{data.GetEntityId()}", IconSource = data, Title = data.title,
                        Description = data.description, Status = "Deploy", Enabled = deployEnabled,
                        Activate = () => { _interaction?.SetInteraction(new EntityGridInteraction(data)); _view.Text("status-label", "Click an open tile to deploy. Escape cancels."); } });
                }
            if (_entities != null && _entities.entities != null)
                foreach (var go in _entities.GetEntities(Entity.EntityType.Player))
                {
                    if (go == null) continue;
                    var entity = go.GetComponent<Entity>();
                    models.Add(new ToolkitCardModel { Key = $"entity-{go.GetEntityId()}", IconSource = entity.data, Title = entity.data.title,
                        Description = entity.data.description, Status = entity.health != null ? $"HP {ToolkitPresentation.Resource(entity.health.Value, entity.health.Max)}" : "Deployed",
                        Activate = () => { _selectedEntity = entity; _selectedItem = null; RefreshEntityDetail(); } });
                }
            _view.Cards("party-list", models);
        }

        void RefreshSpells(bool enabled)
        {
            var models = new List<ToolkitCardModel>();
            if (_player.character != null)
                foreach (var slot in _player.character.skillSlots)
                {
                    if (slot == null || slot.data == null) continue;
                    models.Add(new ToolkitCardModel { Key = slot.GetEntityId().ToString(), IconSource = slot.data, Title = slot.data.name,
                        Description = TextConvertor.Convert(slot.data.description, _player.character, slot.data),
                        Status = LegacyUiReader.SkillStatus(slot),
                        Enabled = enabled && LegacyUiReader.CanUse(slot), Activate = slot.UseSkill });
                }
            _view.Cards("spell-list", models);
        }

        void RefreshChoices()
        {
            if (LegacyUiReader.CurrentView(_ui) == ViewType.Upgrade)
            {
                var models = new List<ToolkitCardModel>();
                foreach (var go in LegacyUiReader.UpgradeChoices(_ui.GetView<UpgradeView>(ViewType.Upgrade)))
                {
                    if (go == null) continue;
                    var entityChoice = go.GetComponent<SelectItemUpgradeButton>();
                    var playerChoice = go.GetComponent<SelectPlayerItemUpgradeButton>();
                    var item = entityChoice != null ? LegacyUiReader.Item(entityChoice) : LegacyUiReader.Item(playerChoice);
                    if (item == null) continue;
                    models.Add(new ToolkitCardModel { IconSource = item, Title = item.title,
                        Description = LegacyUiReader.ItemDescription(item), Status = entityChoice != null ? "Party equipment · Choose reward" : "Healer upgrade · Choose reward",
                        Activate = () => { if (entityChoice != null) entityChoice.SelectUpgrade(); else playerChoice.SelectUpgrade(); Refresh(); } });
                }
                _view.Cards("upgrade-list", models);
            }
            if (LegacyUiReader.CurrentView(_ui) == ViewType.Wave)
            {
                var models = new List<ToolkitCardModel>();
                foreach (var choice in LegacyUiReader.WaveChoices(_ui.GetView<WaveView>(ViewType.Wave)))
                {
                    if (choice == null || LegacyUiReader.Wave(choice) == null) continue;
                    models.Add(new ToolkitCardModel { IconSource = LegacyUiReader.Wave(choice), Title = LegacyUiReader.Wave(choice).name, Status = "Choose wave", Activate = choice.SelectWave });
                }
                _view.Cards("selection-list", models);
            }
        }

        void BuildDetailActions()
        {
            var host = _document.rootVisualElement.Q("detail-actions") ?? _document.rootVisualElement.Q("detail-description")?.parent;
            if (host == null) return;
            _targeting = new DropdownField("Target priority", Enum.GetNames(typeof(TargetBehaviourType)).ToList(), 0);
            _targeting.AddToClassList("detail-targeting");
            _targeting.RegisterValueChangedCallback(evt =>
            {
                if (_selectedEntity != null && _selectedEntity.targetProvider != null && Enum.TryParse<TargetBehaviourType>(evt.newValue, out var target))
                    _selectedEntity.targetProvider.targetBehaviourType = target;
            });
            host.Add(_targeting);
            _equipButton = new Button(TransferSelectedItem) { text = "Select equipment" };
            _equipButton.AddToClassList("button");
            host.Add(_equipButton);
            _inventoryEquipButton = _document.rootVisualElement.Q<Button>("inventory-equip-button");
            if (_inventoryEquipButton != null) _inventoryEquipButton.clicked += TransferSelectedItem;
            _inventoryTarget = _document.rootVisualElement.Q<DropdownField>("inventory-target");
            _inventorySlot = _document.rootVisualElement.Q<DropdownField>("inventory-slot");
            _inventoryTarget?.RegisterValueChangedCallback(evt =>
            {
                int index = _inventoryTarget.index;
                if (index >= 0 && index < _inventoryTargets.Count)
                {
                    _selectedEntity = _inventoryTargets[index];
                    RefreshEntityDetail();
                    Refresh();
                }
            });
        }

        void RefreshEntityDetail()
        {
            if (_selectedEntity == null) return;
            var data = _selectedEntity.data;
            string stats = data.description;
            if (_selectedEntity.health != null) stats += $"\nHealth: {ToolkitPresentation.Resource(_selectedEntity.health.Value, _selectedEntity.health.Max)}";
            if (_selectedEntity.attributeManager != null)
                foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
                    if (_selectedEntity.attributeManager.Has(type)) stats += $"\n{type}: {_selectedEntity.attributeManager.Get(type).Value:0.##}";
            _view.Detail(new ToolkitCardModel { IconSource = data, Title = data.title, Description = stats });
            if (_targeting != null && _selectedEntity.targetProvider != null) _targeting.SetValueWithoutNotify(_selectedEntity.targetProvider.targetBehaviourType.ToString());
        }

        void RefreshInventory()
        {
            var models = new List<ToolkitCardModel>();
            if (_inventoryTarget != null && _entities != null)
            {
                _inventoryTargets.Clear();
                foreach (var go in _entities.GetEntities(Entity.EntityType.Player))
                    if (go != null) _inventoryTargets.Add(go.GetComponent<Entity>());
                _inventoryTarget.choices = _inventoryTargets.Select((entity, index) => $"{index + 1}. {entity.data.title}").ToList();
                int selected = _inventoryTargets.IndexOf(_selectedEntity);
                _inventoryTarget.SetValueWithoutNotify(selected >= 0 ? _inventoryTarget.choices[selected] : "Choose an ally");
            }
            if (_inventorySlot != null && _selectedEntity != null)
            {
                var slots = ToolkitInventoryTransfer.EmptySlots(_selectedEntity.inventoryHandler);
                var labels = slots.Select(index => $"Slot {index + 1} · {3 - System.Math.Min(index, 2)}× effect").ToList();
                string old = _inventorySlot.value;
                _availableSlots = slots;
                _inventorySlot.choices = labels;
                _inventorySlot.SetValueWithoutNotify(labels.Contains(old) ? old : labels[0]);
                _inventorySlot.SetEnabled(_selectedItemOwner == _legacy.playerInventory.inventory.inventoryHandler);
            }
            AddItems(models, _legacy.playerInventory.inventory.inventoryHandler, "Stash");
            if (_selectedEntity != null) AddItems(models, _selectedEntity.inventoryHandler, _selectedEntity.data.title);
            if (_player.character != null) AddItems(models, _player.character.inventoryHandler, "Healer");
            if (models.Count == 0) models.Add(new ToolkitCardModel { Title = "No equipment yet", Description = "Win a wave to earn rewards.", Enabled = false });
            _view.Cards("inventory-list", models);
        }

        void AddItems(List<ToolkitCardModel> models, InventoryHandler owner, string location)
        {
            if (owner == null) return;
            foreach (var entry in owner.items)
            {
                var item = entry.item;
                if (item == null) continue;
                var model = new ToolkitCardModel { IconSource = item, Title = item.title, Description = string.IsNullOrEmpty(LegacyUiReader.ItemDescription(item)) ? $"{location} · Select to manage equipment" : LegacyUiReader.ItemDescription(item), Status = entry.inventoryIndex >= 0 ? $"{location} · Slot {entry.inventoryIndex + 1}" : location };
                model.Activate = () => { _selectedItem = item; _selectedItemOwner = owner; _view.Detail(model); _view.Text("inventory-detail", $"{item.title} · {location}"); RefreshEquipmentAction(true); };
                models.Add(model);
            }
        }

        void RefreshEquipmentAction(bool preparing)
        {
            if (_equipButton == null) return;
            var stash = _legacy != null ? _legacy.playerInventory.inventory.inventoryHandler : null;
            bool fromStash = _selectedItemOwner == stash;
            bool healerItem = _player != null && _player.character != null && _selectedItemOwner == _player.character.inventoryHandler;
            bool valid = preparing && !_paused && _selectedItem != null && _selectedItemOwner != null && _selectedItemOwner.items.Exists(x => x.item == _selectedItem) && !healerItem && (!fromStash || _selectedEntity != null);
            _equipButton.SetEnabled(valid);
            _equipButton.text = _selectedItem == null ? "Select equipment in inventory" : healerItem ? "Healer upgrade equipped" : fromStash ? (_selectedEntity != null ? $"Equip to {_selectedEntity.data.title}" : "Select a deployed ally") : "Return to stash";
            _targeting?.SetEnabled(preparing && _selectedEntity != null && !_paused);
            if (_inventoryEquipButton != null)
            {
                _inventoryEquipButton.SetEnabled(valid);
                _inventoryEquipButton.text = _equipButton.text;
            }
        }

        void TransferSelectedItem()
        {
            if (_selectedItem == null || _selectedItemOwner == null || _legacy == null || _paused) return;
            if (_ascension != null && LegacyUiReader.AscensionState(_ascension) != AscensionGameType.State.WaitForRoundToStart) return;
            var stash = _legacy.playerInventory.inventory.inventoryHandler;
            var destination = _selectedItemOwner == stash ? _selectedEntity?.inventoryHandler : stash;
            if (destination == null || !_selectedItemOwner.items.Exists(x => x.item == _selectedItem)) return;
            int index = ToolkitInventoryTransfer.FirstEmptySlot(destination);
            if (destination != stash && _inventorySlot != null && _inventorySlot.index >= 0 && _inventorySlot.index < _availableSlots.Count)
                index = _availableSlots[_inventorySlot.index];
            if (!ToolkitInventoryTransfer.Transfer(_selectedItem, _selectedItemOwner, destination, index)) return;
            LegacyInventoryAdapter.Synchronize(_selectedItemOwner);
            LegacyInventoryAdapter.Synchronize(destination);
            _selectedItem = null;
            _selectedItemOwner = null;
            Refresh();
        }
    }
}
