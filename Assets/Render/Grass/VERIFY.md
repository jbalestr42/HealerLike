# T2 verification — 2026-09-22

VERIFY:

- `pwd`, `git branch --show-current`, `ls -l .git/hooks/pre-push`: exit 0;
  correct dedicated clone, branch `zfc-grass`, executable hook present.
- `git fetch origin`: exit 0 at start and again before final commit. No merges or
  cherry-picks. Read T1 core/outline notes at d72f516 and T3 registry at 561dc96
  with `git show`; their names remain compatible with the handoff.
- Initial plain compile command below: exit 1, four unique C# diagnostics because
  the existing global Math type masked System.Math. Qualified System.Math; all
  subsequent C# compilations passed. No frozen or gameplay files changed.
- First complete headless suite: exit 0; 215 passed, 0 failed.
- Final complete headless suite: exit 0; 217 total, 215 passed, 0 failed, 2 explicitly
  ignored device-dependent GPU tests. Log has 0 `error CS` and 0 shader-error entries.
  This includes all 198 existing tests and the 17 CPU grass tests.
- Metal grass suite: exit 0; 19 passed, 0 failed, 0 skipped. Device: Apple M2 Max.
  The two GPU tests exercised actual compute dispatch/readback and synchronous
  compilation requests for all grass/ring passes across main-shadow modes and
  both normal encodings. Log has 0 C# or shader-error entries.
- Final focused Metal rerun after the front-face naming cleanup: exit 0; 2 GPU
  tests passed, 0 failed/skipped, 0 C# or shader errors.
- `git diff --cached --check`: exit 0. Owned-source forbidden-name scan: no matches.
  Unity's incidental project settings/importer metadata changes outside the track
  were restored to the initially clean checkout before staging.

Exact Unity commands (run sequentially against this clone only):

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-grass -quit -logFile /tmp/hl-grass-compile.log
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-grass -runTests -testPlatform EditMode -testResults /tmp/hl-grass-tests.xml -logFile /tmp/hl-grass-tests.log
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -force-metal -projectPath /Users/fc/Documents/HealerLike-grass -runTests -testPlatform EditMode -testFilter HealerLike.Render.Grass -testResults /tmp/hl-grass-metal.xml -logFile /tmp/hl-grass-metal.log
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-grass -runTests -testPlatform EditMode -testResults /tmp/hl-grass-final.xml -logFile /tmp/hl-grass-final.log
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -force-metal -projectPath /Users/fc/Documents/HealerLike-grass -runTests -testPlatform EditMode -testFilter HealerLike.Render.Grass.HLGrassGpuTests -testResults /tmp/hl-grass-metal-final.xml -logFile /tmp/hl-grass-metal-final.log
```

CPU coverage: golden/deterministic layouts, independent random state, budgets/cap,
per-cell quotas, translated nonsquare grids, rect density, bounds and invalid data;
32-byte seed/state/zone ABI plus frozen zone offsets; mesh counts, taper, nondegenerate
triangles, cone outward normals and closed annulus winding; null-safe field lifecycle,
budget validation; exact 120/300 benchmark warmup/sample window and reset.

GPU coverage: two-zone 32-byte wire sentinel with different integer kinds and
fractional strength/age, XZ-only influence, nonmultiple-of-64 dispatch guard, exclusive
unique visible IDs, 0/2/64 zone counts, cone time invariance, removed influence reset,
all-culled zero counters. Test readbacks are confined to EditMode tests, never runtime.

BLIND-SPOT: Nothing was rendered on screen or visually reviewed. No staged 300-frame
benchmark, grass GPU timing, base-M1 result, actual indirect depth/normal participation,
look-core integration, outline silhouette, or scene lifecycle capture was measured.
The supplied harness and WAVE3-TODO.md specify the remaining integration checks.
