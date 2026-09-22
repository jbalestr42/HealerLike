# Wave 3 integration

- Material placeholder: `Data/HLSpellPlaceholder.mat` is instanced URP Lit. All five requested effect prefabs plus `Prefabs/HLSpellVisualSink.prefab` use it. Swap their effect/sink material fields and child renderer materials to `Assets/Render/Look/HLLook_Default.mat` (T1 GUID `f647ba1fbb054b24bbb73180b0a65d58`). The runtime fallback `HLSpellFallback` also uses URP Lit if no material is supplied. Set the sink material to avoid that fallback. Runtime colours use `_BaseColor` property blocks. No external prefab placeholders exist.
- Instantiate `Prefabs/HLSpellVisualSink.prefab`; set the stage's `HLRenderRegistry.SpellSink` to its component. Stage owns registration and unregistration.
- Add `HLStatusObserver` beneath each active EntityModel BEFORE its normal Init walk, or call `Init(entity)` explicitly after adding it. The creature builder has no dependency on it. Keep every gameplay `buffEffect` slot empty.
- Bind the actual T3 owner: `sink.AreaPulse = (center, radius, kind, strength) => zones.AddPulse(kind, center, radius, strength, 0.8f);`. Clear this delegate on stage teardown. No zone owner exists in this branch; no fake owner or duplicate buffer is supplied.
- Review geometry under the integrated camera and look shader: tilted gold tori, leaf plate closure/open seams, lime rising spheres, coral stars/cones, curved gold beam/dots, status markers and side rims. Tune sizes with the preserved target sockets. Verify observer attachment precedes handler starts.
- `ShowLink(start, end)` is an explicit cosmetic connection hook. Multi-target healing currently resolves simultaneous independent recipients; an integration with confirmed source/recipient data may draw a fan. Do not infer sequential bounce links or a heal area from aggregate outcomes.

# CONTRACT-CONFLICT

The requested `HLRenderRegistry` zone owner does not exist in the frozen contract or its implementation. Its only mutable sink property is SpellSink. The T3 branch supplies `HLZoneRegistry.Current` and AddPulse, separately. To preserve the contract and compile against wave 0, this track exposes an injected AreaPulse delegate instead. The binding above is the concrete stage adapter; PulseArea is null-safe without it and forwards the exact radius with validated/clamped strength when bound.

The frozen status key excludes source although gameplay groups by source/factory. The observer aggregates known event records by target/factory (sum stacks, minimum elapsed, maximum duration), reports source as null, and preserves the shared visual until the final observed group stops. Exact independent ownership/timers cannot fit that seam. No private gameplay dictionary is inspected.

# Observation limits

Started fires before first stack processing, so pending refreshStacks is included until the same mutable event record is reconciled in LateUpdate. Concrete BuffHandler.durationTimer supplies elapsed time. No resource pulse is emitted from status timing. Unknown/invalid factory data produces diagnostics and no status geometry. Instant handlers emit no start event; no status is invented for them. Tag removal silently drops private records without a stop event, and late observer attachment cannot enumerate existing handlers: those remain gameplay seam limitations. Disable clears visuals but keeps event subscriptions to track lifecycle; re-enable reconciles retained public records. Destroy/Detach removes listeners.

Aggregate resource callbacks have no causal skill/phase ID. The sink intentionally does not guess a multi-heal or chain from adjacent callbacks. The classifier's unmitigated PreviewAmount is intent only: defenses, prevention, criticals, and clamping can change results. Unsupported data is explicitly invalid. Dormant skills are described without executing them. Legacy gameplay defects and data references are unchanged.
