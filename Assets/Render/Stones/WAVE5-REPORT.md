# D4-stones — wave 5

Branch: `zfc-dyn-stones`; clone: `/Users/fc/Documents/HealerLike-dyn-stones`.
Starting HEAD: `b6b6773`. Implementation and tests: `a8fd848`.
Confirmed branch name and `.git/hooks/pre-push` before edits. All committed changes are inside
`Assets/Render/Stones/`. Frozen contracts, gameplay scripts, Stage and Environment are unchanged.

## Done

- Boulder clusters aim with the first public `TargetProvider.GetTargets()` entry, matching
  LookAtTarget. A generated presentation-only pivot damps toward a five-degree lean, winds back
  over the final 30% of the smallest enabled skill cooldown fraction, and snaps forward nine
  degrees at accepted delivery launch. Gameplay roots and source/target anchors do not move.
- Polls the public `ACooldownSkill<T>.cooldownProgress` property on attached skills. Entity creates
  these after model initialization, so component discovery stays live; reflected property lookup
  is cached by concrete type. Disabled gameplay skills, dragging, and nonfinite fractions do not
  drive anticipation. `link.xml` preserves the generic property for stripping.
- `HLStoneEnemyVisual` implements the frozen `IHLDeliverySource`: Thrown plus Direct/Rigid aliases;
  all other styles return false. A faceted shard starts at the body, follows the actual projectile
  each LateUpdate, and bursts at the supplied contact into 3–5 cone shards and a five-ray coral
  star. Duplicate tokens are rejected; contact/end, source disable/reinit/destruction and projectile
  disappearance release owned objects and mesh leases. Contact fragments use the existing effects
  pool and can survive the source. No projectile is moved and no gameplay hit is generated.
- Current/max health polling darkens a deterministic seeded subset of boulders using per-instance
  `_BaseColor`, with full colour restoration on healing. Existing damage-confirmed shedding
  thresholds and lethal collapse retain their behavior. No shared material mutation.
- Every initialized stone enemy and terrain clump creates a long ultramarine ground disc. The
  serialized world-space `directionToKeyLight` defaults to (-1, 2, -1); shadows extend away from
  it. Both owners expose `GroundShadowEnabled`. The unlit material uses queue 2001 and the disc
  sits 0.012 units above the generated assembly base. Width follows footprint and length follows
  height. Shadow resources are released on destruction.
- Cairn/Monolith attack aim and recoil are disabled. Their only pose motion is one tiny rigid
  settle triggered by visual initialization, exponentially decaying from a maximum authored
  1.5-degree amplitude. No part bends; health shedding/death remain available.
- Added 13 EditMode cases, using existing TestHelpers, covering the public cooldown polling seam,
  immutable roots, lean/recoil, rigid presets, supported/rejected delivery styles, contact position,
  lifecycle cleanup, fragment expiry/pool reuse, progressive colour restoration and shadow behavior.

## Not done and why

- End-to-end projectile launch dispatch awaits the delivery-observer track. This checkout's
  `Assets/Render/Creatures/HLProjectileVisualObserver.cs` still calls HLCreatureRig directly and
  does not call IHLDeliverySource. The stone side is implemented and directly tested against the
  frozen interface. No duplicate observer or cross-owner patch was added.
- No real shadows, soft edge shader, terrain raycast or automatic look-controller light binding.
  HLLookSettings contains shadow tint but no direction. The explicitly permitted serialized
  direction is used; the look/stage owner must align it with the scene key light if it changes.
- Fracture is the requested per-boulder colour pattern, not mesh cracks or a texture. A monolith
  darkens as one piece. Existing shedding thresholds are retained, not replaced by new thresholds.
- No on-screen, PlayMode attack, or player/IL2CPP build review. Frozen-interface unit tests establish
  behavior, not final camera composition or observer integration. No zone registry calls were added,
  so wave 4's UpdateZone rename has no local dependency.

## CONTRACT-CONFLICT

- COMMON/CLAUDE place tests under Assets/Scripts/Tests/EditMode, but this task explicitly forbids
  changing anything under Assets/Scripts and owns only Stones. Task authority wins: tests are under
  Stones/Tests with an asmref to the existing EditMode assembly. They reuse TestHelpers and are
  included in the full suite; no test assembly or helper outside ownership changed.
- The existing observer does not yet dispatch the newly frozen delivery contract. The implementation
  stays within ownership and reports this merge dependency above.
- The general no-invented-motion rule is read together with the explicit spawn-settle requirement:
  its clock is a short cosmetic response to initialization, never represented as game state.

## VERIFY:

Commands ran on this clone only, with Unity 6000.6.0f1.

```sh
git branch --show-current
# Exit 0: zfc-dyn-stones
test -f .git/hooks/pre-push
# Exit 0

/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-dyn-stones -quit -logFile /tmp/hl-d4-compile-final.log
# Exit 0
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-dyn-stones -runTests -testPlatform EditMode -testResults /tmp/hl-d4-tests-final.xml -logFile /tmp/hl-d4-tests-final.log
# Exit 0: total 434, passed 432, failed 0, skipped 2; all 13 new cases passed.
grep -c 'error CS' /tmp/hl-d4-compile-final.log /tmp/hl-d4-tests-final.log
# Exit 1 (no matches): 0 in each log.
grep -o 'failed="[0-9]*"' /tmp/hl-d4-tests-final.xml | sort -u
# Exit 0: failed="0"
git diff --check -- Assets/Render/Stones
# Exit 0; no whitespace errors.
```

Forbidden-name extended-regex grep over all of Assets/Render/Stones: exit 1, zero matches.
Scope audit: zero committed paths outside Stones. Unity-generated changes to imported metadata
and ProjectSettings were restored to HEAD after testing. Working tree was clean after the
implementation commit. Only `git push -u origin zfc-dyn-stones` is authorized for delivery.

Earlier verification exposed two Color32 conversion compile errors, corrected before the final
compile. The first full run exited 2 with nine new-test failures: eight from a shard roughness
outside the generator's allowed range, one from exact floating-point colour comparison. The
roughness now obeys the generator contract and colour restoration uses a numerical tolerance;
the final compile and entire suite above were rerun after these corrections.

The two skipped pre-existing tests are HLGrassGpuTests shader-pass compilation and Metal compute
zone readback; both require a graphics device unavailable under -nographics. Existing look-controller
and zone-registry log messages were not changed in this ownership scope.

BLIND-SPOT: Nothing was rendered on screen; shadow/grass overlap, silhouette, settle readability,
contact star orientation, stripped-player reflection and merged observer launch dispatch remain
unobserved. Two existing GPU tests were skipped because this run used -nographics.
