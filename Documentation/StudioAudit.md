# Studio audit — 24 September 2026

Reviewed the studio changes against `d7e3be7`, including runtime grammar parity, editor saving and Undo, draft persistence, cross-studio previews, PNG export, asset references and repository hygiene.

## Corrections

- Native spell overrides bypass unused source derivation. Incomplete handler data produces a diagnostic instead of breaking editor repaint.
- Creature validation follows the composer's reduced head count and validates only settings consumed by the selected side and root configuration.
- Empty projectile override rows show repair guidance.
- Saving a studio asset no longer flushes unrelated dirty assets.
- Grammar edits preserve inspector scrolling, and reopening preserves the selected creature override table.
- Baking explicitly identifies the generated grammar output when an override is being previewed.
- Unsaved creature edits notify the spell preview.
- PNG export preserves framing and restores camera settings. Failed rendering releases the capture texture.

These changes include focused regression tests. Production gameplay scripts, grammar composers, packages and project settings were not changed.

## Verification

Unity 6000.6.0f1, macOS Metal: **1,298 passed, 0 failed, 3 skipped** in the full EditMode run:

| Assembly | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Gameplay | 207 | 0 | 0 |
| Renderer | 919 | 0 | 3 |
| Spell editor | 126 | 0 | 0 |
| Creature editor | 46 | 0 | 0 |

A focused rerun after the final creature camera-state assertions also passed all 46 creature editor tests; report: `Logs/Studio-Audit-Creature-Tests.xml`.

The skips are existing opt-in screenshot fixtures. Full report: `Logs/Studio-Audit-Tests.xml` (local, ignored).

Added assets have metadata, no duplicate GUIDs involving the additions, and no missing preset GUID references. Whitespace checks pass. Unity-generated importer, demo metadata and settings changes are restored after the editor exits; those unrelated changes are excluded from commits.

Preview captures were inspected during implementation. Manual mouse/keyboard acceptance testing remains unverified because Computer Use access was unavailable. Automated rendering and camera-state tests cover both preview implementations.
