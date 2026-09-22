# Creature and spell match pass

OpenAI lane, built in `HealerLike-match-creatures` on `zfc-match-creatures`, from `4120756`. The original artwork 01 and wave-7 portrait were inspected before authoring.

## Presentation

Recipe body geometry is 1.45 times wider and 1.7 times taller; the healer is 2.05 times taller. Root crowns are thicker but remain inside the existing 0.46-cell footprint bound. Gameplay sockets, colliders and parent transforms are unchanged. The five silhouettes remain fern, hanging pods, sphere stack, rosette and torus healer.

Lianas use 48 links instead of 120, still with 24 cells of authored total reach. Tube width increased from 0.018 to 0.045 cells, with visible narrow joint collars and larger leaves. Mesh updates reuse the world-to-local matrix. The first Metal gallery exposed an excessive loop when the fixed-length solver aimed at a nearby target. Actual Direct delivery now uses the existing endpoint profile, as the other projectile styles already do; a short shot no longer draws its unused maximum reach as a giant coil. Healing gestures use the curved profile. The low-level fixed-length solver remains available and tested.

Impacts have a larger minimum presentation scale. Persistent spell assemblies are scaled 1.35 times. Shield leaves use green rather than gold. Character heal links resolve the real generated bud anchor when no explicit resolver is supplied.

## Readouts

`HealerLike.Render.Spells.HLResourceOutcomeObserver.Ensure(Entity)` installs one explicit health adapter per Entity. `Bind(health, mana, registry, inject)` supports the Character mana path and tests. It forwards signed impacts once and health-only positive outcomes to the heal registry. Plants no longer independently forward the same shared outcomes. Character views no longer scan every scene hierarchy each frame; they observe the explicit outcome event and their own mana resource. A positive mana delta cannot create a heal gesture.

`HLStatusObserver.Init` installs the resource adapter and `HLAttributeShieldView` on initialized entity presentations. Existing Stage attachment of the status observer therefore wires these paths for both body types. Character binds its actual BuffManager explicitly.

The new shield view reads the actual HitArmor attribute. This is required because the existing buffer grants two charges with an instant handler, and gameplay does not emit a handler-start event for instant handlers. It creates/removes plates from the current charges without inventing a lasting buff. A regression test invokes the actual instant BuffManager path and proves that plates appear despite zero handler-start events.

Poison status effects aggregate into `HLBodyTintState` on the target. Plant and Character rigs compose this tint with their health color instead of overwriting the material once. Removing another status leaves poison intact; clearing the sink restores normal color. Stones require the owning stone track to compose `HLBodyTintState.Read(entity.gameObject)` into its existing health-color path. White means no status tint; plants blend other values at 0.42. This matters because the actual poison Character skill targets enemy stone bodies.

## Wiring and remaining limits

Stage should rerun its normal builder after integration, use these existing recipe assets and avoid scaling gameplay parents. Its existing status observer attachments call the new adapters. Stone Init can additionally call `HLResourceOutcomeObserver.Ensure(entity)`; it is idempotent. Keep the stone's physical hit/fracture response, but do not add another shared impact/heal forwarder. Source-owned presentation teardown must also disable externally anchored Character views as review 2 requests.

Silent BuffManager removals and initial active-handler seeding still lack a public membership seam. The legacy chain visual still has its separate gameplay socket/lifetime problem. No private reflection, invented expiry, altered gameplay lifetime or change to Julien-owned source was used to claim those fixed.

The visual-only gallery is `/Users/fc/Documents/healerlike-render-specs/captures/wave9-creature-gallery.png`. It isolates silhouettes and a short Direct delivery; it is not gameplay. Final camera readability, stone tint integration, real status motion and the complete playable UI remain the Stage integration acceptance.

VERIFY: Recipe authoring/compile passed in the own clone. First full EditMode suite: 630 total, 624 passed, zero failed, six pre-existing graphics/opt-in skips. Metal gallery completed and its first frame was inspected; the resulting nearby-delivery correction has a dedicated regression test. Final full suite after that correction: 631 total, 625 passed, zero failed, six skipped; second Metal gallery exit 0. Evidence is in external `logs9/creatures-validation-status.txt`, `creatures-tests.xml` and `creatures-gallery.log`. Unrelated Unity-generated metadata/ProjectSettings changes remain uncommitted in this disposable clone.

BLIND-SPOT: The gallery is a staged visual fixture, not a live gameplay or device test. It does not include overlay UI or real combat outcomes. No integrated Stage smoke, player build, GPU timing or interactive pointer test was performed by this track. The public gameplay seams still limit silent status reconciliation and the legacy chain fix.
