# Spell grammar and stage integration

T6 implements a data-derived grammar and the frozen IHLSpellVisualSink. Everything lives in this folder; EditMode tests live in `Assets/Scripts/Tests/EditMode/Render/Spells`. No scene, gameplay data, gameplay prefab or frozen contract is changed.

Use `HLSpellGrammar.DescribeData(data, context)` for pure classification. It accepts plain consumer, modifier, handler, character skill, entity skill and step data, or their factory references. Describe overloads accept runtime skills, handlers and projectiles for read-only adaptation. Data graphs preserve ordered duplicates, value owner/expression, destination, flags, multiplier, operator, stack law, lifetime, period, clock, side and delivery configuration. Unknown inputs and the broken Slow/Time constructors are diagnostics. No consumers/buffs/skills/validators are instantiated. Preview values never trigger visuals.

`HLVisualRecipe` owns a copied, read-only child list. `HLSpellSignature` is the compact style key: operation, sign, destination AttributeType, topology, duration shape, tempo, optional byte variant. The operation axis separates prevention from resource/max-health and attribute changes. Full provenance and delivery data remain in the recipe; compact style equality deliberately allows semantic vocabulary reuse. `HLSpellStyleTable` maps keys to prefab references and reports duplicate entries; ambiguous lookups fail closed. Variant zero is the default, and variants are intended only for art notches, not mechanical changes. No display names, descriptions, icons, unique IDs, or prefab filenames participate in classification.

`HLSpellVisualSink` emits lime spheres for positive health and coral stars/cones for negative health, with an extra paired ring for confirmed criticals. Mana uses hollow gold beads. Zero/nonfinite outcomes are silent. Scale follows the specified bounded square-root amount/max law (100-unit fallback only for isolated targets without health). Effects cap at 128 live requests and expire cosmetically. Status roots are keyed by target/factory and attach to Entity.targetPoint, falling back to the target transform. Repeated SetStatus updates stack/timing fields and beads; RemoveStatus and cleanup release the same root. Compound effects retain individual markers. Observed HitArmor controls counted plates, PercentArmor leaves open seams, prevention closes the bud; no zero-delta block reason is inferred.

`HLStatusObserver` subscribes through IVisualBehaviour.Init(Entity), independent of the creature builder. `Bind(manager, sink)` also supports explicit injection and tests. See WAVE3-TODO for exact stage wiring, the missing frozen zone accessor, and unavoidable event limitations.

Prefabs contain primitive meshes only, no colliders or gameplay components. `HLSpellPrefabBuilder.Build` reproduces the five effects, torus/cone mesh assets, style table and sink prefab through Unity batchmode or the HealerLike menu. Persistent meshes are stored in Data. Runtime primitives share meshes/materials, and per-renderer colour uses property blocks. No image generation or imported art is involved. Shader integration is deferred to the shared Look material swap documented in WAVE3-TODO.

## VERIFY

Commands run with Unity 6000.6.0f1 on this clone only. See REPORT.md for final commands, exit status and counts.

BLIND-SPOT: No rendered frame or PlayMode camera review was performed; integrated timing, density, outlines, fog, socket sizing and performance need stage review.

## Wave 5 integration

`HLSpellPrefabBuilder.Build` now enumerates deserialized `Assets/Data` through
`HLSpellGrammar.DescribeData`, writes `SIGNATURES.md`, and creates one persistent primitive
prefab per key. The inventory includes composition nodes, standalone factory contexts, self
handler contexts, and diagnostic intent. Runtime invalid recipes remain silent and log once;
unmapped keys in a configured style table fail closed. The four signed health/mana resolution
keys supplement the data inventory because the frozen outcome seam carries no original signature.

`HLDeliveryStyles` is an authored asset at `Spells/Data/HLDeliveryStyles.asset`. Inject this asset
into the delivery observer and call `styles.For(projectilePrefab)`; original, stage `HL` names and
`(Clone)` suffixes resolve alike. Unknown/null names return Direct. Component facts are retained
in each row. Both lightning variants are ChainSync. Laser and swarm are presentation variants
(their underlying behaviours are arc/homing and curved respectively). Bounce is supported by
the classifier; none of the nine base prefabs carries it. An item-added bounce must be selected
by the observing track from the installed behaviour, rather than guessed from the base prefab.

`HLSpellVisualSink` queues positive health outcomes from a Character and flushes one gold curved
link per source/recipient in LateUpdate. Dots travel for the cosmetic 0.6-second lifetime.
`HealerAnchor` accepts a render-owned bud anchor adapter; absent that adapter it uses the source
transform. `IsCharacterSource` and `LinkObserved` allow tests with plain objects. Single-recipient
positive outcomes also link, as requested; the frozen seam cannot identify the originating skill.

Status orbits and drip phases read `SetStatus` elapsed/duration and the grammar's authored period.
Three rings have separate tilts and radii. Plates close over the first 0.25 seconds of observed
status elapsed time; removal detaches a cosmetic tail that opens over 0.25 seconds. Refreshes
follow the supplied elapsed reset. Stacks remain counted beads, not an inferred stat magnitude.
Resource outcomes retain the bounded square-root magnitude scale. Round-end/death listener
markers stay still until an actual outcome arrives. Speed markers sit 0.35 units below the anchor.

Creature integration: subscribe to `HLSpellEffect.OnTint` (`UnityEvent<Color>`) on the target's
status children, then read `Tint` immediately to catch late subscription. Negative periodic
resource statuses emit coral, removal/disable emits white (release sentinel). The creature owner
must combine overlapping tints and restore its own body palette when releasing; this event does
not directly overwrite shared materials. No new tint contract or creature code is introduced.
