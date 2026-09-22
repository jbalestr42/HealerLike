# B3 grass beauty

Branch confirmed: `git branch --show-current` printed `zfc-b-grass` in
`/Users/fc/Documents/HealerLike-b-grass`. Implementation: `71d3109`.
Read COMMON, CONTRACT, BRIEF, DYNAMICS, BEAUTY, CLAUDE.md, the grass README,
WAVE5-REPORT and FIX-REPORT, and the Zones README. No Stage, Environment,
Settings, scene, prefab, gameplay or package edits are shipped.

## Delivered

- Exact default 65,536 blades, unchanged 98,304 cap, static seed/state strides and
  64-zone ABI. Each normally populated cell partitions into seeded 3–7 blade
  clumps. Blades share an exact root and a phase/rest-lean heading, with a small
  yaw fan. Clump height spans 0.6–1.4 of a 0.30 m base; outer fan blades are up
  to 4.8% shorter. Cell quotas below three preserve the requested sparse budget;
  they cannot make a three-blade clump without moving blades between cells.
- Seeded jade/lime hue blocks cover 2–4 cells. Hue uses the static seed W lane,
  shared phase uses Y; no extra GPU buffers, textures or shader keywords. The
  existing Rect-only utility has no cell-size input and remains one hue patch;
  the actual field uses the cell-aware overload.
- A broad flow made from two travelling sine modes (fundamental 0.05 Hz) steers
  the existing wind/gust direction. Stable scaled time drives wind; public
  projectile launch still drives the existing doubled gust. No invented state.
- Grass shading tilts the upper blade's shading normal toward the sky/key and
  applies the seeded jade/lime ramp variation. Illumination still uses the
  shared main-light formula and HLEvaluateSurface once, preserving Look's
  ultramarine shadow tint, threshold, hatch and fog. Depth/normal geometry and
  all indirect draw passes remain shared. Hostile cones retain rigid shading.
- Heal ring width reduced from 0.025 to 0.012 m, with grass clearance reduced
  from the 0.06–0.10 m edge band to 0.025–0.055 m. White stays a lit albedo.
- Additive `HLZoneKind.Trample = 6`, accepted/canonicalized by HLZonePacker.
  HLTrampleZone maintains one transform-following authored footprint, updates
  its radius/strength, retries owner availability, and removes its registration
  on invalid input, disable or destruction. Core grass height becomes 0.012 m,
  feathering at the perimeter, and hostile cones cannot occupy the flattened
  core. Heal cannot raise that core. No obstacle discovery or gameplay writes.
- Existing GPU tests retained and extended for core/perimeter trampling and
  hostile overlap. Tests cover clump sizes, seed independence, phase/hue sharing,
  height range, counts/ABI, zone packing and lifecycle. A warmed 1,000-refresh
  allocation regression supplements the source audit of the existing field.

## Stage wiring and unfinished work

For every stone-clump or creature root, after creating the root and choosing its
world-space footprint, Stage should execute these exact lines:

```csharp
var trample = obstacleRoot.GetComponent<HealerLike.Render.Zones.HLTrampleZone>()
    ?? obstacleRoot.AddComponent<HealerLike.Render.Zones.HLTrampleZone>();
trample.Radius = footprintWorldRadius + 0.15f;
```

`obstacleRoot` is the GameObject at the footprint centre; `footprintWorldRadius`
is the authored outer radius in world units, including the root's final scale.
No new bridge is required: Update registers before the registry's LateUpdate;
the existing field snapshot wiring consumes it. Disable the component if a
remaining death visual should stop trampling. Default strength is one.

This branch ships the component, not the attachments. Obstacles beyond the shared
first 64 published zones remain limited by the existing overflow contract; no
claim that an unlimited number can trample simultaneously. Registration/rebuild
may allocate; stable Refresh and field submission have no added managed heap
allocation. Full-frame GC and GPU timings still need a profiler capture.

Heal spheres/stalks belong to Spells. Foreground rosettes and environment planting
remain with Stage/Environment; no competing feature meshes were introduced. The
wide benchmark capture confirms grass submission but makes individual clumps
small. It does not prove reference-image beauty, tip shading at gameplay distance,
wind motion quality, heal ring crispness or attached-obstacle composition. The
benchmark did not wire the new trample component into Stage.

