# Wave 4 report

Branch `zfc-env`, from `origin/zfc-render` at `ad8b672`. Clone `/Users/fc/Documents/HealerLike-scaffold`.
Changed: `Assets/Render/Stage/`, the new `Assets/Render/Environment/`, `Assets/Render/Zones/` (the rename
only), `Assets/Render/Stones/` (the grid recipe counts only), the six renderer assets under
`Assets/Settings/` (rewritten by the builder as in wave 3), and tests under
`Assets/Scripts/Tests/EditMode/Render/`.

## 1. Rename

`HLZoneRegistry.Update(handle, kind, position, radius, strength)` is now `UpdateZone(...)`. Callers:
`HLRangePreview`, `HLZoneRegistryTests` (three calls), `Zones/README.md`. The build, test and capture
logs contain no `Update() can not take parameters` line.

## 2. Portrait camera

`HLStageCalibration.Frame(board, pitch, fov, aspect, margin, centreViewportY)` fits the board width plus
the margin at the board's near edge (the widest it projects under a pitched camera) and puts the board
centre at the given viewport height.

- Pitch 73.7 degrees (Julien's rotation x .5998, w .8002), FOV 40, aspect 9:16, captures 1080 x 1920.
- Margin 0.5 world units per side: the near corners land at viewport x .029 and .971.
- Board centre at viewport y .42.
- Resulting pose: position (0, 43.224, -9.837), rotation (73.70, 0, 0); far clip 200.
- Distances from the camera: near edge 42.758, board centre 43.837, the edge-range end 47.824.

`HLRenderBootstrap` now has a serialized `framing` (`Portrait` by default, or `Landscape`), a
`stageCamera`, and two poses. The landscape pose is wave 3's (50 degrees, distance 31). It is applied in
`OnEnable` and whenever `CameraFraming` is set. The look calibration below is for portrait only.

Conflict with the task: Julien's `Character` sits at (0, 0, 0), the grid centre, not at the near edge.
With the grid filling the width, the grid spans about the middle half of the screen height. So "healer
in the lower third, enemies in the upper third" and "grid fills the width" cannot both hold. I kept the
width fit and set the board centre (the healer) at viewport .42. The enemy half of the grid then sits
roughly between .42 and .70, and the upper 30 percent of the frame is the environment ring. Moving the
healer lower would push the enemy rows toward the centre.

## Calibration (same method as wave 3, measured on the portrait pose)

- Fog start is the camera distance to the board centre. Fog end is the edge-range end plus half its
  span. Result: fog 43.837 / 50.356, six bands, colour `#BFD2E0`.
- Hatch: `InkScale` 0.06648 (four 1920-high pixels per stroke at depth 43.837), `InkWidth` 0.00266,
  `InkDistStart` 43.837, `InkFarSpacing` 0.07978.
- Outline: 1.25 px. Wave 3 drew 1 px at depth 31 on a 1080-high target. At depth 43.8 on a 1920-high
  target, one world unit covers 1.26 times as many pixels, so the width scales by that ratio, rounded
  to a quarter pixel. Screen depth/normal edges stay off, as in wave 3.

All three values are printed by the builder as the `HL calibration:` log line.

## 3. Ground and grass ring

- `HLEnvironmentGround`: a Unity plane scaled 100 (1000 x 1000), with no collider so it never catches a
  gameplay raycast. Its top is 0.01 under the board ground top (y .49). It uses the new
  `Environment/HLLook_Ground.mat`, which is `HL/Look/Primitive` with `_BaseColor #2E7D4F`. The board
  ground uses the same material.
- Julien's own 100-scale `Ground` stays hidden: its top at .51 would bury the board and the grass
  roots at .505.
- No edge or sky shows in any of the three captures. The top of the frame is full fog.
- The 65,536-blade board field is unchanged.
- Ring: `HLEnvironmentGrass` drives four more `HLGrassField`s. Each is bound to a stage-owned proxy
  `GridManager` whose `Generate` is never called; `EnsureCells` only sizes its `cells`. The four
  strips tile a 48 x 48 square (3 grid widths) minus the board: top and bottom 48 x 16, left and right
  16 x 16.
- Ring density is a quarter of the board's (64 blades per square unit): 49152 + 49152 + 16384 + 16384
  = 131,072 blades, seeds 11 to 14.
- The strips borrow the zone buffer with a count of zero, so zones and rings stay on the board.
- I could not do a second density parameter on `HLGrassField` itself: `Grass/` is outside this task's
  ownership.
- Measured in the capture: all five fields are ready, and the ring strips draw 20034 / 6435 / 1868 /
  1876 blades after frustum culling.
- At this density and distance (about 44 units), the ring blades are 1 to 2 pixels wide and cover
  little of the ground. The ring reads as bare `#2E7D4F` ground with sparse lime specks, and the
  board's edge still shows as a change in texture.
- I checked this with a throwaway capture that disabled the board field (not committed); the ring
  blades are there. To make the carpet read as continuous, the ring needs close to full density in a
  band about 4 units deep around the board (more strips, each under the 98,304 budget), or a
  root-to-tip coloured ground. That is a decision for the next wave.

## 4. Environment scatter

- `HLEnvironmentLayout` is pure and deterministic. It is seeded from `HLStoneSeed.ForPart(seed, 0x454E5649)`
  and drawn through `HLStoneRandom`.
- `HLEnvironmentScatter` builds it in `Start` from the gameplay grid.
- Placement:
  - A 1-cell margin: nothing is placed inside the grid or within one cell of it.
  - The ring reaches 32 units past the margin.
  - Density falls as `exp(-2.2 t)`, with t from 0 at the margin to 1 at the outer edge.
  - Stones grow with distance (0.7 to 2.6 times).
  - Monoliths only beyond the far edge (+z, the top of the frame) at t >= .3, scale 1.6 to 3.2.
  - Tall kinds (monoliths, mushroom trees) never go on the camera side (z below the board), so they
    never stand in front of it.
- Counts, radii, falloff and seed are serialized in `HLEnvironmentSettings` on the component.
- Default counts, all placed with seed 1707: Boulder 120, Cairn 40, Monolith 3, MushroomTree 60,
  SpiralFern 80, BladeRosette 120, SphereCluster 70, total 493.
- Stones: `HLStoneMesh` with the `Boulder`, `Cairn` (stacks of two to four) and `Monolith` presets, in
  `HLStoneAssembly.Palette`. Material `HLLook_Stone`, colour per renderer through `_BaseColor`.
- Plants use `HLPrimitiveMeshes` in the four greens, on `HLLook_Default`:
  - Mushroom trees: a capsule stem with a flattened sphere or cone cap.
  - Spiral ferns: three to five fronds, each a seven-segment curling cylinder chain ending in a torus.
  - Blade rosettes: seven to eleven tilted cones.
  - Sphere clusters: three to five cylinder stems topped by spheres.
- Mushroom trees, ferns and clusters sway through `HLIdleMotion.Evaluate` (2.5 to 4 degrees, frequency .1).
- Tests (`Tests/EditMode/Render/Environment/`):
  - `HLEnvironmentLayoutTests`: determinism per seed, a different seed differs, nothing inside the
    margin, counts bounded per kind and by `MaxPerKind` 512, monoliths far side only, density falloff,
    invalid input.
  - `HLEnvironmentScatterTests`: one pivot per item outside the grid, a rebuild is identical, sway
    count, `Clear`.
  - `HLEnvironmentGrassTests`: strips tile the ring without touching the grid, budget cap, proxy
    cells, null safety.
- Also: `HLStageCalibrationTests.PortraitFrameFits...` and
  `HLRenderBootstrapTests.AppliesPortraitByDefaultAndLandscapeOnRequest`.

## 5. Terrain stones on the grid

- Only the render-owned `Stones/Prefabs/HLStoneGrid.prefab` changed, plus `HLStonePrefabBuilder`
  (`BlockCountDivisor = 3`, so a rebuild keeps the thinning) and its test.
- The first block system went from 20..50 to 7..17 blocks and the second from 0..10 to 0..3.
- Unchanged: sizes (5..15, 4..15), `isWalkable` false, generator order, the walkable props (2..5) and
  the completion fence.
- Julien's `Assets/Prefabs/Grid/Grid.prefab` is untouched.
- In the captures the board still carries about a hundred stones. Enough of the board is open around
  the healer and the placed allies, but the enemy half is still busy.

## 6. Captures

`HLStageBuilder.Build` was re-run, then `HLStageCapture.Run` (Metal, `-screen-width 1080 -screen-height 1920`).
The capture forces the camera aspect to 9:16 for both the grass culling and the render request.

- `/Users/fc/Documents/healerlike-render-specs/captures/wave4-1.png`, md5 `d4f86d01d75a042c74f69eb47fb35786`
- `/Users/fc/Documents/healerlike-render-specs/captures/wave4-2.png`, md5 `3ed8ba9e030a8f64c70e5df5add1e08c`
- `/Users/fc/Documents/healerlike-render-specs/captures/wave4-3.png`, md5 `3aa24fca6928caee6efd9b724b6edb63`

All are 1080 x 1920. Scripted player:
- Allies placed at 0.5 s.
- Heal +30 on Normal at 2.5 s.
- At 6 s, -500 on Soldier and on HitArmorBuffer, plus +16 on both allies.
- Totals: 5 negative outcomes (2 on enemies) and 3 heals.

Capture log (`/tmp/hl-wave4-capture-final.log`):
- 0 `error CS`, 0 shader errors.
- 5 repeats of T2's known Metal warning (`HLGrassZones.hlsl(8)`, potentially uninitialized
  `HLGrassZoneWeight`, now once per grass field).
- 2 exceptions, both editor-side and pre-existing: the A* update check's `Insecure connection not
  allowed`, and Unity Search's `SearchDatabase.EnumerateAll` index out of range.

## What could not be done, and why

- The grass ring does not read as a continuous carpet at a quarter density (section 3).
- The healer cannot sit in the lower third while the grid fills the width (section 2).
- The landscape option moves the camera but keeps the portrait look calibration.
- Nothing here was checked on a device or in the Editor Game view. The only observation is the three
  batchmode Metal captures.
