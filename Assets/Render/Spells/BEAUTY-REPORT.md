# B4 spell beauty

Branch `zfc-b-spells`, clone `/Users/fc/Documents/HealerLike-b-spells`, base `b490ff2`.
The initial `git branch --show-current` printed `zfc-b-spells`. Read COMMON, CONTRACT,
DYNAMICS, BEAUTY, BRIEF, CLAUDE, the spell README, WAVE5-REPORT and FIX-REPORT.
The explicit build task supersedes the older document-only BRIEF scope.

## Delivered and reviewed

`HLSpellPrefabBuilder.Build` rebuilt all **99 prefabs**. The generated `SIGNATURES.md` now
records a beauty code on every one of its **88 rows**. Its semantic inventory remains unchanged:
66 three-torus assemblies, 3 shield assemblies, 7 impact assemblies, 3 hollow mana assemblies,
4 poison assemblies and 5 heal assemblies. The style table retains 90 unique signed keys.
The inventory test walks every prefab, verifies persistent meshes/no colliders, and checks the
new geometry against its semantic kind. The builder stays in the existing Editor-only assembly.

- **Heal:** seven lime spheres each have a thin cylinder stalk. Stalk bottoms stay anchored;
  tops track the rising sphere. Buds grow, briefly swell, then pop away before expiry.
  Positive periodic health signatures now use this vocabulary too, waiting for the first
  observed period and using supplied status elapsed time thereafter. The expanding area ring
  is white and vertically thin. The actual grass bloom/disc remains the existing grass owner.
- **Multi-heal:** retained the continuous parabolic path sampled by 16 joined gold cylinders
  and its travelling dots, one link per resolved source/recipient. Added a gentle brightness
  envelope through `_BaseColor`, keeping alpha opaque; no bloom, transparency or textures.
- **Buff:** three distinct authored tilts and radii now rotate their planes around the body
  (the previous rotation spun the rotationally symmetric torus in its own plane). Added a
  3.5% size pulse and gentle brightness pulse, both reading observed status elapsed time.
  Harmful status colors continue to honor the existing coral grammar.
- **Shield:** six wider flattened sphere leaves overlap slightly at closure. They close over
  0.25 seconds of observed elapsed time, swing radially outward and separate on removal.
  Percentage-armor seams and observed charge counts remain authoritative.
- **Impact:** replaced crossed cubes and the eight-spoke cone cloud with a two-sided planar,
  eight-ray coral star and **four** small cone shards. The star faces the main camera (or
  explicit `FacingCamera`); shard positions use ballistic gravity and deterministic spin.
- **Hostile:** five asymmetric flat-shaded octahedral boulders emerge over 0.15 seconds,
  sit inside the supplied radius, then sink below ground. `PulseArea` starts them with the
  same 0.8-second cosmetic pulse forwarded to the zone owner. Existing grass darkening and
  cones are reused, not duplicated. Standalone `HLFx_HostileLitter` is also shipped.
- **Lightning:** `ShowContactLink` makes thin gold threads without heal dots. New
  `HLChainContactVisual` listens only to actual `Projectile.OnHit` events, captures ordered
  contact positions, and draws between consecutive contacts. Binding resets contact history;
  disable/destroy unsubscribe. There is no target prediction, damage application or projectile
  movement. Stage attachment is intentionally left as the integration line below.
- **Poison:** five small coral drops fall with accelerating vertical motion from the status
  anchor, reading the supplied elapsed time and authored tick period. They remain invisible
  before the first tick; no private timer executes or predicts damage. Existing tint hooks
  remain available to the body owner.

The signed outcome square-root scale law and critical paired rings are retained. New star and
boulder meshes participate in the existing shared cache lifetime and are persisted by the builder.
All new animation uses cached transforms/renderers and a reused property block. Tests measure zero
managed bytes in warmed animation loops for heal, impact, buff, shield, chain, poison and litter;
existing observer/sink/status allocation tests remain active. Construction, event-time prefab
instantiation and destruction still allocate; this is not a claim of a fully pooled effect system.

## Exact Stage wiring

On each render-owned **ChainLightning** and **ChannelingLightning** projectile variant, before
`Projectile.Init` runs, with `projectileVariant` the variant GameObject:

