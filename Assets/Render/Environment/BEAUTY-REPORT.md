# Environment beauty — B1

Branch: `zfc-b-environment`, based on `b490ff2`. Confirmed branch before editing.
Shared COMMON, CONTRACT, DYNAMICS, BEAUTY and BRIEF read, plus root CLAUDE.md and
render README / WAVE4 report. Environment had no README, WAVE5-REPORT or FIX-REPORT.

## Shipped

- Seeded ring layout and its existing bounds remain unchanged: 493 default roots,
  at most 512 of each kind. Density falls and scale increases with distance.
- All plant types sway at independent seeded 0.2–0.4 Hz frequencies. Angular amplitude
  scales with plant height; rosettes have a much smaller, stiff response. The first
  1.2 seconds add a damped settle. Absolute simulation-clock sampling avoids accumulated
  drift and catches up after LOD re-entry.
- Mushroom capsule stems are lighter; jade/mint/sage caps have a dark underside and a
  separately nodding pivot. Ferns use nine shrinking, overlapping spheres per frond;
  each curl joint opens by up to three degrees during a gust. Rosettes use broad, thin
  cone blades in fans. Sphere clusters keep three to five stalks and heads.
- Per-instance seeded hue variation within six degrees and value within eight percent.
  Colour is assigned once through property blocks; no frame-by-frame material changes.
- HLEnvironmentGust accepts direction, strength and duration, uses eight fixed slots,
  replaces the oldest slot on overflow, clamps combined strength to one, and expires
  naturally. The API has no gameplay side effects or fabricated launches.
- Plant transform updates stop when the root is beyond the active look's fog end
  (fallback 60 without an applied look). Camera is cached; Stage can supply it explicitly.
- HLEnvironmentForeground creates two to four large rounded stones plus two giant
  blade rosettes in ultramarine shadow tint. They frame/crop the bottom corners for
  perspective and orthographic cameras, follow camera pose, and drift at 0.013 Hz.
  Sizes are proportional to frustum height; near-plane clearance scales with camera clipping.
- HLEnvironmentRidge builds at most 48 silhouettes, default 24: thin capsule mushroom
  stems/caps and leaning pointed monolith primitives. BuildInFogBand positions the roots
  at the middle of the last non-opaque radial fog band. No per-frame ridge loop; camera
  motion provides natural parallax. Rebuild on a framing/fog-calibration change.
- Shared primitive mesh lifetime is retained/released. Rebuild/Clear disables old geometry
  immediately before deferred destruction. No colliders, textures, packages or game state.

## Exact Stage wiring

Add `using HealerLike.Render.Environment;` to the Stage owner. With the existing
`stageCamera`, plant `Material plantMaterial`, `HLEnvironmentScatter scatter`, validated
`HLLookSettings look`, and Stage owner `gameObject`:

```csharp
var environmentGust = gameObject.AddComponent<HLEnvironmentGust>();
scatter.ConfigureMotion(stageCamera, environmentGust, look.FogEnd);
var foreground = gameObject.AddComponent<HLEnvironmentForeground>();
foreground.Configure(stageCamera, plantMaterial, 3, 1707);
var ridge = gameObject.AddComponent<HLEnvironmentRidge>();
ridge.BuildInFogBand(stageCamera, .5f, look.FogStart, look.FogEnd, look.FogBands, plantMaterial, 1707, 24);
```

At an actual projectile launch, once per initialized projectile, using its already
resolved world positions `sourcePosition` and `targetPosition`:

```csharp
environmentGust.Gust(targetPosition - sourcePosition, .65f, .8f);
```

Keep that component reference in the Stage launch adapter. No scene search or subscription
is performed by Environment. The existing projectile observer has an Init seam but no public
launch event; Stage must call the line from its real launch adapter (or arrange the observer
owner's hook), rather than infer launches from cooldown. Re-run the ridge BuildInFogBand line
after switching portrait/landscape framing or changing fog calibration. It intentionally
returns an empty ridge if the requested band cannot intersect the ground plane.

## Not done / limitations

- Stage, settings and prefab wiring were outside ownership; the gust connection and new
  depth planes require the above integration. Existing scatter improvements load automatically.
- Ring rocks retain the existing faceted boulders, stacked cairns and rare monoliths.
  Damage dust, cracked-rock trickles, enemy attack/death reactions and trampled grass are
  owned by Stones/Grass and were not duplicated here. Long soft shadows require Stage's
  light/shadow setup; Environment renderers retain normal shadow casting.
- Ridge root positions are calibrated to the last band; the tops of tall silhouettes can
  enter a nearer band, giving visible relief. It is intentionally sparse/static, not another
  animated high-detail ring. Frustum cropping, material lighting and gameplay readability
  still need a rendered Stage review.
- LOD skips transform work, not drawing or GameObjects; the existing fog shader handles
  visibility. Build-time allocations are intentional. Steady-state managed allocation tests
  cover scatter animation, gust submission/sampling and foreground following; GPU/native
  allocation profiling is not available in the headless test run.

VERIFY:

```text
git branch --show-current
  exit 0; zfc-b-environment
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-environment -quit -logFile /tmp/hl-b-environment-compile-final.log
  exit 0; 0 error CS
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-environment -runTests -testPlatform EditMode -testResults /tmp/hl-b-environment-tests-final.xml -logFile /tmp/hl-b-environment-tests-final.log
  exit 0; 539 total, 534 passed, 0 failed, 5 skipped; 0 error CS
```

The five existing graphics-dependent skips are two HLGrassFieldTests resource-lifecycle
cases, two HLGrassGpuTests cases and HLLookShaderTests.CapturePortraitShadowsAndMaskedEdges.
All 11 added test cases passed, including zero-byte steady-state managed allocation checks.
Initial compile and full test run also exited 0 (534 passed, 0 failed, 5 skipped).
Unity emitted existing plugin-importer/assembly warnings. Its unrelated generated changes
to plugin metadata, model-import metadata and ProjectSettings were restored after testing.
Owned-path `git diff --check`: exit 0. Forbidden-string scan: 0 matches.

BLIND-SPOT: No on-screen rendering or playable Stage integration was observed in this headless run.
