# Procedural creatures (T4)

`HLCreatureBuilder` is the EntityModel `IVisualBehaviour` entry point. It builds one shared-material rig, keeps authored sockets untouched, and observes its owner's processed health contributions. Positive pre-clamp values (including overheal) produce local lime motes and `HLRenderRegistry.NotifyHeal`; signed nonzero values produce `SpellSink.ShowImpact`. Zero values produce neither. No singleton, targeting, damage, projectile movement, cooldown or resource mutation is introduced.

`HLCharacterView` renders the healer recipe at an explicitly authored anchor and registers the actual Character GameObject as the heal source. It never creates an Entity or calls Character.Init. `HLHealerCharacter.prefab` is a variant of the original Character prefab, with an `HLHealerAnchor` child. Stage integration supplies its final position relative to the board.

## Stage wiring

1. Use model variants in `Prefabs/` for the corresponding entity presentation. Each retains its original EntityModel, exact SkillSource subclasses/order/hierarchy, SkillTargetPointTag hierarchy, authored socket transforms, and colliders. Imported renderers, MeshFilters, Animators, and old IVisualBehaviour motion components are removed through prefab overrides. The original prefab assets remain dependencies of the variants; no original asset is modified.
2. Use the matching `HLProjectile*.prefab` variants for projectile presentation. All gameplay components/settings remain inherited. Renderers (including the required lightning LineRenderer) remain present but disabled. Each observer binds during Projectile.Init; it records individual contacts before inspecting any retargeted result in LateUpdate. `preserveContactPath` is authored for the two lightning variants. Channel is only a visual lifetime profile, not a claim of recurring gameplay ticks.
3. Let EntityModel.Init call the builder. Do not initialize EntityModel twice: its gameplay source list does not clear. Calling the builder Init repeatedly is safe.
4. Call `builder.Configure(registry, cellSize, groundOrigin, groundNormal)` with the actual terrain plane. Without configuration the rig uses its model origin as the foot plane; it does not assume the entity cell-center Y is terrain height. Configure projects the render root onto the supplied plane every LateUpdate, including after drag/spawn. It never moves the Entity or sockets.
5. Bind the Character view with `Bind(character, recipe, visualAnchor, material, registry, cellSize)` or supply its serialized references. Views also support the stage-owned `HLRenderRegistry.Current` when no registry is explicitly injected, including late publication/replacement. Only the stage assigns Current.
6. Replace `Data/HLPlaceholder.mat` with the shared look material (or update builder/view material references). Look assets were absent in this clone. The placeholder is instanced URP Lit, with per-renderer `_BaseColor` and optional `_EmissionColor` for lime buds. Call `HLPrimitiveMeshes.ReleaseAll()` only after all creature rigs have been disposed at render-bootstrap shutdown.

No scene, EntityData, skill-factory or original prefab wiring is changed by this track. T7 must select these presentation variants for them to become active in the game.

## Assets

| Recipe | Shape | Model variants |
| --- | --- | --- |
| HLHealer | Bulb on stem, torus crown, three luminous buds | HLHealerCharacter |
| HLSpiralFern | Curled stalk with alternating flattened fronds | HLNormal, HLFastShoot, HLRandomShoot |
| HLHangingArch | Tall open arch with three pendant pods | HLSwarm, HLTripleShoot, HLMultiShot |
| HLBladeRosette | Low hub with six outward leaf cones | HLHitArmorBuffer (Stage currently uses a stone; see WAVE5-REPORT.md) |
| HLSphereStack | Three offset spheres above a broad cone | HLTest, HLChainLightning, HLChanneling, HLSoldier |

The four recipes are authored primitive trees; each part has a pivot separate from its dimension-scaled geometry. Recipe socket positions are new-authoring hints only: runtime never overwrites the preserved prefab sockets with these hints. Legacy imported socket heights therefore remain substantially above the smaller bodies in some models. The visible shoulder is recipe-authored; its tip follows the actual invisible projectile even when the original source socket is above the body.

## Reach, timing and bounds

