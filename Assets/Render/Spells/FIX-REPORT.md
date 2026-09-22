# F2 Spells fix report

Branch: `zfc-fix-spells`; clone: `/Users/fc/Documents/HealerLike-fix-spells`.
Read COMMON.md, CONTRACT.md, BRIEF.md, review-render.md and the clone's CLAUDE.md.

## Findings

- **1 fixed.** Added `Editor/HealerLike.Render.Spells.Editor.asmdef`, restricted to Editor and referencing the render and gameplay assemblies. The builder assembly is asserted in its existing test. The only runtime reference to an editor API is the persistent-object guard inside `#if UNITY_EDITOR`. Added `HLSpellPrefabBuilder.VerifyPlayerCompilation` for actual StandaloneOSX player-script compilation and an assertion that spell authoring is absent.
- **7 fixed within Spells.** Disabled/inactive sinks reject SetStatus and ShowImpact, suppress their own area/link visuals, clear on enable/disable/destroy, and retain no live status children after disable. Zone requests still forward to their independently owned zone registry, preserving the existing Stage contract test. An observer notices a sink's clear/enable version and republishes unchanged live statuses when it returns. Registry-retention and re-enable tests cover this. The review's separate request to change Stage bootstrap ownership is outside this track.
- **11 fixed.** Sinks and initialized/built effects retain the shared primitive cache. Destruction releases a lease; static Release destroys generated torus/cone meshes and fallback material only after the last user. Effects also hold leases so deferred destruction and independently hosted effects cannot lose their shared resources when a sink dies. Persistent assets are exempt; the prefab builder persists mesh copies. Tests cover two sinks, a standalone effect, last-user destruction, repeated release, cache regeneration and persistent mesh survival.
- **14 fixed.** Observer grouping dictionaries and sink dead/removal scratch lists are reused. Empty reconciliation returns immediately; unchanged groups are not republished. Status effect arrays and target attributes are cached. Each effect reuses a property block, and unchanged side, signature, stack beads and shield state skip renderer updates. Shield charges remain observed by sink LateUpdate even if status timing is unchanged. Allocation tests use warmed `GC.GetAllocatedBytesForCurrentThread` deltas for empty/populated observers, empty/populated sinks and repeated effect updates.

## CONTRACT-CONFLICT — finding 6 remains unresolved

The finding is correct, but the requested reconciliation against BuffManager's public active-handler state cannot be implemented against this checkout. There is no public active-handler state or membership query. `Assets/Scripts/Buff/BuffManager.cs:62` stores handlers in private `_buffHandlerPerSource`; its nested group type is private (line 50). `GetBuffHandlerData` is private and creates entries when absent (lines 435–449), so it is not a read-only membership query either. Public BuffHandlerData fields describe a previously emitted record; they do not say whether the manager still owns it.

Tag removal and source cleanup do silently remove those private entries. The fix needs an allocation-free public query such as `bool ContainsHandler(BuffHandlerData data)`, or a public copy-into-caller-buffer API, in gameplay's BuffManager. That file is outside the allowed folders. No gameplay file or frozen contract was changed, no private-field reflection was introduced, and duration expiry was not substituted for actual membership. A fake-only implementation would leave the production bug intact, so no such fix or passing reconciliation test is claimed. Infinite statuses can still remain after a removal that emits no stop event.

No other ranked findings were changed: they belong to other tracks or exceed the assigned subset.

## Verification history

The initial plain compile exited 0. The first full suite exposed a property-block initializer error in the new code and the Stage inactive-sink pulse expectation; both were corrected before final verification. That diagnostic run exited 2 (417 passed, 32 failed, 2 skipped). A second diagnostic run exited 2 (446 passed, 5 failed, 2 skipped): lifecycle tests needed explicit runtime callback invocation in EditMode. Fixtures now use TestHelpers for those messages, and editor teardown explicitly releases effect resources. The final counts below supersede both runs.

VERIFY:

Commands used Unity `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity` as `$UNITY`, and this clone `/Users/fc/Documents/HealerLike-fix-spells` as `$PROJECT`.

| Command | Exit | Result |
|---|---:|---|
| `git branch --show-current` | 0 | `zfc-fix-spells` |
| `"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -quit -logFile /tmp/hl-f2-compile-final2.log` | 0 | Zero C# compiler errors |
| `"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults /tmp/hl-f2-editmode-final2.xml -logFile /tmp/hl-f2-editmode-final2.log` | 0 | 453 total; 451 passed, **0 failed**, 2 skipped |
| `"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -quit -buildTarget StandaloneOSX -executeMethod HealerLike.Render.Spells.HLSpellPrefabBuilder.VerifyPlayerCompilation -logFile /tmp/hl-f2-player-final.log` | 0 | Actual player scripts compiled; render DLL produced; spell editor DLL excluded; zero C# errors |
| `grep -n 'error CS'` on each of the three final logs | 1 each | 0 matches each |
| `grep -R -n -E` with the six forbidden literals from COMMON.md, over `Assets/Render/Spells` and `Assets/Scripts/Tests/EditMode/Render/Spells` | 1 | 0 matches |
| `git diff --check` | 0 | No whitespace errors |
| `git push -u origin zfc-fix-spells` | 0 | Only the authorized branch pushed |

The two ignored tests are the pre-existing Grass GPU/shader tests that require a graphics device; no Spells test is skipped. All allocation assertions passed with zero bytes in the measured warmed loops. Unity-generated changes to project settings and unrelated imported metadata were restored. Only Spells and its allowed tests are committed.

Implementation commits: `960feed` (1), `bdd1ede` (7), `9a0d132` (11), `ce4b121` (14 and verification/lifecycle follow-ups for 1, 7, 11).

BLIND-SPOT: Nothing was rendered on screen. EditMode and player-script compilation do not prove live rendering, play-mode deferred-destruction timing, or native lifetime behavior across domain reload. Finding 6 remains blocked by the missing public gameplay membership API.
