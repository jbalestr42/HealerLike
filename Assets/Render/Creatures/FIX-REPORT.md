# F1 creature fixes

Branch confirmed before edits: `zfc-fix-creatures`. Work is limited to Creatures, the registry implementation, and their EditMode tests. Read COMMON, CONTRACT, review-render, BRIEF and the clone's CLAUDE test rule. The explicit task authorizes the registry fixes despite the older frozen-file rule; no contract signature or shader changed.

- **3:** Resolve active source-model components through `IHLDeliverySource`, trying subsequent adapters if one declines. Hide projectile renderers only after a successful lease; restore their original enabled states on lease loss/disable. Unsupported, missing and saturated deliveries preserve fallback visibility. Creature-owned projectile variants and authoring now inherit original renderer states. Tests cover generic adapters, rejection, fallback, restoration, styles and prefab states.
- **9:** Dispatch newest-first over an invocation-local snapshot. Registration changes affect subsequent notifications; captured sinks run once even if unregistered during the current notification. Nested notifications get independent snapshots. Tests cover removal of A from [A,B,C] by C (C,B,A, then C,B) and nested dispatch.
- **10:** Each rig retains the primitive mesh cache and releases it exactly once on disposal, including when its parent has already been destroyed. Last-owner release destroys cached meshes; explicit ReleaseAll cannot invalidate live rigs. Tests exercise two simultaneous rigs, repeated disposal, destroyed parents and guarded cache cleanup.
- **12:** Registry removal rejects only actual null source references. Builder and externally anchored Character view retain the registered GameObject wrapper for teardown after owner destruction. Tests assert the dictionary entry is removed in all three cases.
- **16:** Replaced 241 per-arm render objects with one six-sided procedural tube mesh and one renderer. It is hidden at rest, with no rest mesh uploads; only visible active gestures rebuild geometry. The existing 120-link solver, reach, delivery styles, contact timing and retraction remain. Each arm owns and disposes its mesh. Tests cover the authored arm, finite geometry, endpoint, idle visibility/upload stability and native mesh destruction, alongside existing gesture/reach tests.
- **18:** Spell sink owns target heal particles. Removed creature EmitHeal, mote pool, allocation and ticking; retained impact forwarding, registry notification and source heal gestures/body response. Tests still verify exactly one impact/notification per resolved outcome and no creature mote objects.
- **19:** Recipe glow and mana readout brighten RGB through the shader-supported `_BaseColor`; alpha is retained by the brightness helper. Removed unsupported emission writes and authoring setup. Tests inspect initial and updated property blocks. No look shader edits.

## Review qualifications and ownership

Finding 3's assertion that no delivery implementations exist is stale for this branch: creature builder, Character view and rig already implemented the interface. The unconditional renderer suppression remained valid and is fixed. Stone adapter implementation belongs to Stones; Stage-owned projectile prefabs still contain disabled-renderer overrides (for example Stage/Prefabs/Projectiles/HLChainLightning.prefab). Those must be addressed by their owners to restore integrated stone fallback visibility. This track does not force-enable renderers that another owner deliberately authored disabled.

Finding 18's broader cross-track primitive consolidation is outside the assigned duplicate-heal fix. Other review findings (1, 2, 4–8, 11, 13–15, 17, 20, 21) were not changed: their fixes belong to gameplay or other render folders. In particular, finding 2's source-socket read is in the gameplay chain coroutine; this observer never advances that socket.

The renderer reduction is structural, not a measured frame-time claim. Active Direct gestures retain the existing FABRIK iteration budget. Snapshot allocations occur on heal notification, with explicit nested-dispatch semantics. Mesh cache lifetime follows the final live rig; it is not per-recipe eviction.

VERIFY:
- `git branch --show-current`: exit 0, zfc-fix-creatures.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-fix-creatures -quit -logFile /tmp/hl-f1-compile-final.log`: exit 0.
- Same Unity executable with `-batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-fix-creatures -runTests -testPlatform EditMode -testResults /tmp/hl-f1-editmode-final.xml -logFile /tmp/hl-f1-editmode-final.log`: exit 0; 444 total, 442 passed, zero failed, two skipped.
- `grep -c 'error CS' /tmp/hl-f1-compile-final.log /tmp/hl-f1-editmode-final.log`: zero matches in each log (grep exit 1 means no matches).
- XML parse: failed=0, inconclusive=0. The two skipped tests are GrassAndRingShaderPassesCompileOnGraphicsDevice and MetalComputeReadsZoneLanesPartitions65SeedsAndClearsRemovedInfluence; both require a graphics device.
- `grep -R -n -E <the six forbidden literal alternatives from COMMON> Assets/Render/Creatures Assets/Render/Contracts Assets/Scripts/Tests/EditMode/Render/Creatures Assets/Scripts/Tests/EditMode/Render/Contracts`: exit 1, zero matches, including this report and metadata. Used grep because rg is unavailable.
- `git diff --check`: exit 0. Unity-generated changes to unrelated importer metadata and ProjectSettings were restored; no such changes are committed.

The first full suite ran 444 tests: 440 passed, two failed, two skipped (exit 2, zero C# errors). Both failures were last-owner cleanup assertions; the same last-owner test passed alone (exit 0). Existing EditMode fixtures manually initialize components without the player lifecycle, so their teardown now invokes OnDestroy through TestHelpers before destroying fixtures, matching their manual initialization. The final full suite verifies that cleanup without resetting the owner count.

BLIND-SPOT: Headless verification does not render the scene on screen. Tube silhouette, shading/bloom, outlines, live chain coroutine timing, GPU submission cost, deferred runtime mesh destruction and player builds were not observed. Integrated stone/Stage fallback requires the out-of-ownership work above.
