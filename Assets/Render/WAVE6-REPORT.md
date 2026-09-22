# Wave 6 report, stage pass

Branch `zfc-stage6`, from `origin/zfc-render` at `b490ff2`, clone `/Users/fc/Documents/HealerLike-scaffold`.
Pushed to `origin/zfc-stage6`. Nothing was merged. Julien's `Assets/Scripts/` (other than the render test folders),
`Assets/Data/`, `Assets/Prefabs/`, `Assets/Scenes/` and Build Settings are unchanged.

## Tier, shadows, edges

- Tier: the stage renders with `Low_PipelineAsset`. The capture logs `GraphicsSettings.currentRenderPipeline`, and it
  is Low because the Ultra quality slot points at a missing pipeline GUID (wave 3 note). So I did not pick one tier.
  All six pipeline assets now use the look track's tested Very High setup: main light per pixel, main shadows on,
  distance 70, two cascades (split 1/3), a shadow map of at least 2048, and soft shadows. That covers the whole board
  (42.8 to 47.8 units from the portrait camera) and the look track's 26 to 40 range, whichever tier resolves.
  Low and Very Low now pay for main-light shadows. If Julien wants them cheap again, the stage should force its own
  tier instead.
- Shadows: on. The capture logs `real shadows=True cheap ellipses switched off=38`.
- Edges: `DepthNormalEdges` is on for all six renderers, with the look track's grass-safe values (depth 1 world unit at
  reference 31, slope 1, normal 55 degrees, density penalty 35 degrees, normal mask on). The board grass is not inked
  in the frames.
- Key light: the builder aims the scene's directional light so that its direction toward the light is the stones'
  serialized cheap-shadow direction (-1, 2, -1), upper left. `RenderSettings.sun` is set to it. `HLLookController`
  already publishes that light as `_HLKeyLightDir`, so the look, the real shadows and the cheap ellipses share one
  direction.
- New `Stage/HLStageKeyLight` checks at 2 Hz whether the active pipeline really renders main shadows for that light
  over the board. When it does, it switches off `GroundShadowEnabled` on every stone enemy and terrain clump, and
  switches them back on otherwise.
- The look controller now sets `InkSpacingPixels = 3.5` explicitly, as the look report asked.

## The eleven items

1. Capture with combat: done. `HLStageCapture` presses Next Wave once, after both allies are placed (0.67 s).
   Frames are at 1, 4, 8 and 12 s. The log shows:
   - allies firing on their own from 0.76 s;
   - `negative=31 (on enemies 18, from ally entities 18) ... projectiles=36 (from allies 24)`;
   - `HL capture: ally auto-attack confirmed`.

   The Character cast once (a heal at 2.5 s). Its strike on an enemy never fired, because enemy hits were already
   coming from the allies. So the 18 negative outcomes on enemies all come from ally entities, counted by
   `modifier.source`.
2. Finding 2, legacy chain visual: done. The stage chain variants keep `ChainLightningProjectile`. Its hits are applied
   synchronously before the visual loop, and those hits are untouched. The loop itself reads
   `Entity.skillStartPoint` every frame, so I skip it:
   - the builder sets `_effectMode` to FixedDuration and `_effectDuration` to -1, so the loop never runs;
   - the projectile is then flagged for destruction at once;
   - the LineRenderer stays disabled.

   The creature observer draws the chain instead. One side effect: the ChainSync lianas no longer hold for the
   projectile's lifetime, because that lifetime is now about one frame.
3. Finding 4, observer settings: done. `HLStageBuilder.ConfigureObserver` copies the creatures track's authored
   observer (`Creatures/Prefabs/HLProjectile<Name>.prefab`, through `ComponentUtility`). It then sets `deliveryStyle`
   from `Spells/Data/HLDeliveryStyles.asset`. The variants no longer force their renderers off; the observer hides
   them itself once it holds a lease (creatures fix 3). A test compares the built variants against the creature
   prefabs and the style table.
4. Finding 8 plus the grass wiring: done.
   - `HLHealPulse` is on every stage entity model and on the healer anchor.
   - The bootstrap calls `Initialize(character)` on enable and releases it on disable, because no `EntityModel` walk
     covers a Character.
   - `HLLaunchWave` is on all nine projectile variants.
   - `HLBruiseZone` is on enemy models whose authored range is under the board width (16). The Soldier's range is
     100, which bruised every cell and turned the whole board dark in my first capture, so it gets none. Today no
     shipped enemy qualifies, so no bruise shows in the demo.
   - The bootstrap also enables and disables its spell sink explicitly (part of finding 7).
