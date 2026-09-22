# B2 Stones beauty

Branch confirmed `zfc-b-stones`, clone `/Users/fc/Documents/HealerLike-b-stones`, base `b490ff2`.
Implementation and tests: `4a85865`. Only Stones and its permitted EditMode test folder changed.

## Delivered

- Every accepted `HLStoneEnemyVisual.RecordImpact` emits five small grey sphere puffs immediately,
  including contacts with zero resolved damage. Unrecorded negative resource outcomes use the existing
  estimated contact. Recorded outcomes do not emit a second dust puff when health processing follows.
  Dust rises, expands gently and alpha-fades over 0.65 seconds. Existing coral/shard impact feedback remains.
- Terrain clumps read `HLZoneRegistry.Current.Snapshot` after its early LateUpdate publication.
  A new overlapping Hostile record emits dust once; Heal, Range, Bruise and Launch do not.
  The bounded 64-record comparison handles compaction, pulse age reset and concurrent overlaps without
  inventing an expanding gameplay field. No registry writes or gameplay calls.
- A health fraction crossing below 0.5 starts a three-second trickle of tiny cone shards, at most one
  every 0.18 seconds. It starts at an actual outer vertex of the primary stone. Healing above the
  threshold stops/rearms it; zero health stops it. Health comes from the existing public resource poll.
- A nearby recorded/estimated impact rocks a cairn's top stone for 1.2 seconds, with a damped seven-degree
  peak envelope. The top stays rigid. Disable restores its authored rotation and removes subscriptions.
- Seed membership `seed % 5 == 0` adds one ochre triangular facet on the primary terrain stone.
  Other facets retain their grey/slate palette. Geometry/cache leases remain untouched; the small
  overlay is owned and released by the clump. This is a deterministic one-in-five distribution.
- Every terrain clump creates a flat bare-ground disc beneath its stones, slightly wider than the
  footprint. Its default is the Stage ground green, darkened to 65 percent. Public world-space
  `BareGroundRadius` and `BareGroundCenter` are available for the grass owner to mask blades.
- Boulder aim leans seven degrees, anticipates to fourteen degrees backward over the final 30 percent
  of public cooldown, and snaps eighteen degrees forward on accepted delivery launch. Roots and
  source/target anchors do not move. Existing health-confirmed boulder shedding remains intact.
- Death throws twelve shards in an evenly distributed outward fan, 1.2–1.8 units/second horizontally,
  with upward lift, plus a short dust cloud. Collapse is still idempotent and effects outlive the body.
- Cached fracture property blocks, bound cooldown getter delegates, reusable linked-list pool nodes
  and bounded snapshot arrays remove steady-state allocations from the touched polling/effects paths.
  Native objects and meshes are created at initialization or pool growth; projectile creation and
  detached mesh copies remain event-time work. The existing global 256-fragment cap also bounds dust.
  Automated allocation assertions cover repeated cooldown/fracture polling, snapshot/health polling,
  and warmed dust emit/advance/release cycles (zero managed bytes).
- The transparent dust shader is under Stones/Resources so runtime shader lookup survives build
  stripping. It uses primitive geometry and a uniform colour/alpha only, with no textures or packages.

## Integration and limits

Existing `HLStoneEnemyVisual.Initialize` and `HLStoneTerrainClump.Initialize` install/configure their
life components automatically. Existing scene effects lookup and impact bridge wiring remain usable;
no Stage edit is required to enable the default path.

For Stage's ground palette override, after clump initialization, the exact optional wiring line is:

```csharp
clump.GetComponent<HealerLike.Render.Stones.HLStoneGroundRing>().GroundColour = groundMaterial.GetColor("_BaseColor");
```

For grass integration the available readout is:

```csharp
Vector3 center = clump.BareGroundCenter;
float radius = clump.BareGroundRadius;
```

Grass masking is not implemented here because that renderer is outside ownership. Grass may still
cover the ground disc until its owner consumes these values. Environment-owned decorative cairns
are also outside ownership; their owner can attach `HLStoneLife` and configure its existing top:

```csharp
life.Configure(effects, seed, footprintRadius, false, topStone);
```

The frozen snapshot exposes neither stable pulse handles nor duration. Observationally identical
replacement pulses at the same position/radius/age/strength cannot be distinguished; they may coalesce.
Hostile dust responds to entry into the published radius, not an invented travelling wave speed.
No contract change was made. Ground discs assume the clump's local base is the ground; no raycast is
added. Existing opaque ultramarine shadow ellipses remain as before. Dust uses a simple transparent
unlit pass, without additional outline or fog integration; final camera appearance requires review.

## Verification history

The first full run exposed a property-block constructor restriction during Unity prefab loading
and a quaternion equality assertion (28 failures including constructor cascades). The block is now
created lazily on first fracture application and reused; the rotation test uses an angle tolerance.
The final compile and full suite below ran after those fixes. Unity-generated settings and unrelated
import metadata were restored and excluded from commits.

VERIFY:

```sh
git branch --show-current
# Exit 0: zfc-b-stones
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-stones -quit -logFile /tmp/hl-b2-compile-final.log
# Exit 0
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-stones -runTests -testPlatform EditMode -testResults /tmp/hl-b2-tests-final.xml -logFile /tmp/hl-b2-tests-final.log
# Exit 0: total 538, passed 533, failed 0, skipped 5. Stones: 74/74 passed.
grep -c 'error CS' /tmp/hl-b2-compile-final.log /tmp/hl-b2-tests-final.log
# Exit 1 (no matches): 0 in each log.
grep -m 1 'failed=' /tmp/hl-b2-tests-final.xml
# Exit 0: failed="0"
git diff --check
# Exit 0
```

Prohibited-name audit over both owned trees: zero matches (grep exit 1). All ten added tests passed.
The five skips are the existing two grass resource-lifecycle cases, grass shader compilation,
Metal zone readback and opt-in Look capture; they require a graphics device.

BLIND-SPOT: Nothing was rendered on screen. PlayMode timing, final dust/grass/shadow composition,
shader GPU compilation, player stripping and IL2CPP delegate binding were not exercised; five existing
graphics-dependent tests were skipped by the required headless run.
