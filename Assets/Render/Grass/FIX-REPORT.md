# F3 grass fix report

Branch confirmed with `git branch --show-current`: `zfc-fix-grass`. Work stayed in
this clone, Grass/Zones/HLGrass shader ownership and the corresponding EditMode
folders. Read COMMON, CONTRACT, BRIEF, CLAUDE.md, the complete render review,
bench-stage.md's benchmark results, and `git show
origin/zfc-dyn-look:Assets/Render/Look/WAVE5-REPORT.md`.

## Findings and disposition

- **D5 CONTRACT-CONFLICT (unnumbered):** added the explicitly requested
  `_MAIN_LIGHT_SHADOWS_SCREEN` branch using
  `ComputeScreenPos(TransformWorldToHClip(positionWS))`; atlas/cascade variants
  retain world-to-shadow conversion. The existing shader test compiles all four
  shadow keyword choices, both normal encodings and every grass/ring pass.
  **Qualification to the review:** the installed URP `Shadows.hlsl`, lines
  360–373, already branches inside `TransformWorldToShadowCoord` for opaque
  screen shadows. Thus the assertion that this checkout necessarily supplied
  atlas coordinates is wrong for its installed URP version. The explicit branch
  documents the requested behavior and matches the Look track's approach; it
  is not evidence of a reproduced visual defect.
- **#15, owned portion:** `HLRangePreview.SetPreviewState` now sets state only;
  its Update performs publication. Added a regression test proving that setters
  do not add/remove zones until Update, and retained explicit Refresh coverage.
  **Out of ownership:** `HLStageRangeDriver` still owns scene discovery and its
  registration/selection strategy. F3 cannot remove its scene scan. Its Update
  order relative to preview Update can delay a changed selection by one frame;
  Stage should order its state input before previews.
- **Allocation/search audit:** no `FindObjectsByType` or `FindObjectsOfType` in
  Grass/Zones. `HLLaunchWave` has a cached fallback `FindAnyObjectByType` only
  during launch initialization. Preview refresh uses value types and existing
  handles. Registry publication reuses `_source`/`_packed` arrays and compacts
  its existing list; growth occurs on Add, and overflow logging occurs on the
  overflow transition. Grass reuses frustum arrays and render parameters;
  allocations occur on build/rebuild, not stable frame submission. Benchmark
  logging/sorting occurs once at completion. No recurring managed allocation
  was found by source inspection; this is not a profiler measurement.
- **Unnumbered lifecycle coverage note:** no production disposal fix was needed.
  New `HLGrassFieldTests` build a real field, count all **7 owned GraphicsBuffers,
  3 meshes, 3 materials and 1 compute instance**, then require zero surviving
  native resources after explicitly dispatching the runtime OnDisable or OnDestroy callback
  through TestHelpers (EditMode does not automatically dispatch them here). Disable is
  tested across three rebuild cycles; the borrowed zone buffer must remain
  valid. Object destruction follows callback dispatch. Existing cleanup is idempotent and the Build catch path calls it.
- **Benchmark request:** README records the unchanged **65,536 default** and
  **98,304 stock cap**, the stock clamp experiment, and the loaded M2 Max
  measurements: full stage 11.696 ms average versus no grass 7.699 ms (about
  4 ms difference), with percentile figures and shared-machine caveats.
- **#8:** missing heal-pulse attachment/resource wiring belongs to Stage and
  the body/resource observers, outside F3. The existing zone producer is usable.
  **#18:** the review explicitly justifies the specialized grass cone; general
  creature/spell primitive consolidation is outside F3.
- Other numbered findings (#1–7, #9–14, #16–17, #19–21) concern gameplay,
  Contracts, Stage, Spells, Creatures or Stones. They were not changed. Frozen
  zone/layout contract files and shader seams were not edited. The inherited
  `HLZoneRegistry.Update` Unity message-signature diagnostic remains; its API
  rename is explicitly assigned to the other integration track in this source.

Implementation commits: `d3ce7fc` (D5 conflict, lifecycle test, benchmark docs),
`4a784fc` (#15). No new production C# class. Tests use shared TestHelpers and
never access a gameplay singleton instance.

VERIFY:

`U=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`
and `P=/Users/fc/Documents/HealerLike-fix-grass`. All runs were batchmode on P.

| Command | Exit | Results |
| --- | ---: | --- |
| `"$U" -batchmode -nographics -projectPath "$P" -quit -logFile /tmp/hl-f3-compile.log` | 0 | zero compiler errors |
| `"$U" -batchmode -nographics -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-f3-editmode.xml -logFile /tmp/hl-f3-editmode.log` | 0 | 432 passed, 0 failed, 4 graphics skips |
| `"$U" -batchmode -force-metal -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-f3-metal.xml -logFile /tmp/hl-f3-metal.log` | 2 | initial fixture: 434 passed, 2 failed, 0 skips |
| `"$U" -batchmode -nographics -projectPath "$P" -quit -logFile /tmp/hl-f3-compile-final.log` | 0 | final compile: zero compiler errors |
| `"$U" -batchmode -force-metal -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-f3-metal-final.xml -logFile /tmp/hl-f3-metal-final.log` | 0 | **436 passed, 0 failed, 0 skipped** |

Initial Metal failures were the two new fixture cases relying on automatic
runtime callbacks in EditMode. Commit `04f84be` corrects the fixture to dispatch
those callbacks explicitly and guarantees cleanup even if an assertion fails.
The final full suite includes the pre-existing compute readback and shader-pass
compilation tests. Both lifecycle variants pass with actual Metal resources.
`grep -c 'error CS'` prints 0 for all five logs (exit 1 means no match).
XML root `failed` is 0 in both successful full-suite runs.

The six COMMON forbidden literals were scanned with `grep -REn` over
Grass, Zones, HLGrass shader files and both owned test folders: exit 1, zero
matches. The scene-search grep in Grass/Zones likewise returned zero matches.
`git diff --check`: exit 0. Unity-generated unrelated metadata and ProjectSettings
changes were restored; no unrelated source or assets are committed.

BLIND-SPOT: No on-screen rendering, screen-shadow renderer-feature capture,
player build, new benchmark or GC profiler capture was performed. Resource tests
exercise runtime build/cleanup methods with real Metal allocations in EditMode;
they do not observe deferred Destroy timing in PlayMode or domain reload. Stage's
scene scan and producer ordering remain integration work outside F3 ownership.
