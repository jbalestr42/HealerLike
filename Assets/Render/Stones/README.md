# Stones integration

T5 assets are isolated under this folder. Assign the two model variants to the corresponding enemy model slots during stage assembly: `HLStoneSoldierModel` for Soldier and `HLStoneCairnModel` for HitArmorBuffer. Their inherited HUD, EntityModel, authored SkillSource and target tag remain present. Monolith is a parameter preset, intentionally unassigned to enemy data. Geometry is created once during visual Init; there is no editor-time generation or deformation.

Source asset limitation: the original Grid prefab’s first generator references missing script GUID `51e649e13e54af44aaa511b582e95fc6`. The render variant explicitly substitutes a basic `GridGeneratorSystem` with the same min/max/walkability values in that slot. Its prefab GUID `356c856d27b224948a6a15287e83110a` is also missing, so that slot uses `HLStoneGridPlaceholder`, an empty nonblocking prop. This keeps basic cell selection operational without inventing an obstruction on a walkable cell. The missing implementation cannot be recovered from this clone, so exact behavior of that first slot cannot be verified. The two BlockGridSystem entries are fully preserved.

`HLStoneGridEntry.Generate(grid, ground, seed)` invokes gameplay generation and changes cell walkability. Production attachment must never call it; gameplay owns generation. The entry refuses by default. Only an explicit demo fixture may set the serialized `demoSceneOnly` flag on the calling `HLStoneGridEntry` component and use `Prefabs/HLStoneGrid.prefab` after creating its grid. Both original block entries retain their order, walkability, count and chain-size ranges. The last generator entry is a render-only completion fence: it consumes no random values and creates no objects. `IsGenerating` stays true until that fence is reached; overlapping Generate calls throw before overwriting seeds. Do not remove or reorder the fence, or invoke the raw GridGenerator concurrently. An externally stopped coroutine must not be restarted by calling Generate on the locked entry; recreate the generation host after cancellation.

Use the `HLStone*` projectile prefab variants where impacts should record an estimated contact. The observer reads OnHitData's target because Projectile has already cleared its target property by the callback. Direct hits fall back to the nearest stone surface toward the modifier source. Estimated contacts are explicitly marked; these are not physics contact reports. Recording only changes visual context, never consumers or projectile motion.

Place an `HLStoneEffects` component on a scene root and an `HLStoneDeathBridge` on an external scene object, then call `Bind(entityManager, effects)` with the scene's injected manager. The bridge synchronously collapses only entities with zero health before removal. The health listener is an idempotent fallback. If no effects owner was injected, a scene-local effects root is created on first runtime use and survives enemy destruction; no gameplay singleton is accessed. No effects emit from disable, destroy, or living administrative removal.

`HLStoneEnemyVisual.Initialize(resource, seed, effects)` permits explicit per-life seed/resource injection. `Init(Entity)` uses the authored seed (default 1), binds the entity's health and samples root displacement. A future mover can implement `IHLStoneMotionSource` on the entity root to supply actual planar velocity and yaw. Current enemies remain stationary; spawn jumps, dragging, vertical displacement and teleports do not produce locomotion. LookAtTarget owns BodyPivot yaw when present. All surviving parts remain fixed relative to BodyPivot.

`HLStoneMesh` is a version-1 radially displaced icosphere with positive ellipsoid scaling, 60/240/960 private render vertices, flat face normals, welded topology retained in the mesh data, and reference-counted mesh leases. Size is full nominal X diameter; elongation scales Y and depth ratio scales Z. Boulder, Cairn and Monolith use this same sphere-derived generator. Word-wise FNV-1a folds grid seed, signed coordinates as uint, then version 1; part/layout/geometry salts do not consume gameplay random state.

The material is `HLPlaceholderStone` (URP Lit, slate, zero metallic/smoothness), because no Look material was present in this branch. Parts use `_BaseColor` property blocks; coral effects use an owned shared coral material. T7 should swap the material for the shared Look stone material and verify its outline path with the preserved welded topology. No custom look shader or renderer feature is introduced here.

`Editor/HLStonePrefabBuilder.cs` can reproduce the variants via the HealerLike menu or batch `-executeMethod HealerLike.Render.Stones.HLStonePrefabBuilder.Build`. It writes only this folder and leaves source prefabs, scenes and data untouched. Runtime event order, graph release, apparent ground contact, fog/outline integration and silhouette readability require stage PlayMode and camera review.

## Verification (2026-09-22)

