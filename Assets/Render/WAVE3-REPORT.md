# Wave 3 report

Branch `zfc-wave3`, from `origin/zfc-render` at `0b01227`. Clone `/Users/fc/Documents/HealerLike-scaffold`.
Nothing outside `Assets/Render/`, `Assets/Settings/` (the six renderer assets) and
`Assets/Scripts/Tests/EditMode/Render/` changed.

## Contract v2 (additive)

- `Contracts/IHLZoneOwner.cs`: `int AddPulse(HLZoneKind kind, Vector3 center, float radius, float strength, float seconds)`.
- `HLRenderRegistry.ZoneOwner { get; set; }`, null by default.
- `HLZoneRegistry` implements `IHLZoneOwner` (its existing `AddPulse` already matched).
- `HLSpellVisualSink.PulseArea` uses its injected `AreaPulse` when set, otherwise
  `HLRenderRegistry.Current?.ZoneOwner?.AddPulse(kind, center, radius, strength, 0.8f)`.
- `HLRenderBootstrap` publishes the zone owner on the registry, binds `sink.AreaPulse` to
  `zones.AddPulse(..., 0.8f)` on enable and clears it on disable.
- Tests: `HLRenderRegistryTests.ZoneOwner_DefaultsToNullAndRoundTrips`,
  `HLSpellVisualSinkTests.NullDelegateFallsBackToRegistryZoneOwner`,
  `HLZoneRegistryTests.ActsAsTheRegistryZoneOwner`,
  `HLRenderBootstrapTests.BindsZoneOwnerAndSinkAreaPulseThenClearsOnDisable`.

## Swapped

`HLStageBuilder.Build` was re-run on the merged tree; `Stage/WAVE3-TODO.md` now lists no open item.

- `HLRenderStage`: `HLLookController` (calibrated, below), `HLZoneRegistry`, an instance of
  `Spells/Prefabs/HLSpellVisualSink.prefab` (its `material` set to `HLLook_Default`),
  `HLStoneGrid.prefab` as `HLStoneGeneration` with its `HLStoneGridEntry`, `HLGrassField` bound to
  the gameplay `GridManager`, ground and camera, `HLStageZoneBridge` (order 0, between zone
  publication at -1000 and grass at 10000), and the new `HLStageRangeDriver`.
- Stone generation keeps both block-system recipes from the prefab (20..50, size 5..15; 0..10,
  size 4..15) and the completion fence; the template's second `GridManager` is removed and the
  existing gameplay grid is used, seed 1707.
- `HLHealerAnchor` carries `HLCharacterView` with `Creatures/Data/HLHealer.asset` and `HLLook_Default`.
- The nine projectile variants carry `HLProjectileVisualObserver` and
  `HLStoneProjectileImpactBridge`; `HLArea1..3` carry `HLAreaPulse`.
- Entity data now points at stage model variants under `Stage/Prefabs/Models/`: each is the
  creature or stone model prefab plus `HLStatusObserver` at the model root (allies also get
  `HLRangePreview` with `ObservePointer` off). `Entity.Init` instantiates the model and calls
  `EntityModel.Init`, which walks every `IVisualBehaviour`, so the observer binds before any
  buff handler can start.
