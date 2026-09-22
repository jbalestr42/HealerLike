# Wave 7 report, stage wiring pass

Branch `zfc-stage7`, from `origin/zfc-render` at `5e12f52`, clone `/Users/fc/Documents/HealerLike-scaffold`.
Pushed to `origin/zfc-stage7` only. Nothing was merged. Julien's `Assets/Scripts/` (other than the render test folders),
`Assets/Data/`, `Assets/Prefabs/`, `Assets/Scenes/` and Build Settings are unchanged.

The six tracked files Unity re-dirties on import (MineBot FBX meta, four Sirenix demo metas, `ProjectSettings.asset`)
were dirty in both the trunk and this clone before I started. They are not committed.

## Wiring, item by item

New Stage code:
- `Stage/HLStageLaunchGust.cs` is the stage's launch adapter. It is an `AProjectileBehaviour`, so `Projectile.Init`
  calls it once per real launch. It calls `gust.Gust(targetPosition - sourcePosition, .65f, .8f)`, the Environment
  report's line. A prefab cannot reference the scene's gust, so the target is a static that the wiring component sets
  on enable and clears on disable (only if it is still its own).
- `Stage/HLStageBeautyWiring.cs` is on the stage root at order -1900, after the bootstrap. It holds the runtime half
  of the wiring: grid globals, tip light, gust target, healer bind, and terrain clump trample and ground colour.
- Tests are `Stage/HLStageLaunchGustTests`, `Stage/HLStageBeautyWiringTests` and the built-state checks in
  `Stage/Editor/HLStageWave7Tests`.

Environment:
- Gust: done. `HLStageBuilder.Environment` adds `HLEnvironmentGust` to `HLEnvironment` and calls
  `scatter.ConfigureMotion(stageCamera, gust, fogEnd)`. `fogEnd` is the calibrated look fog end (50.356). That is
  the scatter's sway, gust uncurl and distance LOD. `HLStageLaunchGust` is on all nine projectile variants beside
  `HLLaunchWave`, so every launch gusts the grass and the plant ring together.
- Foreground and far ridge: not rewired, as the task says. They are the wave-6 stage-wired versions. The report's
  `Configure(stageCamera, plantMaterial, 3, 1707)` and `BuildInFogBand(...)` lines name the dropped beauty
  duplicates. They do not exist on this branch; the branch has `Configure(camera, stone, green, top, 1707)` and
  `Configure(camera, stone, green, gridRect, top, fogStart, fogEnd, 6, 1707)`, which the builder already calls.
- The landscape capture re-runs `Build()` on both after switching framing, as the report asks.

Creatures:
- Buffer model override: already on the branch from wave 6 (`name.Contains("HitArmor")` selects
  `HLHitArmorBuffer.prefab`). The existing `ModelsCarryTheirReadoutsAndTheBufferIsAPlant` test still passes.
- Healer Bind: done at runtime. `HLStageBeautyWiring.Start` calls
  `view.Bind(character, HLHealer recipe, HLHealerAnchor, HLLook_Default, bootstrap.Registry, grid.size)`.
  It uses the bootstrap's own registry instead of whatever `HLRenderRegistry.Current` is.

Stones:
- Bare ground and ground colour: done at runtime. Terrain clumps only exist after `HLStoneGridEntry.Generate`, so
  the wiring component waits until it has seen generation start and finish (10 s cap). Then, on every
  `HLStoneTerrainClump`, it runs the report's optional line: `HLStoneGroundRing.GroundColour` takes the stage ground
  material's `_BaseColor`. That is the same green as the ring's default, so the change is not visible.
- Impact hooks: nothing to wire. The Stones report says `Initialize` installs dust, trickle, wobble and lean itself,
  and "no Stage edit is required". `HLStoneProjectileImpactBridge` was already on every variant.
- The decorative cairns' `HLStoneLife.Configure` line belongs to the Environment owner and was not added.

Grass:
- Trample on terrain clumps: done. `HLTrampleZone` gets radius `BareGroundRadius + .15`, on the clump itself, or on
  an `HLTrample` child at `BareGroundCenter` when the disc is off the pivot. Log line:
  `HL stage wiring: trample on 37 terrain clumps (generation observed)`.
- Trample on creature roots: done at build time. Every `Prefabs/Models/*.prefab` gets one, stone enemy included,
  and so does `HLHealerAnchor`. The radius is half a cell times the root's scale, plus .15. The Creatures report
  keeps the root crown inside its cell, and the cell is 1.
- Zone budget: in the captures the registry holds 43 of its 64 records at every frame, most of them trample. That
  leaves 21 records for heal, launch, range and hostile pulses. I did not see overflow, but a busier round could
  reach it. The Grass report says obstacles beyond the first 64 zones are not guaranteed.
- Report-named hook in `Shaders/HLGrassData.hlsl`: the fragment's last line is now the Look report's sequence
  (`HLShadeSurface`, then `HLApplyTipLight(color, height01, illum)`, then `HLApplyBandedFog`). Without it the tip
  light global would reach no grass. It is the only edit outside Stage ownership.

Spells:
- Lightning attachment: done. `HLChainContactVisual` is on `HLChainLightning` and `HLChannelingLightning` only. It
  finds the sink through `HLRenderRegistry.Current.SpellSink`, the report's default path.
- Prefab reference updates: none needed. The Spells report's 99 rebuilt prefabs keep their GUIDs, and the stage
  reaches them through the sink prefab.