Unity 6000.6.0f1 batchmode ran only on `/Users/fc/Documents/HealerLike-stones`.

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-stones -executeMethod HealerLike.Render.Stones.HLStonePrefabBuilder.Build -quit -logFile /tmp/hl-stones-assets.log
# Exit 0; prefab creation succeeded; zero error CS matches.
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-stones -runTests -testPlatform EditMode -testResults /tmp/hl-stones-final.xml -logFile /tmp/hl-stones-final.log
# Exit 0; 244 passed, 0 failed: 46 Stones and 198 pre-existing tests; zero error CS matches.
```

Actual XML summary:

```xml
<test-run id="2" testcasecount="244" result="Passed" total="244" passed="244" failed="0" inconclusive="0" skipped="0" asserts="0" engine-version="3.5.0.0" clr-version="4.0.30319.42000" start-time="2026-09-22 19:25:12Z" end-time="2026-09-22 19:25:26Z" duration="14.5642253">
```

The mesh sweep covers 147,456 generated configurations (1,024 seeds × 3 subdivisions × 3 presets × 2 roughness limits × 8 axis-scale extremes). Tests also cover exact word-fold seed values, a version-1 geometry hash, flat normals, bounds, global RNG independence, cache leases, clamped health shedding, lethal precedence, recorded impacts, expiry/unbind, independent debris lifetime, global fragment capacity, terrain determinism, and prefab structure.

Scope/name/metadata and whitespace audits passed. Source prefabs, scenes, data, frozen contracts and account preferences were not edited. Pre-existing clone warm-up changes outside the Stones ownership area were left untouched and uncommitted.

BLIND-SPOT: No on-screen or PlayMode camera review was performed. Exact behavior of the source grid's missing first generator/prop cannot be recovered; the explicit render-owned fallback is described above. Shared Look material, outline integration and stage wiring still need T7 review.

## Wave 5 dynamics

`HLStoneEnemyVisual` adds a generated `HLStonePresentation` pivot below BodyPivot. Only this
pivot leans; gameplay root, source/target anchors and BodyPivot remain untouched by the new
attack poses. It reads the same first `TargetProvider.GetTargets()` entry as LookAtTarget.
Enabled skill components are discovered each frame (Entity adds them after model Init); cached
public reflection reads `ACooldownSkill<T>.cooldownProgress`. The minimum finite remaining
fraction drives anticipation during the last 30% of cooldown. A successful delivery launch
snaps the boulder forward. There is no fabricated cast clock. The generic public property is
preserved by `link.xml` for stripping. Cairn/Monolith ignore aim and recoil and receive only a
1.5-degree, exponentially decaying rigid settle from visual initialization; this is cosmetic
spawn response, not simulation state. Existing health shedding/collapse still apply.

Delivery supports Thrown, Direct and Rigid, rejects other styles and duplicate live tokens,
spawns a small faceted shard at the visual body, then tracks the live projectile in LateUpdate.
Contact emits 3–5 stone cones plus a five-ray coral star. Contact, End, disable, reinitialization
and projectile disappearance release shard mesh leases idempotently. Contact debris belongs to
the independent effects owner and can outlive the source. The observer must dispatch the frozen
`IHLDeliverySource` callbacks (the wave-3 observer in this checkout does not yet do so).

Health polling restores authored per-instance colours at full health and progressively darkens
a seeded subset of boulders toward ultramarine as health falls. This is a cluster-level crack
pattern using `_BaseColor`, not a texture or per-face mesh alteration; the monolith darkens as
one piece. Existing damage-confirmed shedding thresholds are preserved.

Every initialized enemy and terrain clump creates an `HLStoneGroundShadow` disc, dark ultramarine,
with queue 2001 and a 0.012 world-unit ground offset. Both visual owners expose
`GroundShadowEnabled`; set it false when real shadows are enabled. `directionToKeyLight` is a
serialized WORLD direction toward the light, default (-1, 2, -1); the ellipse extends away
from it. HLLookSettings has no light direction field, so this is intentionally authored rather
than inferred from unavailable globals. Ground means the generated assembly base, not a physics
raycast; uneven terrain and grass occlusion need camera review. These are opaque cheap stand-ins.

All stone tests live in `Assets/Scripts/Tests/EditMode/Render/Stones/`, namespace
`HealerLike.Render.Stones`, using the existing EditMode assembly and TestHelpers.
Disabling an effects owner clears live fragments, releases global slots and hides its pool.
Scene lookup retains that owner while disabled; emission calls are ignored until re-enabled.
LookAtTarget is cached during visual initialization; reinitialize after changing BodyPivot components.
