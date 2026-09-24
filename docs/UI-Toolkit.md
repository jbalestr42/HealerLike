# UI Toolkit interface

This is an opt-in UI implementation in the isolated project copy. The original gameplay and UGUI source files, data assets, prefabs, and original scenes are preserved. Integration lives in new Toolkit files and copied demonstration scenes. The Toolkit presentation currently bridges the existing gameplay/UI events; it is not a replacement for the gameplay state machine.

## Open the new interface

1. Open this project copy with its pinned Unity version (see `ProjectSettings/ProjectVersion.txt`).
2. Open `Assets/Scenes/Toolkit/MenuToolkit.unity` and enter Play mode. Select **Start expedition** to open `MainToolkit`, then start the run and prepare your party.
3. If the demo scenes have not been generated, run **HealerLike → UI Toolkit → Create Separate Demo Scenes**. This copies the original menu/gameplay scenes, installs the Toolkit host, without changing Build Settings. Existing demo scenes are reused and their Toolkit configuration is updated; their contents are not recopied from the originals.

**HealerLike → UI Toolkit → Open Demo** opens the copied menu and enters Play Mode. **Install in Current Scene** only accepts scenes under `Assets/Scenes/Toolkit/`. Use the separate demo scenes for design iteration. Original scenes retain their legacy interface; the Toolkit host suppresses legacy screen rendering only while its replacement is active. Keep the legacy components in demo scenes because they still supply gameplay event contracts.

## Layers and ownership

| Layer | Location | Responsibility |
| --- | --- | --- |
| Structure | `Assets/Resources/UI/Toolkit/GameUI.uxml` | Semantic hierarchy, stable element names, view composition |
| Card template | `Assets/Resources/UI/Toolkit/DataCard.uxml` | Reusable data-card structure for every collection |
| Layout | `Assets/Resources/UI/Toolkit/GameUI.Layout.uss` | Sizing, flex flow, spacing, scrolling, compact layout |
| Theme | `Assets/Resources/UI/Toolkit/GameUI.Theme.uss` | Color tokens, type hierarchy, borders, interaction states |
| Control foundation | `Assets/Resources/UI/Toolkit/RuntimeTheme.tss` | Unity's default runtime control geometry and base styles |
| Presentation | `Assets/Scripts/UI/Toolkit/ToolkitGameView.cs` | Binds models to named elements and reusable data cards |
| Models | `Assets/Scripts/UI/Toolkit/ToolkitCardModel.cs` | Generic title, description, icon source, status, enabled state, action |
| Game integration | `Assets/Scripts/UI/Toolkit/ToolkitGameUI.cs` | Gameplay snapshots refreshed every 0.1 seconds, state, scene navigation, action routing |
| Icons | `Assets/Scripts/UI/Toolkit/Icons/` | Descriptor, deterministic renderer, catalog, runtime lookup/cache |
| Editor tooling | `Assets/Scripts/Editor/Toolkit/`, `ToolkitIcons/` | Opt-in installation, validation, asset discovery, baking |

Edit the theme for a visual redesign. Edit layout/UXML for composition changes. Keep element names when rearranging the hierarchy so gameplay bindings continue to work. The runtime creates cards from models; a new spell or creature does not require hand-authored UI elements.

Open **HealerLike → UI Toolkit → Design Preview** for a quick editor preview with real data cards. The preview can reload styles, switch modal states, and toggle compact layout without starting an encounter. Use Play mode for final interaction checks.

## UX structure

The header keeps encounter state, gold, inventory and pause visible. Creature choices sit on the left, an inspection panel on the right, and the spellbook and encounter controls at the bottom. The center remains open for targeting and placement. Reusable cards expose an image, name, description and live status. Hover or keyboard focus populates the inspection panel.

Wave selection and rewards are deliberate modal decisions. Inventory is dismissible; pause stops the encounter until resumed. Collection lists scroll and wrap, and the spellbook scrolls horizontally as content grows. Buttons expose hover, focus, pressed and disabled states. Text labels accompany icons, so color and imagery are not the only way to identify content.

