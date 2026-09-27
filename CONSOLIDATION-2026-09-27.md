# HealerLike consolidation — 27 September 2026

Continue development in `/Users/fc/Documents/HealerLike`, on `zfc-render`.

## Integrated history

- Previous original project: `2d898737` (spell composition, grammar icons, grass reactions, live creature visuals, local HUD/font/selection work).
- `b331803a`: spell-owned travelling fragments and connected chain/beam paths. Caster anatomy supplies a launch point; creature rigs no longer choose or claim projectile geometry. Healing outcomes retain a creature response without growing a delivery arm.
- Merge `69e7742707dbc3dca9b8141baf1409cf2f6e3156` includes `origin/main` at `c924236b`: Julien's 32 newer gameplay commits, enemy roster, waves, sandbox controls and gameplay fixes.
- Audit of all 47 branch references in the original plus five working clones found no missing commit history after this merge. Compact UI, selection facing, spell sources, Android 0.1.6/0.1.7, spell composition and feature/new-enemies are covered. Full ref/commit mapping is in `branch-audit.json` in the archive.
- Merge compatibility: two Toolkit test doubles implement the new abstract item description property. The grammar reads summon cooldowns, mana-drain payloads and HealingReceived defence modifiers. Asset pinning now covers 31 entity assets, MortarShell and all 15 newly introduced handlers; existing assertions remain in place.

## Ownership and scope

Delivery style, payload colour, travelling fragment and chain/beam path come from the spell. Travelling shots snapshot the cast outlet and survive source movement/recomposition/removal; connected paths retain an anatomical source while active, then release and fade. Authored geometry lives in DeliveryVocabulary. Existing creature arm/stone gesture utilities and their older diagnostic fixtures remain available, but the runtime projectile observer no longer invokes them.

The original project's pre-existing local settings, generated icon catalog and plugin metadata changes are retained. Clone-specific build/settings changes are backed up, not applied over the original project's settings. Latest generated test icons are reproducible import output, not new authored spell grammar.

## Recovery archive

`/Users/fc/Documents/HealerLike-archive/2026-09-27-consolidation`

For every repository: verified `all-refs.bundle`, branch/worktree/status inventory, binary tracked and staged patches, working-file copies with SHA-256 manifest, ignored-file inventory, UserSettings, Logs and build output. The previous original-project stash and backup branch are retained. The earlier merge backup and validation results, plus the spell-composition handoff, are preserved here.

Restore committed history with `git clone <repository>/all-refs.bundle <destination>`. Restore selected local file copies from `working-files/`, consulting `working-files.json` for deleted files; the binary patches preserve the index/worktree distinction. Unity Library/Temp caches and generated IDE projects are rebuildable and excluded from the archive.

## Validation and cleanup

- Full Unity 6000.6 EditMode run: **3,678 passed, 0 failed, 0 skipped**, including opt-in rendering/portrait checks.
- Targeted integration run: **261 passed**, including every live handler, all projectile prefabs and both sides of the expanded creature roster.
- Delivery C# files remain below 300 lines; source diff whitespace checks pass.
- Native battle and source capture passed on `1c214acd`. Reviewed battle HUD and travelling/chain spell images; launch and support attachment error was zero through source recomposition. Full screenshots and proof JSON are archived under `validation/evidence/spell-sources`.
- Visual follow-up: long roster names such as Channeling wrap awkwardly on narrow cards; dense grass still competes with combat silhouettes. These remain polish observations, not hidden validation claims.
- All integrations are local; remote branches have not been deleted or rewritten. The original project's Git branch stays `zfc-render`; `main` is not modified.

## Completed cleanup

The original project was fast-forwarded to the validated integration and its pre-existing local edits were verified byte-for-byte. Removed the five working clones (`HealerLike-compact-ui`, `HealerLike-spell-sources`, `HealerLike-android-016`, `HealerLike-android-017`, `HealerLike-spell-composition`), `/private/tmp/healerlike-integration`, and the handoff folder after verifying its archived copy. Every removed clone's HEAD is an ancestor of the original project's consolidated history. Timestamped actions are in `cleanup-actions.jsonl` in the archive.

Only `/Users/fc/Documents/HealerLike` remains as an active project. Keep using `zfc-render` there. Recovery bundles and evidence live in the sibling archive folder; no remote publication or branch deletion was performed.

## Publication requested

Following the user's request to commit and push everything, the remaining original-project changes are included: generated data icons and catalog entries, Unity platform icon settings, quality settings, and deletion of orphaned Sirenix demo metadata. Publication target: `origin/zfc-render`, including the complete consolidated history. The earlier no-publication notes describe the state before this request. No gameplay source changed after the 3,678-test validation.

## Visual polish and icon recovery

- Roster titles use 9px single-line labels with ellipsis for overflow. Channeling now fits without splitting its final letter; spell labels retain their two-line layout.
- Combat grass height is 0.45 in the scene, prefab and authoring code (previous scene value 0.6); the surrounding meadow keeps its full height.
- The open Editor had a stale EffectVocabulary instance with zero table entries despite all 18 entries being present on disk. Unloading and reloading that asset restored the table. A normal play session after removing diagnostics retained the table and displayed grammar spell icons without EffectComposer errors.
- SpellIcons now caches failed compositions until invalidation, matching its existing failed-capture behavior. A regression test covers repeated requests, shared factory/data keys and recovery after vocabulary repair and invalidation.
- Validation: **276 spell EditMode tests passed, 0 failed, 0 skipped**. Live portrait preparation checked in Unity; Editor returned to Edit mode. Results and runtime log are at `/private/tmp/healer-polish-tests.xml` and `/private/tmp/healer-polish-runtime.log`.
- Publication includes the polish, cache fix, regression test and all regenerated icon/catalog changes requested by the user. Temporary diagnostic scripts and probe assets were removed.
