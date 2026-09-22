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
