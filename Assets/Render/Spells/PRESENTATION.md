# Spell presentation

The shipped vocabulary contains fourteen procedural compositions. `ElementEntry` owns parts,
count rules, socket, motion, presentation envelope and ground response. `EffectPresentation`
controls entrance, release, scale, head clearance, beam width and marker behavior without
changing gameplay timers. `SpellPolishVocabulary` is an explicit authoring command, never an
import hook; editing the saved vocabulary does not require changing runtime code.

`SpellLooks.Compose` derives one layer per buff in declaration order. Compatible held layers
share a view; ticking handlers retain their own phase owner. A `SpellLook.recipe` replaces the
whole composition with an owned snapshot. To bake one, select a `SpellStudioPreset` and use
**Tools > Render > Bake Selected Spell Recipe**. Additional recipes retain independent parts,
motion and clocks. Validation rejects cycles, invalid attachments and aggregate budgets above
256 parts before allocating geometry. Procedural meshes belong to the effect and are disposed
with it. Attachment resolution uses the same fragment resolver as creatures.

`SpellGround.Play` publishes one-shot reactions and `SpellEffect.BindGround` owns held reactions.
Every composite layer participates. Removal, target loss and deactivation release held handles;
Ground owns their release envelope, bounds and recovery. Moving ring state is an annulus, so an
impact colours the moving front instead of painting a filled disc. The rendered simulation uses
the configured GPU GroundSimulation; CPU tuft response is its testable mathematical mirror.

`CreatureEvolution` observes initialized runtime attributes, coalesces changes after simulation,
and rebuilds only when derived appearance channels change. It preserves the rig/root and authored
recipe choices. Health maximum changes mass; current health loss does not. Removing improvements
restores derived features. Attribute observers are released on rebinding and destruction.

For repeatable art review, **Tools > Render > Capture Polished Spells on Grass** records all fourteen
entries at three phases through real spell meshes and GPU grass. These are presentation fixtures,
not gameplay casts. `SpellSourceRun.Capture` and the compact stage capture exercise gameplay views.

## Composition and icon audit

Optional reach, delivery, trigger, side and origin entries in `EffectVocabulary` append authored
pieces in that order. Each keeps its own socket and motion; the full tree shares the 256-part
budget. Empty tables preserve existing art. Distinct child compositions cannot share a pooled
status accidentally; periodic descendants retain their handler's clock owner.

`SpellIconDerivation` is the pure boundary for character skills, consumers and handlers.
`SpellIconComposer` uses the same vocabulary and complete `SpellLooks` overrides as the world.
`SpellIconRenderer` poses private recipe copies, renders native geometry to a transparent texture,
and releases all temporary effects and procedural meshes explicitly. UI-only surface settings
quiet hatching and improve dark-glyph contrast without modifying the world material. The seal
shows reach, held/periodic tempo, non-cast triggers and item origin when supplied by the context.
Compound recipes display up to four glyphs with an overflow marker.

`StageIcons` owns creature and spell providers. `SpellIcons` caches by source identity, shares
factory/data requests, caps owned textures at 128, and releases them on invalidation or disposal.
The shared Toolkit has one optional `IToolkitDataIconProvider` extension; null results retain its
existing catalog fallback. Only that interface, the lookup in `ToolkitGameView`, and its test
extend the earlier Render-only scope. No gameplay data or generated catalog is rewritten.
Use **Tools > Render > Capture Grammar Spell Icons** for an atlas and source-path manifest.

Percentage buff magnitudes use fraction units directly (0.2 means twenty percent). Additive
modifiers use the documented 100-point reference when no target attribute baseline is supplied.
Live creature attribute interpretation lives in `Grammar/LiveUnitDerivation`; observers only
collect state and apply the derived channels. Init validation, child loss, billboard propagation,
delivery shape ownership and disabled EditMode preview cleanup have dedicated regression tests.
