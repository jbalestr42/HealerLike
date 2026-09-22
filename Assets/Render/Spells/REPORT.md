# T6 spell track report

Branch: zfc-spells. Base: frozen-contract wave 0 at 075b8be. The branch and .git/hooks/pre-push were verified before work. Origin was fetched at the start and again before the final commit. Look, Zones, Creatures and Stones integration documents were read using git show; no branches were merged or cherry-picked.

Implemented HLSpellGrammar, immutable recipes and compact style signatures, HLSpellStyleTable with collision rejection, HLSpellVisualSink, HLStatusObserver, primitive construction and motion, five requested effect prefabs, a wired sink prefab, shared torus/cone meshes and a default style table. Thirty-eight EditMode tests cover classification on plain data, expression provenance, ordered duplicates, invalid data, costs, collision variants, target/factory keying, anchors, outcomes, pulse forwarding, event reconciliation, cleanup, primitive geometry, motion and persistent asset references.

Only Assets/Render/Spells (plus its folder metadata) and the allowed Render/Spells EditMode test folder are committed. Unity's automatic importer/project-settings changes were restored after verification. No scene, gameplay data, original prefab or frozen contract changes are included.

## CONTRACT-CONFLICT

The frozen HLRenderRegistry has no zone-owner property. PulseArea validates input and calls an optional injected AreaPulse delegate. WAVE3-TODO.md provides the exact adapter for T3 HLZoneRegistry.AddPulse. The frozen target/factory status key also cannot express gameplay's independent source groups: known event records are aggregated, and unknown source is passed as null. See WAVE3-TODO.md for handler event gaps and topology limits.

Look assets are absent in this wave-0 clone despite the initial task's material assumption. The only placeholder is HLSpellPlaceholder.mat (and its runtime unassigned-material fallback). All affected references and their real Look replacement are listed in WAVE3-TODO.md.

## VERIFY

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-spells -quit -logFile /tmp/hl-spells-compile.log
# Exit 0; zero error CS matches.

/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-spells -executeMethod HealerLike.Render.Spells.HLSpellPrefabBuilder.Build -quit -logFile /tmp/hl-spells-assets-final.log
# Exit 0; regenerated all five effects and sink; zero error CS matches.

/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-spells -runTests -testPlatform EditMode -testResults /tmp/hl-spells-final.xml -logFile /tmp/hl-spells-final.log
# Exit 0; 236 passed, 0 failed, 0 skipped: 38 Spells and 198 pre-existing tests.
# Zero error CS matches. XML result Passed, failed="0".
```

Ownership, metadata, forbidden-name and staged whitespace audits passed. Classifier source contains no calls to consumer construction or resource resolution. Tests do not access gameplay singletons. Earlier full test runs passed 226 and then 235 tests before additional coverage and the final 236-test run.

BLIND-SPOT: No on-screen rendering or PlayMode camera review; shared Look shading, socket proportions, effect density, stage wiring, runtime event ordering and frame cost remain unobserved. Existing events cannot recover instant statuses, silent tag removals or causal multi-target links.