5. Finding 20, reflection: done. `HLStageZoneBridge` serializes `HLZoneRegistry` and `HLGrassField` and calls
   `Buffer`, `Count` and `SetZoneSnapshot` directly. `HLRenderBootstrap` holds an `HLStoneGridEntry` and calls
   `Generate` directly. The injection seam used by the tests is kept.
6. Finding 15, range driver: done. `HLStageRangeDriver.Tick` scans the scene at 2 Hz, or at once when a cached preview
   is destroyed or disabled. It re-applies state only when the mode or the featured preview changes. Order stays
   -1500, before the previews' own Update.
7. Finding 5, stone generation: done. The stage's `HLStoneGeneration` instance sets the stones fix's `demoSceneOnly`
   flag. Without it, the stones fix makes `Generate` throw. The flag lives only in this scene, as a prefab
   modification.
8. Mapping: done. HitArmorBuffer now uses `Creatures/Prefabs/HLHitArmorBuffer.prefab` (HLBladeRosette recipe). The
   stage's cairn variant had no remaining reference, so it is deleted.
9. Shadows and edges: done, see above.
10. Environment: partly done.
    - Ring grass: done. It is now 12 strips in three bands (3, 5 and 16 units wide) at 85, 60 and 15 percent of the
      board density. The ring reaches 24 units past the board, well past the camera's near edge at z -12.6. In the
      frames the board rectangle is much softer, but a slight change in density still shows at the board edge.
    - Terrain stones: done. The block recipes are 9 to 14 blocks of 1 to 3 cells, plus 0 to 3 blocks of 1 to 2 cells,
      instead of 7 to 17 blocks of 5 to 15 cells. The board now shows small scattered clumps.
    - Foreground plane: done, but the result is weak. `HLEnvironmentForeground` places 2 to 3 boulders and 1 to 2
      blade rosettes at each bottom corner of the frame, cropped by it, in the shadow tint. In the frames the
      bottom-left corner reads as one large dark faceted stone and the bottom-right as a cropped dark edge. I cannot
      make out the rosettes.
    - Far ridge: done, but it barely reads. `HLEnvironmentRidge` places 8 to 12 monoliths and 8 to 12 mushroom stems
      so that each mid-height point sits in the last fog band. The fog nearly erases them: they show only as faint
      pale shapes at the top corners.
    - Fog warp: done. `HLFogFactor` now shifts the distance by a sine and dash-noise warp. The warp's frequency comes
      from `_HLInkWarpFreq` and its amplitude (about a third of a band) from `_HLDashAmount`. The bands read as
      wavy contours instead of arcs. The signature is unchanged and no new global was added.
11. Rebuild, tests, grep, captures, push: done. See VERIFY.

## Playable addendum

- `Stage/Editor/HLStageSmoke.Run` (batchmode, `-force-metal`) runs Julien's round loop:
  - it presses Start, places the two allies through `EntityManager.SpawnEntity` and presses Next Wave;
  - it runs at x3 through the HUD speed button;
  - on every SelectUpgrade it picks the first choice through the button's own onClick;
  - in later rounds it places two allies from the Character's `entityPool`, as `EntityGridInteraction` does (Julien's
    loop has no cost for placing), and presses Next Wave again.

  It follows the state through Julien's own `[AscensionGameType]` transition log lines. It reached round 3 and
  finished it with no exception:

  | round | alive allies | enemies | projectiles | negative | positive | frame ms avg | exceptions |
  |---|---:|---:|---:|---:|---:|---:|---:|
  | 1 | 2 | 0 | 96 | 79 | 0 | 7.30 | 0 |
  | 2 | 3 | 0 | 96 | 94 | 0 | 6.90 | 0 |
  | 3 | 4 | 0 | 143 | 145 | 0 | 7.25 | 0 |

  Two known editor-side errors are not counted: the A* update check's insecure-connection error and Unity Search's
  index error. Positive is 0 because the smoke policy never casts the Character's skills.

  The first smoke run failed with a GameOver in round 2: it placed nothing after round 1, and the two allies died.
  Placing from the pool fixed that.
- Menu items `HealerLike/Render/Open Stage` and `HealerLike/Render/Open Stage Menu` open the stage scene and the menu
  copy. `HealerLike/Render/Build Stage Menu Scene` rebuilds that copy.
- `Stage/HLStageMenu.unity` is a copy of Julien's `MenuScene`. Its StartGame button (Julien's `MainMenu.StartGame`
  loads "Main") now calls `HLStageSceneLoader.Load`:
  - in the Editor, when the stage is not in Build Settings, it uses `EditorSceneManager.LoadSceneInPlayMode` by path;
  - in a player, it calls `SceneManager.LoadScene("HLRenderLook")`.
