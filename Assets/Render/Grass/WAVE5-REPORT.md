# D2 grass zones, wave 5

Branch: `zfc-dyn-grass`, clone `/Users/fc/Documents/HealerLike-dyn-grass`.
Implementation commit: `c57aa37`. Branch and local pre-push hook confirmed before work.

## Delivered

- Contract v3 appends Range=3, Bruise=4, Launch=5. The 32-byte ABI, field offsets,
  64-zone capacity, and 65,536 default blade budget remain intact. Packer validates all
  five kinds. Launch reserves byte 28 for a uint full-turn XZ heading: +X=0,
  +Z=1073741824, -X=2147483648, -Z=3221225472. Other kinds clear that lane.
  HLSL decodes the heading without changing the two storage lanes.
- Range zones bend outward, brighten mildly, and carry a thin edge ring. Selection and
  dragging stay on SetPreviewState. Collider hover uses one shared camera raycast per frame.
  AllRanges shows every active ally preview at exactly 0.15 strength. Ordinary previews
  use 0.35. Hover does not change selection. ObserveHover defaults true independently of
  the legacy ObservePointer stage flag, so the existing Featured driver permits hover.
- Heal radius blooms from zero to its authored radius in 0.3 seconds in the shared shader
  loader; blades and ring share that radius. HLHealPulse.Pulse(Transform) registers a
  target-following pulse. The registry polls the target transform until pulse expiry or
  target destruction/deactivation, independently of the source's lifetime. Existing
  positive resolved-heal notifications use this path. CPU snapshots retain authored radius.
- HLBruiseZone implements IVisualBehaviour for enemy model variants. It polls public range,
  position, health and active/type state; Bruise darkens and flattens blades without cones.
- Hostile cone height rises over 0.15 seconds, then follows pulse strength downward.
  Cone geometry stays in the cone list while sinking instead of switching to full-height blades.
- HLLaunchWave is an AProjectileBehaviour, called synchronously by Projectile.Init. It
  captures source/target positions and registers a 0.4-second directional grass front.
  It does not sample skillStartPoint, move the projectile, or apply hits. The pulse survives
  projectile destruction, including synchronous deliveries.
- The same Init calls HLGrassField.TriggerGust. Wind amplitude doubles for 0.5 scaled seconds
  toward the target; the latest launch replaces the heading and restarts the envelope.
  These durations are presentation envelopes of actual public events, not simulated cast state.
- Both new logic classes ship with their own EditMode test files in the implementation
  commit. Existing packer, registry, heal, range, field and GPU tests were extended.
  TestHelpers is reused; new tests do not access singleton instances. All GPU tests remain.

## Integration and work deliberately not done

Stage must attach HLBruiseZone to enemy model variants and HLLaunchWave to projectile
variants. Assign the launch Field explicitly or allow discovery of the single active field.
The healer Character still needs stage wiring to Initialize its HLHealPulse source.
Enable AllRanges through the static HLRangePreview.AllRanges property; no stage UI was added.
Every ally needs a preview component, as in wave 3. All previews must share the gameplay
camera; the first hover sample is reused for the frame. UI pointer interception is not read.

Stage/, Environment/, prefabs, scenes, gameplay, and both frozen delivery contract files
were not changed. Only the allowed render folders and Grass/Zones EditMode tests changed.
Unrelated Unity import changes to project settings and third-party asset metadata were restored.
Rising spheres remain the spells track's responsibility. Debug gizmos show final heal extent.

No frame-time, grass GPU-time, memory-budget, visual-quality or reference-hardware performance
claim is made. There was no on-screen composition review or integrated gameplay capture.
The 64-zone cap still limits simultaneous readouts, including AllRanges; overflow stays stable.

## CONTRACT-CONFLICT

COMMON/CONTRACT freeze HLZone and HLZoneData at the older kinds. This task explicitly
supersedes that restriction for additive v3 kinds and the reserved heading lane, so those
changes were made; the delivery contracts remain frozen and untouched.

SelectableEntity/DraggableEntity do not expose current selected/dragging state. The explicit
state API remains authoritative. The old render-owned click approximation was removed;
hover reads public colliders only. No private gameplay fields are read.

Wave 4 owns Update -> UpdateZone. The method is not renamed here. At merge, change the
single HLZoneRegistry.RefreshZone wrapper body from Update(...) to UpdateZone(...).
The existing Unity message-name warning therefore remains on this branch. Metal also
retains the pre-existing potentially-uninitialized HLGrassZoneWeight warning; both GPU tests
compile and pass. Neither warning is counted as a C# compile error or a test failure.

## VERIFY:

Commands below ran in this clone. Unity binary:
`/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`.
Each Unity command used `-projectPath /Users/fc/Documents/HealerLike-dyn-grass`.

- `git branch --show-current`: exit 0, zfc-dyn-grass.
- `test -f .git/hooks/pre-push`: exit 0.
- Initial `-batchmode -nographics -quit -logFile /tmp/hl-d2-compile.log`: exit 1;
  a project Math class shadowed System.Math. Qualified the two calls before proceeding.
- Final `-batchmode -nographics -quit -logFile /tmp/hl-d2-compile-final.log`: exit 0;
  `grep -c 'error CS'` reports 0 (grep exit 1 means no matches).
- `-batchmode -nographics -runTests -testPlatform EditMode -testResults
  /tmp/hl-d2-editmode.xml -logFile /tmp/hl-d2-editmode.log`: exit 0;
  433 total, 431 passed, failed=0, 2 graphics-dependent tests skipped; 0 error CS.
- `-batchmode -force-metal -runTests -testPlatform EditMode -testResults
  /tmp/hl-d2-metal.xml -logFile /tmp/hl-d2-metal.log`: exit 0;
  433 total, 433 passed, failed=0, skipped=0; 0 error CS. Includes compute readback,
  new zone semantics, cone timing, bloom timing, and grass/ring shader passes.
- `git diff --check` on owned render folders and Grass/Zones tests: exit 0.
- `grep -R -n -E` with the COMMON forbidden-name alternatives over owned output and
  Grass/Zones tests: no matches, exit 1 (the expected no-match result).

BLIND-SPOT: Metal tests validate computation and compilation, not on-screen appearance,
actual stage attachment, live pointer/gameplay integration, or rendering cost.