`is-hidden` controls panel visibility. `is-compact` on `hud-root` selects narrower spacing and sidebars. Layout uses UI Toolkit flex rules rather than browser CSS media queries. Test at 1280×720, 1920×1080 and a narrow window when changing dimensions; very small screens may need a future collapsed-sidebar layout.

## Dynamic icon creation

`DataIconService.GetIcon(data)` is the single runtime lookup for cards and inspection. The catalog supplies baked images when available; uncatalogued/new runtime objects receive an image through the deterministic procedural fallback. These are generated, recognizable category emblems with per-data variations, rather than externally generated painted illustrations.

The editor discovers game `ScriptableObject` assets and writes PNGs to `Assets/Resources/UIToolkit/GeneratedIcons/`. References and fingerprints live in `Assets/Resources/UIToolkit/DataIconCatalog.asset`. Asset imports schedule an incremental bake; builds also bake before packaging. Run **HealerLike → UI Toolkit → Rebuild All Data Icons** to force regeneration after changing the renderer. A catalog entry's `artworkOverride` can replace its generated image while retaining automatic defaults for other data.

The existing data assets do not need icon fields populated for the new UI to work. Add new spell or creature assets normally; discovery and runtime fallback cover them. Other game data receives the generic data emblem. To introduce a distinct visual category, extend `DataIconKind`, its descriptor classification, and `ProceduralDataIcon`; presentation code remains unchanged. The scanner automatically includes all project-owned `HealerLike` and `HealerLike.*` runtime assemblies, including `HealerLike.Render`, while excluding Editor and Tests assemblies. New project runtime assemblies using that naming convention need no registration. Render creature and spell namespaces receive their corresponding icon categories; other project data uses the generic category.

## Verification and iteration checklist

- Enter the Toolkit menu, select Start expedition, and confirm Toolkit gameplay loads.
- Deploy creatures through the new cards and verify battlefield clicks still reach placement/targeting.
- Start an encounter; inspect mana, cooldowns, gold, speed and pause/resume behavior.
- Complete a wave, choose the next encounter and select a reward; open inventory to verify the result.
- Confirm defeat returns to the Toolkit menu and that original menu/gameplay scenes still expose legacy UI.
- Add a spell/creature asset and verify an icon is produced without a manual UI registration step.

Automated validation does not replace a visual Play mode pass. Theme and layout changes should be reviewed in Game view at the target aspect ratios, including keyboard focus, long data names and larger inventories.

## Integration boundary

Toolkit components read existing gameplay state and invoke existing public actions/events. They do not require additional methods or Toolkit references in the existing gameplay or UGUI classes. Where legacy presenters keep display data private, a dedicated read-only adapter centralizes access; those private names are a compatibility boundary that should be checked when legacy presenters change.

The Toolkit host hides legacy canvas rendering and raycasters during its lifetime and restores their prior state when disabled. Legacy presenters stay alive because existing gameplay depends on their events. Icon generation reads source data and writes its own catalog and images; it does not populate fields on existing game data.

The copied Toolkit scenes are new assets. Editor demo navigation loads them by asset path through our added `ToolkitSceneNavigation` / `ToolkitDemoLauncher` adapter. Original scene files and Build Settings remain unchanged. A standalone player requires a dedicated build scene list containing the two Toolkit scenes; this editor preview does not configure or validate that build.

## Compatibility and icon policy

The Toolkit views use generated catalog icons by default, so the new UI presents a consistent icon family. Set a catalog entry's `artworkOverride` to replace a generated image without editing data or layout. Existing sprite fields remain untouched for the original UI; `DataIconService.TryGetAuthoredSprite` is available to opt into those sprites in a different presentation.

The existing player shortcut initialization is unchanged. The current demo data contains more skills than configured shortcuts, so legacy initialization reports `Not Enough inputs`. Toolkit spell cards remain available through the existing public skill actions. This pre-existing keyboard-binding limitation has not been fixed in gameplay code.

The default icon art is procedural and semantic, not a rendered portrait of each 3D model. Creature/spell categories and names choose recognizable silhouettes; unknown data gets a deterministic fallback. The renderer and catalog are independent of the UI and can be replaced with another art pipeline later.