COMMON's older frozen kind list is superseded only by this task's explicit additive
Trample authorization. HLZone field offsets, stride and HLZoneData storage stay
unchanged; no change to frozen Look signatures or delivery contracts.

## Loaded-machine benchmark

One completed harness capture, on this clone, Apple M2 Max / Metal / Unity
6000.6.0f1, alongside other active Unity processes. Explicit URP rendering and
Game view both 1920×1080, Ultra, renderScale 1, MSAA 1, v-sync 0, frame cap -1.
Two scripted allies placed, then 120 warmup and 300 measured frames. The same
HLGrassBenchmark and instrumentation method as `$S/bench-stage.md` were used.
Temporary instrumentation lived in Grass/Editor, with its own Editor assembly;
it was removed after the run. Stage files were never edited or saved.

| Metric (ms) | bench-stage full-65536 | B3 full-65536 |
| --- | ---: | ---: |
| Harness average | 11.695 | 8.270 |
| Harness p95 | 16.273 | 13.035 |
| Harness p99 | 73.783 | 22.714 |
| Stage average | 11.696 | 8.267 |
| Stage p50 | 8.949 | 7.228 |
| Stage p99 | 58.960 | 23.622 |

Actual blades: 65,536. Harness completion saw 0 zones; the final stage callback
saw 1. Both collectors completed 300 samples. These are whole-frame Editor
measurements on a loaded machine, with evolving encounters and different source
revisions. They are not a paired cost estimate or proof of a performance gain.
No new no-grass run, player build, base-M1 run, zone/orbit matrix or GPU profiler
capture was made. No grass-only cost is inferred from the table.

Evidence on this machine: `/tmp/hl-b3-benchmark-final.log`,
`/tmp/hl-b3-machine.txt`, `/tmp/hl-b3-captures/bench-full-65536.png`, and
`/tmp/hl-b3-benchmark-instrumentation/`. Instrumentation preflight failed to
compile because it initially lacked an Editor assembly reference and an explicit
System.Environment qualification; both were corrected before measurement.
The successful benchmark has zero C# errors but includes pre-existing Metal
HLGrassZoneWeight warnings and two QuickOutline.SmoothNormals index exceptions
from SelectableEntity.Start, outside this track. Exit 0 is not an exception-free
or fully playable-game certification.

VERIFY:

Unity executable: `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`.
All commands use `-projectPath /Users/fc/Documents/HealerLike-b-grass`.

- `git branch --show-current`: exit 0, `zfc-b-grass`.
- `-batchmode -nographics -quit -logFile /tmp/hl-b3-compile.log`: exit 0,
  zero `error CS`. An accidentally overlapping test launch was rejected by the
  project lock (exit 1); the suite was relaunched after compilation exited.
- `-batchmode -force-metal -runTests -testPlatform EditMode -testResults
  /tmp/hl-b3-editmode.xml -logFile /tmp/hl-b3-editmode.log`: exit 0,
  531 passed, 0 failed, 1 opt-in Look capture ignored; all grass GPU tests passed.
- `HL_BENCH_SCENARIO=full-65536 HL_BENCH_BUDGET=65536`, with
  `-batchmode -force-metal -screen-width 1920 -screen-height 1080 -executeMethod
  HealerLike.Render.Stage.HLGrassBeautyCapture.Run -logFile
  /tmp/hl-b3-benchmark-final.log`: exit 0; 300 measured frames; zero `error CS`.
- After instrumentation removal, `-batchmode -nographics -quit -logFile
  /tmp/hl-b3-compile-final.log`: exit 0, zero `error CS`.
- Final `-batchmode -force-metal -runTests -testPlatform EditMode -testResults
  /tmp/hl-b3-editmode-final.xml -logFile /tmp/hl-b3-editmode-final.log`: exit 0,
  **532 passed, 0 failed, 1 ignored** (existing opt-in Look capture). Zero
  `error CS`; all grass GPU tests and the zero-byte warmed refresh test passed.
- Forbidden literal scan over owned Grass/Zones/shader/test output: zero matches.
  `git diff --check`: exit 0. Unity's incidental metadata/settings changes restored.

BLIND-SPOT: No on-screen or player-build review, motion video, wired Stage trample
capture, full-frame allocation profiler or isolated GPU timing. One offscreen wide
benchmark image was inspected; it cannot establish reference-image visual quality.
