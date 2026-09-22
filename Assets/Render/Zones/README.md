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
- Add `HLHealPulse` to each healer's active model hierarchy before `EntityModel.Init`, or call
  `Initialize(sourceGameObject)` explicitly. It implements `IVisualBehaviour` and subscribes
  to the source's `HLRenderRegistry.NotifyHeal` notifications. Set `CellSize` from the grid's
  `size`; radius is 0.6 times that world-unit size. Positive resolved heals create 0.45-second
  pulses at the target's current root position. Re-init and disable unsubscribe. Already emitted
  pulses finish independently of the healer's lifetime.
- Add `HLRangePreview` to an entity/model. It implements `IVisualBehaviour`, with parent discovery
  as a Start fallback. It samples the current Range attribute in world units and emits one Heal
  zone at strength 0.35. `ObservePointer=false` plus `SetPreviewState(selected, dragging)` lets
  stage wiring drive the cosmetic preview precisely. Default pointer observation uses an
  assigned `PreviewCamera` or Camera.main, checks enabled SelectableEntity/DraggableEntity and
  public Entity.isDraggable eligibility, and maintains its own presentation selection until an
  outside click or Escape. It never calls Range.Show or mutates gameplay input state.
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

The render-owned pointer fallback is approximate: it cannot observe programmatic selection,
interaction cancellation, UI interception or custom gameplay interactions. Use the explicit
state API when the stage has that information; a future public read-only gameplay state seam
would remove this limitation. No reflection into private gameplay state is used.

# Validation limits

EditMode tests use an injected `IHLZoneUpload` fake and never allocate a GraphicsBuffer.
They validate registry ordering, capacity, stale handles, fade and teardown ordering, and
producer lifecycle/data behavior. Batch compilation validates Unity API usage. Metal buffer
layout/readback, actual grass/ring draws, gizmo appearance and pointer interaction require
stage integration and visual verification.