The existing entity data uses Range 100 (ordinary allies) and 1000 (Test, Swarm, buffer), while `Assets/Prefabs/Player.prefab` declares a 16 by 16 grid of one-unit cells. The proposed 24 x .2 chain only covered 4.8 cells. Shipped data instead uses 120 x .2 = 24 cells, enough for opposite current cell centers (15, 15 horizontal separation) plus four units of vertical difference. Rest links form ten compact exact-length coils. This is an art-data reach choice, not a promise to cover arbitrary Range 100/1000 targets outside the current board. Any farther target produces a fully extended chain and reports clamping; segment lengths, projectile motion and damage remain unchanged. Folded silhouette and renderer cost still require in-game review.

Arms use equal-link, allocation-free FABRIK, deterministic pole-plane seeding for degenerate chains, finite-input validation, bounded iterations, and explicit residual reporting. Reach goals use .10 seconds only for a cosmetic extension; observed projectile motion drives its own goal directly. Resolved heal/hit contact bypasses anticipation, remains for at least .04 seconds, then retracts over .20 seconds. Restoring the exact rest configuration completes recovery. Every solve projects fixed lengths; solved joint arrays are never simply interpolated as the final output.

Eight visual chains per creature is the hard pool cap, including temporary contact-to-contact branches. On saturation same-position contacts refresh an existing contact without sharing its lease token; additional contacts remain recorded by the projectile observer and health impacts still route independently; no gameplay projectile is removed. A source-owned chain survives projectile destruction long enough to show the contact and retract. Generation tokens prevent a stale end from retracting a new lease. A teleport cancels outstanding tokens and replants feet.

The original SlowTowerModel root scale .9 is preserved to retain socket placement. Rig generation compensates positive uniform ancestor scale; nonuniform or reflected ancestors are rejected. Generated geometry has no colliders. Authored gameplay colliders are intentionally preserved.

## Verification approach

EditMode coverage is under `Assets/Scripts/Tests/EditMode/Render/Creatures/`, in the existing test assembly that already references Render. Tests use TestHelpers and isolated Entity/Character fixtures without their gameplay Init methods. The projectile fixture safely calls the real Projectile.Init with empty buff/consumer lists, then invokes OnHit directly; the real ChainLightning coroutine is not run because it reads gameplay state. Tests cover immediate subscription, ordered hits, retargeting, renderer hiding, cancellation and listener cleanup.

Asset tests validate all recipes, build procedural geometry, compare every model variant's source/target sockets against its original, check retained colliders, confirm removed model renderers, and check every projectile variant's retained components and hidden renderers. Runtime camera composition, glow/bloom, simultaneous-chain readability and frame cost cannot be established by headless EditMode tests.

## Wave 5 simulation readouts

The generated body turns toward the first `TargetProvider.GetTargets()` root, with damped yaw;
its rest arms share that orientation. No target selects a slow cosmetic scan. The former
free-running sway/breath is replaced by health droop, event-driven hit shake, and cooldown swell.
Health uses current/max; enabled cooldown skills use `1 - cooldownProgress`, with the greatest
readiness driving the body. No cooldown skills means no charge. The generic public getter is
bound once per skill to a delegate; component enumeration each frame catches initial skills
(which arrive after model Init), AddSkillBuff, and removal. A projectile launch releases charge.

Builder and Character view implement the frozen `IHLDeliverySource`. The observer's serialized
`deliveryStyle` defaults to Direct; the spells track authors prefab overrides. Direct retains
fixed-length FABRIK. Arc uses an overhead segmented curve; Rigid telescopes straight and snaps
back in .045 seconds; Swarm thins individual projectile leases and accepts at most four live
leases; Bounce updates its straight tip between contacts; ChainSync collects simultaneous
contact-to-contact tips, holds until the projectile ends, then retracts together. Non-Direct
profiles telescope their visual segments so their tips track the simulation exactly. These are
cosmetic shapes and recovery times, not simulated cast phases. Thrown returns false. Legacy
`preserveContactPath` remains compatible with existing Direct lightning prefabs.

Character views discover ResourceAttributes before default-order Update, subscribe once, and
filter resolved contributions by the actual Character source. Positive registry/direct reports
are paired one-for-one within the frame; negative and zero resolved outcomes cast too. Disabled
views unsubscribe. Discovery includes resources on stones, not just creature models. It currently
scans the scene per frame because the runtime has no global resource-added event. `Bud0`, `Bud1`,
`Bud2` and `BudAnchors` expose generated presentation transforms for ShowLink callers. Bud base
colour as well as emission follows mana fraction, so the readout works with the shared primitive
shader even where emission is unsupported. This does not relocate gameplay sockets.
