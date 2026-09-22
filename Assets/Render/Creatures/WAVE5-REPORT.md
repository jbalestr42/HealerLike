# D1 creatures — wave 5

Branch `zfc-dyn-creatures`, clone `/Users/fc/Documents/HealerLike-dyn-creatures`.
Confirmed branch with `git branch --show-current` and confirmed `.git/hooks/pre-push` exists.

Implemented targeting from the same public TargetProvider list as LookAtTarget; generated body
and resting arms turn together with damping. No-target scanning is cosmetic, as requested.
Removed autonomous body breathing/sway in favor of simulation readouts. Health current/max
controls droop and jade tint; negative processed health contributions trigger a decaying shake;
healing restores posture and colour. Enabled generic cooldown skills are enumerated at Init and
refreshed each frame, including skills added after Init and by AddSkillBuff. Public getter
bindings are cached; maximum readiness (one minus remaining fraction) swells upper parts and
brightens luminous parts. Missing/disabled skills contribute zero; launch releases charge.

Both source views implement IHLDeliverySource. The projectile observer discovers that interface
on the source model, with a serialized `deliveryStyle` defaulting to Direct. Contacts, lifetime,
and positions still come from the invisible gameplay projectile. Direct preserves the original
FABRIK behavior. Arc bends overhead; Rigid is a straight snapping rod; Swarm leases up to four
thin live tendrils; Bounce re-aims after contacts; ChainSync collects tips in the synchronous hit
loop, holds for projectile lifetime, and retracts together. Other profiles telescope their
render segments; original Direct fixed lengths and tests remain intact. Thrown is declined.
Legacy Direct lightning contact-path authoring remains supported.

Character views subscribe to ResourceAttribute outcomes across the scene, filter Character
source, and cast on signed outcomes. Positive direct/registry duplicate reports are paired
one-for-one, preserving repeated same-frame outcomes. Mana current/max drives bud colour and
emission. Public Bud0/Bud1/Bud2 and BudAnchors supply stable generated beam anchors until a rig
rebuild. Rebind/disable/destroy detach listeners. No Character.Init or gameplay mutation.

Added HLBladeRosette with six broad leaf cones around a low hub, assigned in the owned buffer
prefab and supported by creature asset authoring. The Stage mapping itself was not edited.

Intended mapping for the Stage owner:

| Model/entity | Recipe |
| --- | --- |
| Character healer | HLHealer |
| Normal, FastShoot, RandomShoot | HLSpiralFern |
| Swarm, TripleShoot, MultiShot | HLHangingArch |
| Test, ChainLightning, Channeling | HLSphereStack |
| HitArmorBuffer | HLBladeRosette if adopting the plant-buffer role |
| Soldier | Keep Stage's HLStoneSoldierModel enemy silhouette; owned HLSoldier plant fallback uses HLSphereStack |

This supplies four distinct plant recipes among the eleven Entity model choices when the
buffer mapping is adopted, plus the separate healer recipe and enemy stone silhouette.

## Not done and why

- Per-projectile prefab style values belong to the spells track; no guessed overrides authored.
- Stage/Environment changes and UpdateZone rename belong to wave 4; no zone-registry calls added.
- Thrown and enemy stone dynamics belong to the stones track.
- No on-screen composition, bloom, animation quality or performance capture was possible here.
- Character resource discovery scans once per frame, since no global resource-created event
  exists. A resource created and processed between discovery passes could escape its first
  direct callback; existing creature positive registry notifications remain a fallback.
- Cooldown is readiness, not a guaranteed imminent shot: failed requirements can leave a fully
  charged body. ConfigurableSkill's hidden steps do not expose ACooldownSkill progress.
- Original non-attacking allies from the wave 3 capture were not diagnosed by this render change.

## CONTRACT-CONFLICT

- The shared BRIEF has old spec-only write instructions. This build task explicitly authorizes
  implementation in this clone; its newer scope was used.
- The task says nothing under Assets/Scripts changes while also requiring Julien's tests.
  COMMON explicitly allows `Assets/Scripts/Tests/EditMode/Render/<Track>/`; only the existing
  Creatures test folder was changed there. No runtime scripts were changed.
- Stage currently maps HitArmorBuffer to HLStoneCairnModel. Changing that to the new plant-buffer
  recipe requires the Stage owner's choice; this branch cannot change the Stage mapping or
  stone assets. The intended recipe and owned creature variant are ready for integration.
- Frozen delivery contracts were sufficient and were not edited.

VERIFY:
- `git branch --show-current`: exit 0, `zfc-dyn-creatures`; `test -f .git/hooks/pre-push`: exit 0.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-dyn-creatures -quit -logFile /tmp/hl-d1-compile.log`: exit 0; zero `error CS`.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-dyn-creatures -runTests -testPlatform EditMode -testResults /tmp/hl-d1-tests.xml -logFile /tmp/hl-d1-tests.log`: final exit 0; zero `error CS`; 434 total, 432 passed, failed=0, 2 skipped. The skips are existing HLGrassGpuTests graphics-device checks under `-nographics`.
- First suite run: exit 2, two new fixture failures caused by Character/TargetProvider Reset on uninitialized objects. Fixed with TestHelpers.WithLoggingDisabled. The final full-suite rerun above passed.
- `grep -R -n -E "$HL_FORBIDDEN_PATTERN" Assets/Render/Creatures Assets/Scripts/Tests/EditMode/Render/Creatures`, using the COMMON forbidden-string expression: exit 1 (zero matches).
- `git diff --check`: exit 0 after restoring Unity's automatic unrelated importer/ProjectSettings changes. Final implementation changes are confined to Creatures and its allowed EditMode test folder.
- Implementation and tests committed together as `1613ef1`.
- `git push -u origin zfc-dyn-creatures`: exit 0; only the authorized branch pushed. Report follows in a separate documentation commit.

Verification logs and NUnit XML remain at the `/tmp/hl-d1-*` paths above in this clone's host environment.

BLIND-SPOT: Headless EditMode checks cannot establish the rendered silhouettes, motion readability,
bloom, or runtime scene-discovery cost; the parallel Stage/spells merge has not been exercised.
