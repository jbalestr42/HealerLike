# Creature Studio

Open **Tools → Render → Render Studio** or **Tools → Render → Creature Studio**. The header switches between creature and spell authoring.

Creature Studio edits real `CreatureRecipe` assets consumed by the renderer. Its isolated 3D preview uses `CreatureRig`, baked meshes and the project's shader, with neutral studio lighting. No Play mode is required.

![Creature and spell previews](CreatureStudio-Overview.jpg)

## Author a creature

- Start with a Healer, Sprout or Stone Sentinel draft, or select an existing recipe.
- Select a part and edit its primitive, role, parent, transform, colour, glow and mesh variant. Add and duplicate parts to build a silhouette. Removing a part removes its subtree and remaps surviving parents, arm bindings and sockets.
- Tune roots, idle sway/breathing, arms, and the visual source/neck sockets. Add an arm on the selected part, then use **Rebuild rest pose** after changing segment lengths/counts; paired arm sockets are maintained by the add/remove tools. Validation explains malformed recipes instead of feeding them into the renderer.
- Play or scrub the preview; inspect health, charge and glow readouts. Orbit, pan and zoom the camera, or reset its framing.
- Save as a reusable recipe, duplicate it for a variant, or export the preview as a PNG. Undo/Redo is supported. Double-clicking a saved recipe opens the studio.

Recipe assets affect the game only when assigned to a renderer consumer such as a CharacterView. The studio does not alter entity balance, skill logic or game scenes.

## Preview spells on your creature

In Spell Studio, use the **Creature** field beneath the timeline to choose your saved recipe. None uses the original healer. The **Edit** button opens that recipe in Creature Studio. Effects use its actual body, head and source anchors.

Saved creature examples live in `Assets/Render/Creatures/Data/StudioSamples`; the samples menu creates missing examples without replacing existing work.

## Verification

Run the EditMode filters `CreatureStudio` and `SpellStudio`. To produce visual comparisons in graphics-capable Unity batch mode:

```
-executeMethod HealerLike.Render.Creatures.Editor.Studio.CreatureStudioCapture.CaptureAll
```

Captures are written to `Logs/CreatureStudioCaptures/`.

Verified in Unity 6000.6.0f1 on macOS Metal: **988 passed, 0 failed, 3 skipped** across the renderer and both studio assemblies. All **32 Creature Studio** and **65 Spell Studio** tests passed. The three skips are existing opt-in screenshot fixtures. Captures of all three creatures and their spell combinations were visually inspected. Manual mouse/keyboard acceptance testing remains unavailable because Computer Use permissions were not granted. Full results: `Logs/RenderStudio-Tests.xml`.
