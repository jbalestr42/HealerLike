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
