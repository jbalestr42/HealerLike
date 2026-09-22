# F4 stones fix report

Branch confirmed as `zfc-fix-stones`; clone `/Users/fc/Documents/HealerLike-fix-stones`.
Read COMMON.md, CONTRACT.md, BRIEF.md, review-render.md and the local CLAUDE.md.

- Finding #13: disabling an effects component or its GameObject clears live fragments, destroys detached mesh copies, hides pooled objects and releases its global slots. All four emission entry points reject inactive owners before allocating or consuming collapse state. Scene lookup deliberately retains the disabled owner, so it cannot bypass disablement. Assets and the hidden pool remain reusable until destruction. Tests cover both disable modes, other owners' slots, re-enable reuse, every emission entry point and scene lookup.
- Finding #17: cache the LookAtTarget reference when Initialize resolves BodyPivot; moving LateUpdate uses that reference. Tests exercise existing, removed and newly added LookAtTarget components, and cache refresh on reinitialization. Changing pivot components requires reinitialization.
- Finding #5: Generate refuses before touching gameplay unless its own serialized `demoSceneOnly` flag is explicitly enabled. Default remains false, including existing prefabs. README prohibits production attachment from calling this API. Tests cover the default refusal, opted-in argument validation and the existing generation fence lock.
- Moved all four wave-5 test sources and their metadata into `Assets/Scripts/Tests/EditMode/Render/Stones/`; retained namespace `HealerLike.Render.Stones`, removed the old asmref and folder metadata. Tests reuse TestHelpers and do not access gameplay singletons. Effects fixture teardown explicitly dispatches OnDestroy because EditMode does not automatically send that message to these instances.

No assigned finding was rejected as wrong. Finding #5's Stage bootstrap caller and the broader gameplay generation integration are outside Stones ownership: unchanged, so any unopted Stage invocation now refuses intentionally. Other numbered findings and review notes were outside the assigned fixes and were not changed. No prefab was opted into demo generation automatically.

The first full suite had two new teardown-assertion failures (435 passed, 2 failed, 2 skipped), with zero C# compiler errors. Those exposed the EditMode message-dispatch issue above; fixture cleanup was corrected before the final full run. Unity imported unrelated bundled assets and updated project settings; those generated tracked changes were restored and excluded from commits.

VERIFY:

```sh
git branch --show-current
# Exit 0: zfc-fix-stones.
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-fix-stones -quit -logFile /tmp/hl-f4-stones-compile-final.log
# Exit 0.
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-fix-stones -runTests -testPlatform EditMode -testResults /tmp/hl-f4-stones-editmode-final.xml -logFile /tmp/hl-f4-stones-editmode-final.log
# Exit 0: 439 total, 437 passed, 0 failed, 2 skipped; Stones 64/64 passed.
grep -c 'error CS' /tmp/hl-f4-stones-compile-final.log /tmp/hl-f4-stones-editmode-final.log
# Exit 1 (no matches): 0 in each log.
grep -m 1 'failed=' /tmp/hl-f4-stones-editmode-final.xml
# Exit 0: failed="0".
git diff --check
# Exit 0.
```

Case-sensitive `grep -R -n -F` with all six prohibited literals over both owned trees: exit 1, zero matches. Test relocation preserves all four source GUIDs; no old test folder or asmref remains. Only Stones and its permitted EditMode test folder are committed. Implementation commits: `4601494` (#13, #17, #5 and relocation), `b51dcd0` (#13 fixture teardown).

BLIND-SPOT: Nothing was rendered on screen. PlayMode lifecycle timing, deferred native destruction, player builds and camera appearance were not exercised. Two pre-existing grass GPU tests require a graphics device and were skipped by the required headless run. The out-of-ownership Stage invocation must be removed or explicitly designated a demo fixture by its owner.