```csharp
if (!projectileVariant.GetComponent<HealerLike.Render.Spells.HLChainContactVisual>()) projectileVariant.AddComponent<HealerLike.Render.Spells.HLChainContactVisual>();
```

`Projectile.Init` initializes all `AProjectileBehaviour` components before synchronous hits.
The component obtains `HLRenderRegistry.Current.SpellSink` as `HLSpellVisualSink`. If Stage
uses explicit injection, the exact equivalent setup line is:

```csharp
projectileVariant.GetComponent<HealerLike.Render.Spells.HLChainContactVisual>().Sink = spellSink;
```

For an explicitly managed projectile lifecycle, use `contactVisual.Bind(projectile, spellSink)`
before hits; rebind after a disable/re-enable cycle. Do not attach this component to every
projectile, because its authored meaning is lightning contacts. No Stage files were changed.

## Not done and why

No rendered capture or visual equivalence claim: this run used batchmode with no graphics device.
Scene camera framing, final outline thickness, HDR brightness appearance, overlapping effects,
playability and live draw-call cost still need Stage review. `FacingCamera` defaults to the tagged
main camera; multi-camera per-view billboarding is not implemented. Heal links retain captured
endpoints during their 0.6-second cosmetic tail. Stalk bases are local to the heal/status anchor,
not raycast to terrain. The grass owner controls actual ground bloom and hostile cone rendering.

The lightning component is shipped and tested but not attached to Stage variants here, as Stage
is outside ownership. Body poison tint remains the documented `OnTint` integration hook.
No scene, gameplay, Stage, Environment, Settings, package, shader or frozen contract was edited.

## CONTRACT-CONFLICT

The existing outcome seam lacks original skill/signature/topology provenance, so signed outcome
prefabs remain the resource-resolution vocabulary. Inventory rows describing composition or
invalid authored intent do not authorize synthetic gameplay events. Buff source attribution,
effective stat deltas and silent handler-removal reconciliation remain constrained as documented
in WAVE5-REPORT/FIX-REPORT; this beauty task did not bypass the public-state boundary.

## Verification history

The first full run returned 2: 541 passed, one new color test failed on float equality, five
pre-existing graphics/opt-in tests skipped. The color assertion now uses a 0.00001 tolerance.
An early overlapping compile attempt returned 1 because the rebuild still held the clone; all
subsequent Unity commands were run sequentially. Final results below supersede those attempts.

VERIFY:

Implementation and tests committed together as `c1da018` and pushed to `origin/zfc-b-spells`.
All Unity commands below used executable
`/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity` with
`-batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-spells`.

| Command / check | Exit | Result |
|---|---:|---|
| `git branch --show-current` | 0 | `zfc-b-spells` |
| Unity `-quit -executeMethod HealerLike.Render.Spells.HLSpellPrefabBuilder.Build -logFile /tmp/hl-b4-build-final2.log` | 0 | 99 prefabs rebuilt, 88 reviewed rows, 90 style keys |
| Unity `-quit -logFile /tmp/hl-b4-compile-final2.log` | 0 | Plain compile, zero C# errors |
| Unity `-runTests -testPlatform EditMode -testResults /tmp/hl-b4-tests-final.xml -logFile /tmp/hl-b4-tests-final.log` | 0 | **548 total: 543 passed, 0 failed, 5 skipped** |
| `grep -c 'error CS'` on the three final logs above | 1 | 0 matches in each log |
| Recursive extended-regex grep for COMMON's forbidden strings in Spells and its test folder | 1 | 0 matches |
| `git diff --check` and `git diff --cached --check` | 0 | No whitespace errors |
| Changed-path audit against `b490ff2` | 0 | All 119 implementation paths within Spells or its authorized test folder |
| `git push -u origin zfc-b-spells` | 0 | Only the authorized branch pushed |

The five pre-existing skips are two grass resource-lifecycle graphics tests, grass shader
compilation, Metal compute, and the opt-in Look portrait capture. No Spells test was skipped.
The final run adds 20 passing tests to the 523-pass baseline. Allocation assertions passed.
Unity-generated example import metadata, missing demo metadata and ProjectSettings changes
were restored after Unity exited. Serialized owned assets received whitespace-only cleanup
following verification; no production logic changed after the final compile/test run.

BLIND-SPOT: No frame was rendered on screen; Stage lightning attachment, integrated playability,
GPU appearance, draw-call cost and multi-camera behavior were not observed.