- To add the two scenes to a build (not done, Build Settings are Julien's):
  1. Open File > Build Profiles, then the Scene List.
  2. Add `Assets/Render/Stage/HLStageMenu.unity` and `Assets/Render/Stage/HLRenderLook.unity`, with the menu first if
     it should be the entry point.
  3. The loader finds the stage by the name `HLRenderLook`.

  Julien's GameOver restart button loads "MenuScene", his own menu, not the copy. So after a GameOver in a player
  build you land on his menu, whose Start loads "Main".

## CONTRACT-CONFLICT and cross-owner edits

- `Creatures/HLLianaArm.cs`, one line. The rest tube mesh (creatures fix 16) had vertices and no normals. Julien's
  `SelectableEntity` adds QuickOutline, whose `Awake` reads one normal per vertex, and it threw
  `ArgumentOutOfRangeException` once per spawned creature. The mesh now gets its (zero) normals array at creation.
  A regression test is in `HLLianaArmTests`.

  This is outside Stage ownership. The task requires the game to reach round three with no exception, so I followed
  the task.
- `Shaders/HLLookCore.hlsl`, the body of `HLFogFactor` only, for the fog warp the task asked for. The signature and
  the 21 globals are unchanged.
- The Environment graded ring, foreground and ridge were written by a sub-agent inside this run, then compiled,
  tested, captured and committed by me.

## Not done, and limits

- Finding 6 (status removal without a stop event) needs a public gameplay membership query. It is out of scope here.
- The foreground rosettes and the far ridge barely read, as described in item 10. Moving the ridge nearer, or giving
  it its own fog offset, is the next step.
- The frame-time numbers come from the smoke run on a shared machine, in the Editor at x3 time scale. They are not a
  benchmark. Ring grass went from 131,072 to 250,524 blades in 12 fields.
- The menu copy's Start button was not pressed in a run; only its wiring was built (build log line
  `StartGame now calls HLStageSceneLoader.Load`).
- The six tracked files Unity re-dirties on import (MineBot FBX meta, four Sirenix demo metas, ProjectSettings) were
  dirty before I started and are not committed.

## VERIFY

```
VERIFY:
Unity -batchmode -nographics -quit (warm compile, base b490ff2) -> exit 0, 0 error CS
Unity -batchmode -nographics -quit (with changes, second try) -> exit 0, 0 error CS (first try: exit 1, 2 error CS, fixed)
Unity -batchmode -nographics -quit -executeMethod HealerLike.Render.Stage.HLStageBuilder.Build (final) -> exit 0, 0 error CS
Unity -batchmode -nographics -quit -executeMethod HealerLike.Render.Stage.HLStageMenu.BuildMenuScene -> exit 0
Unity -batchmode -force-metal -screen-width 1080 -screen-height 1920 -executeMethod HealerLike.Render.Stage.HLStageCapture.Run (final) -> exit 0, 4 frames, 0 error CS, 0 shader errors, 2 exceptions (A* insecure connection, Unity Search index; both pre-existing editor-side), 1 known Metal warning HLGrassZones.hlsl(8)
Unity -batchmode -nographics -runTests -testPlatform EditMode (final) -> exit 0, total 562, passed 557, failed 0, skipped 5 (graphics-device tests under -nographics), 0 error CS
Unity -batchmode -force-metal -executeMethod HealerLike.Render.Stage.HLStageSmoke.Run (final) -> exit 0, HL smoke result: PASS rounds=3 exceptions=0
grep -rnE forbidden strings over Assets/Render Assets/Settings Assets/Scripts/Tests/EditMode/Render -> exit 1, 0 matches
git push -u origin zfc-stage6 -> exit 0
BLIND-SPOT: every observation is a batchmode Metal frame or log on Fc's shared Mac; nobody looked at the stage in an interactive Editor Game view or on Julien's portrait device, and the menu copy's Start-to-stage load was never pressed at runtime.
```

Frames (1080 x 1920) in `/Users/fc/Documents/healerlike-render-specs/captures/`:
- `wave6-1.png`, md5 `dfe844a5624394b5c1e9e0d65c1295c1`
- `wave6-2.png`, md5 `09f30cc42809976504e1b15837e6f064`
- `wave6-3.png`, md5 `e3a191b7b8caa3b4e2ba0397e5758353`
- `wave6-4.png`, md5 `6c4836ed7d601b99d726c988e2b0b16a`
