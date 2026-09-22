# Zone integration

Add exactly one `HLZoneRegistry` to the stage. It owns a 64-record, 32-byte structured
buffer. Producers register/update during `Update`; registry `LateUpdate` runs at execution
order -1000 and uploads the packed snapshot. Grass submissions must run after this, binding
`registry.Buffer` and `registry.Count` explicitly to `_HL_Zones` and `_HL_ZoneCount` on each
compute kernel. Graphics shaders consume the globals. `Snapshot` is a read-only view of the
same packed prefix for rings/debugging; do not retain that view across frames. Never dispose
the borrowed buffer. Stop grass submissions before disabling/releasing the registry.

`Add(kind, position, radius, strength)` returns a positive handle, or zero for invalid/inactive
input or an unavailable owner. `Update(handle, kind, position, radius, strength)` preserves
order and age. Invalid or zero-strength updates remove the registration. `Remove` ignores
unknown handles. Removed handles are never reassigned, including after disable/re-enable;
this prevents stale producers from modifying a replacement. Storage slots can be reused.
Overflow keeps all registrations on the CPU and publishes the first 64 active ones in stable
order, with one warning per overflow episode. `AddPulse(..., duration)` fades linearly on
scaled time and removes the entry at zero strength. CPU arrays grow only when registering
more entries; publishing does not allocate managed arrays.

- Add `HLAreaPulse` beside `AreaOfEffect` in the stage-owned prefab variant. The authored kind
  defaults to Hostile. Start samples the initialized radius in world units (not object scale),
  emits a 0.8-second pulse, and disable removes any remaining registration.
- Add `HLHealPulse` to each healer model before `EntityModel.Init`, or call
  `Initialize(sourceGameObject)` explicitly for a Character. Positive resolved heals call
  `Pulse(Transform target)`: radius is 0.6 times CellSize, lifetime 0.45 scaled seconds.
  The registry follows the target even if the source disappears; target destruction or
  deactivation ends the pulse. `HLLoadZone` blooms the visual radius from zero to its
  authored radius over 0.3 seconds, shared by blades and ring. CPU snapshots retain the
  authored radius (debug circles therefore show the final extent).
- `HLRangePreview` reads a live Player entity's Range and position. Selection and dragging
  come exclusively from `SetPreviewState(selected, dragging)`; no click-derived selection.
  Hover uses one shared entity-collider raycast per frame, with PreviewCamera or Camera.main.
  All previews must use the same gameplay camera; the first sample is shared for that frame.
  `ObserveHover` defaults true, independently of the legacy `ObservePointer` stage-mode flag.
  This lets wave 3's Featured wiring coexist with hover. Set ObserveHover false when needed.
  Range zones have strength 0.35; static `HLRangePreview.AllRanges = true` shows every active
  ally preview at exactly 0.15, including selected/hovered allies. The stage must attach a
  preview to each ally (as wave 3 does). The 64-zone shared capacity still applies.
- Add `HLBruiseZone` to enemy model variants before `EntityModel.Init` (parent lookup fallback).
  It polls public Entity Range, position, type, active state and health, and removes its zone
  on disable/death. Bruise darkens/flattens blades without generating cones.
- Add `HLLaunchWave` alongside Projectile in projectile variants. Projectile.Init discovers
  AProjectileBehaviour components and calls Init; the wave reads source and target transforms
  once, never the round-robin skill source. It registers a 0.4-second directional front and
  calls `HLGrassField.TriggerGust(target - source)` for a doubled-amplitude 0.5-second gust.
  Assign Field explicitly, or it discovers the single active field on launch. Pulses outlive
  the projectile so synchronous hits still leave a wave. Repeated launches replace the gust
  direction and restart its cosmetic envelope; they do not stack amplitude.
- `HLZoneDebugGizmos` draws XZ circles from the published prefix, green for Heal and red for
  Hostile, with strength as alpha. Assign a registry or use the active owner.

All three producers are cosmetic. Their duration, kind and footprint do not assert persistent
healing, damage, exact application timing or gameplay occupancy. No scenes/prefabs are changed
by this track.

# CONTRACT-CONFLICT

The task requests reading public selected/dragging state, but `SelectableEntity` only stores
private `_isSelected`; `DraggableEntity` has no current-dragging flag. `InteractionManager`
also holds selected/draggable references privately. seams.md section 2 confirms Range.Show has
no events or callers. Thus exact gameplay selection cannot be polled through the requested
public seam in this clone. Gameplay and frozen contract files are left unchanged.

Selection stays on the explicit state API; collider hover does not assert selection or dragging.
Hover is a physics presentation readout and does not reproduce UI interception or custom input.

## Contract v3

Kinds append Range=3, Bruise=4, Launch=5 without changing any 32-byte wire offsets. Launch
uses reserved at byte 28 as an unsigned full-turn heading: +X=0, +Z=1073741824,
-X=2147483648, -Z=3221225472. EncodeDirection projects to XZ; zero defaults +X.
TryCreate initializes the lane to zero. Pack preserves all 32 heading bits for Launch and
clears reserved for every other kind. HLLoadZone decodes to a shader-local float2 direction;
this does not change HLZoneStorage. Launch radius is source-to-target XZ distance.

`RefreshZone` isolates the producer call to the existing registry Update method. Wave 4
owns the UpdateZone rename: change that single wrapper call at merge. This branch does
not rename the method. Contracts/ delivery files remain untouched.

# Validation limits

EditMode tests use an injected `IHLZoneUpload` fake and never allocate a GraphicsBuffer.
They validate registry ordering, capacity, stale handles, fade and teardown ordering, and
producer lifecycle/data behavior. Batch compilation validates Unity API usage. Metal buffer
layout/readback, actual grass/ring draws, gizmo appearance and pointer interaction require
stage integration and visual verification.