Look:
- Grid globals: done at runtime. On enable, `HLStageBeautyWiring` publishes `_HLGridOrigin` (board min),
  `_HLGridCell` (`grid.size`), `_HLGridExtent` (board size) and `_HLGridStrength` .12 from the GridManager. On
  disable it zeroes `_HLGridStrength` and `_HLTipLight`.
- Ground material: `_HLGroundGrid = 1` is set on a Stage-owned copy, `Stage/Materials/HLStageGround.mat`, used by the
  board ground and the environment plane. The shared `Environment/HLLook_Ground.mat` stays at 0, and a test checks
  that. The grid shows as a faint pale lattice on the board in `wave7-*.png`.
- Tip light: `_HLTipLight` .12 is published with the grid, and grass now calls it (see Grass).
- Outline: the builder's distance-derived width (1.25 px) is replaced by `OutlineWidthPixels = 1`, as the Look report
  asks. `InkSpacingPixels = 3.5` was already set.

## Smoke (PlayMode, three full rounds)

`HLStageSmoke.Run`, batchmode, `-force-metal`, x3 time scale:

| round | alive allies | enemies | projectiles | negative | positive | frame ms avg | exceptions |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 2 | 0 | 96 | 79 | 0 | 10.25 | 0 |
| 2 | 3 | 0 | 104 | 101 | 0 | 9.21 | 0 |
| 3 | 4 | 0 | 163 | 163 | 1 | 9.40 | 0 |

`HL smoke result: PASS rounds=3 exceptions=0`. The log has the same two editor-side exceptions as wave 6 (the A*
update check's insecure connection and the Unity Search index), outside the game, and not counted. Wave 6 measured
7.30 / 6.90 / 7.25 ms on the same machine. The rise of about 2 to 3 ms comes from this wave's extra work: 43
trample zones, scatter animation with the gust, and the tip-light path. The machine is shared and the Editor runs
at x3, so this is not a benchmark and I did not profile which part costs what.

## Captures

Portrait 1080 x 1920 in `/Users/fc/Documents/healerlike-render-specs/captures/`:
- `wave7-1.png` (1 s), md5 `7d24effbdd4dc1a8bedd945b1c14a7bd`
- `wave7-2.png` (4 s), md5 `4fed43837a64709ccc1cc75ef30530b6`
- `wave7-3.png` (8 s), md5 `1a693f272a11bb66c1e755dd3a06a37f`
- `wave7-4.png` (12 s), md5 `6de550b14f7aa2135032a0bf201d253b`

The portrait run's log shows the ally auto-attack confirmed, `real shadows=True cheap ellipses switched off=38`,
pipeline `Low_PipelineAsset`, 0 shader errors, and the two editor-side exceptions above.

Landscape 1920 x 1080: `wave7-land.png` (8 s), md5 `1b90063770283620a62331aeb9928be1`. The frame comes from
`HL_CAPTURE_LANDSCAPE=1` on `HLStageCapture.Run`. The log shows 0 shader errors and the same two editor-side
exceptions.

The landscape frame shows a problem: the foreground rebuilt for the 16:9 wave-3 pose puts two very large
dark boulders over the bottom edge of the board. In landscape they hide part of the play area. The look
calibration (fog, hatch) is also for portrait. Landscape needs its own foreground scale before anyone plays it.

## Not done, and limits

- The foreground in landscape is too large (see above). Not changed; the foreground is the wave-6 stage version the
  task says not to rewire.
- Trample rings and gusts are in the frames only as still images. No capture shows grass flattening or the plant ring
  swaying on a launch as motion, so I did not see them work, only that nothing threw.
- The ground colour line has no visible effect today (same green).

## VERIFY

```
VERIFY:
Unity -batchmode -nographics -quit (compile with wiring) -> exit 0, 0 error CS
Unity -batchmode -nographics -quit -executeMethod HealerLike.Render.Stage.HLStageBuilder.Build -> exit 0, 0 error CS, "HL stage build complete"
Unity -batchmode -force-metal -executeMethod HealerLike.Render.Stage.HLStageSmoke.Run -> exit 0, HL smoke result: PASS rounds=3 exceptions=0 (2 known editor-side exceptions in log)
Unity -batchmode -force-metal -screen-width 1080 -screen-height 1920 -executeMethod HealerLike.Render.Stage.HLStageCapture.Run -> exit 0, 4 frames, 0 shader errors, 0 error CS, 2 known editor-side exceptions
HL_CAPTURE_LANDSCAPE=1 Unity -batchmode -force-metal -screen-width 1920 -screen-height 1080 -executeMethod HealerLike.Render.Stage.HLStageCapture.Run -> exit 0, 1 frame, 0 shader errors, 2 known editor-side exceptions
Unity -batchmode -nographics -runTests -testPlatform EditMode -> exit 0, total 627, passed 621, failed 0, skipped 6, 0 error CS
grep -rnE forbidden strings over Assets/Render Assets/Settings Assets/Scripts/Tests/EditMode/Render -> exit 1, 0 matches
git push -u origin zfc-stage7 -> exit 0
BLIND-SPOT: the gust, trample flattening and tip light are motion or subtle shading, and I only saw four still batchmode Metal frames plus one landscape frame; nobody watched the stage move in an interactive Game view, and the 43-of-64 zone occupancy was not stressed past round 3.
```