- All six renderer assets carry the real `HLOutlines` feature (no `HLOutlines_PLACEHOLDER` left).
- Materials: every serialized reference under `Assets/Render` to `HLSpellPlaceholder`,
  `Creatures/Data/HLPlaceholder`, `Stage/Materials/HLAllyPlaceholder` goes to
  `Look/HLLook_Default.mat`; `Stones/HLPlaceholderStone` and `Stage/Materials/HLStonePlaceholder`
  go to the new `Look/HLLook_Stone.mat` (HL/Look/Primitive, base `#8E93A1` slate; stones still
  override `_BaseColor` per instance from the brief's palette `#C9C4B4 / #8E93A1 / #4A5468 / #C79A4B`).
  This covers the five spell effect prefabs, the sink prefab, the creature and stone prefabs,
  `HLStoneBlock` and the stage scene.
- Removed as dead: `Stage/Prefabs/HLModel*Entity.prefab` (11), `Stage/Materials/` (both
  placeholder materials), `Stage/HLGrassPlaceholder.asset`. No asset referenced them.

## Range preview

`HLStageRangeDriver` (order -1500, before zone publication) uses the explicit
`SetPreviewState(selected, dragging)` API from `Zones/README.md`. Gameplay selection stays private,
so the stage cannot mirror it. Mode `Featured` (the stage default) shows the first live ally's
range as a Heal zone at strength 0.35; `Hidden` shows none; `Pointer` hands the previews back
to their own approximate pointer observation. Tests: `HLStageRangeDriverTests`.

## Not swapped, with the reason

- `HLHealPulse` is not attached. The healer is a `Character`, not an `Entity`, so no `EntityModel`
  walk inits it, and `HLCharacterView` already registers the heal sink for the Character.
  Attaching it would need a stage call to `Initialize(character.gameObject)`; not done here.
- `HLStatusObserver` is on entity models only, not on the Character.
- The placeholder `.mat` files owned by the tracks stay on disk:
  `HLCreatureAssetAuthoringTests` loads `Creatures/Data/HLPlaceholder.mat`, and the track
  builders recreate them. Nothing in the stage or the prefabs references them any more.
  The runtime URP Lit fallbacks in `HLSpellPrimitives` and `HLStoneEffects` remain in code; they
  only trigger when a material field is empty, which I did not observe directly.
- Depth/normal screen edges are off (below).

## Calibration against the actual camera

Camera: perspective, pitch 50 degrees, FOV 40, distance 31, position (0, 24.247, -19.926),
board 16 x 16 at cell 1, grass roots at y=.505. Board distances from the camera: near edge 26.57,
centre 31.00, far corners 37.5.

- Fog: start 30.996 (board centre), end 46.276 (the wave-2 edge range end 39.707 plus half its
  span), six bands, colour `#BFD2E0`. The wave-2 range (26.570/39.707) started at the near edge
  and washed out the back half of the board in the first capture; the far row now keeps
  roughly 55 to 60 percent of its colour.
- Hatch: `InkScale` 0.08358 (four 1080p pixels per stroke at depth 31), `InkWidth` 0.00334,
  `InkDistStart` 30.996, `InkFarSpacing` 0.10030. Other ink values stay at `HLLookSettings.Default`.
- Outline: 1 px, hull-only. `HLOutlines.DepthNormalEdges` is false on all six renderers. With it
  on, a side-by-side capture showed the screen edge pass inking almost every grass blade (its
  normal threshold of 0.2 and depth threshold are fixed in `HLOutlinesEdges.shader`), turning
  the field navy with lime specks. Hull outlines on stones, creatures and effects are unchanged.
  Re-enabling edges for grass needs distance- or density-aware thresholds in T1's edge shader.

## Capture

`HLStageCapture.Run` enters play and presses Start, then acts as a scripted player through the
same public calls a player's clicks make: at 0.5 s it places `NormalEntity` and
`ChainLightningEntity` next to the first enemy (`EntityManager.SpawnEntity`), from 2.5 s it
uses a Character skill on an ally (`CharacterSkillSlot.UseSkill` then the interaction's
`OnMouseClick` with a real raycast), and from 6 s it uses the next skill on an enemy. Frames are
taken at 1, 4 and 8 simulated seconds after Start, from a `LateUpdate` hook at order 32000 so the
indirect grass draws of that frame are included. It repaints the Game view each tick: without
that, batchmode never reaches `WaitForEndOfFrame` and `GridGenerator` never places stones.

Observed in the final run: heal +30 on Normal at 2.5 s (skill slot 0), -500 on Soldier and on
HitArmorBuffer at 6 s (skill slot 4, which also heals both allies +16). The two allies placed
next to the Soldier did not attack on their own during 6 s; I did not find out why.

## Known leftovers

- Metal compile warning: `HLGrassZones.hlsl(8)` potentially uninitialized `HLGrassZoneWeight` (T2).
- Unity logs `Script error (HLZoneRegistry): Update() can not take parameters.` because T3's public
  `Update(handle, ...)` shares the message name. It compiles and works, but it logs on every import.
- The stone block recipes fill most of the board, which hides allies among the stones.
- Each rebuild regenerates the added-component file IDs in the projectile and area variants and
  rewrites the matching data references, so a rebuild always shows a diff there.
