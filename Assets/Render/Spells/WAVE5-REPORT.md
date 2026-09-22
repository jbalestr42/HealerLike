# Wave 5 — D3 spells

Branch `zfc-dyn-spells`, clone `/Users/fc/Documents/HealerLike-dyn-spells`, base `b6b6773`.
Implementation and tests: `6c9b6f3`. Initial branch check printed `zfc-dyn-spells`; the pre-push
hook exists. Only Spells and its allowed EditMode test folder are committed. Frozen contracts,
gameplay, Stage, Environment and zone ownership are unchanged. No registry Update call was added.

## Delivered

- `HLSpellPrefabBuilder.Inventory` walks deserialized Assets/Data and invokes
  `HLSpellGrammar.DescribeData`, retaining asset paths per signature. `SIGNATURES.md` contains
  88 distinct authored keys, including composition nodes, standalone factories, self handler
  contexts and diagnostic intent. Each key has a persistent primitive prefab. The table has
  90 entries: those 88 plus two extra signed mana outcome keys required by the resolution seam.
  Equal semantic keys share vocabulary; asset names never choose spell payload shapes.
- Shape selection covers spheres for healing, spikes for damage, three tilted tori for stat/rate
  effects, plates for armour/prevention, hollow mana markers, and periodic drips. Help cores are
  lime/gold, harm cores coral, enemy ownership rims slate. Scale for resolved resource outcomes
  keeps the bounded square-root amount/max law. Stacks use countable beads; charge shields use
  observed HitArmor. No stack count is represented as a measured stat delta.
- The nine-entry `HLDeliveryStyles.asset` records projectile component evidence. BulletSpeed is
  Direct; ChainLightning and ChannelingLightning are ChainSync; the three Curve variants are Arc;
  LaserBullet and StraightLaserBullet are Rigid; SwarmBullet is Swarm. The pure classifier also
  supports Bounce. The lookup accepts original, stage-prefixed and clone names.
- Positive Character health outcomes are paired per source/recipient within the frame, deduplicated
  at the endpoint, then call ShowLink in LateUpdate. Curved gold segments and travelling dots hold
  for 0.6 seconds. The default start is the Character transform; an injected HealerAnchor can supply
  the bud socket after merge. Recipient anchors use Entity.targetPoint with transform fallback.
- Status poses read supplied elapsed/duration, and periodic drips read the recipe period. Three
  rings have distinct radii and tilts. Plates close over 0.25 seconds of observed elapsed time,
  then open in a 0.25-second cosmetic tail on removal. Speed markers sit low. Refresh resets follow
  the observer's elapsed reset. Round-end/death listener resource markers do not invent ticks.
- `HLSpellEffect.OnTint` is a public UnityEvent<Color>; `Tint` exposes current state for late
  subscribers. Negative periodic resource markers request coral; removal/disable requests release
  with white. README documents overlapping-tint composition and restoring the creature's palette.
- Area events now create a central expanding ring with the exact supplied radius as well as the
  existing cosmetic zone pulse. Group effects are rendered once at each actual recipient, with no
  fabricated spatial radius. Unmapped keys in configured tables fail closed and warn once per key
  per sink; invalid recipes also remain closed.
- Tests cover every new logic class and significant added behavior: inventory coverage and persistent
  geometry, delivery classification/lookup, fake Character pairings, travelling link dots, elapsed
  status poses, period-driven drips, tint release, plate opening, Speed placement, radius, magnitude
  scaling and unknown-key logging. Production logic and these tests share the implementation commit.

## Not done, and why

The delivery observer, creature body and stage bootstrap are outside this track. They must inject
HLDeliveryStyles into the observer, optionally provide HealerAnchor, and subscribe to OnTint. Actual
body tint is therefore an exposed and tested hook, not a claim that the creatures already recolour.
The Character's missing status observer/heal pulse from wave 3 remains a stage/creature integration
item. No graphics capture or in-game judgement of size, density, fog or palette was performed.

Bounce installed by an item is runtime state, not a property of any of the nine base prefab mappings;
the observing track must override from the installed behaviour. LaserBullet uses an arc behaviour,
and SwarmBullet a curved behaviour: their Rigid/Swarm presentation is explicitly authored as requested,
not a claim that the underlying projectile simulation differs. Both lightning variants are synchronous,
including the cosmetically named channel variant; no invented periodic channel damage is shown.

## CONTRACT-CONFLICT

The frozen ShowImpact seam has source, recipient, resource, signed amount and critical flag, but no
original signature, skill, topology, expression or period. It cannot select every authored contextual
prefab at resolution time. The sink uses the signed resource outcome key, emits per recipient, and
uses status grammar for maintained effects. Inventory coverage is not a claim of restored missing
causal data. The Character link rule consequently includes single-recipient heals as requested; it
cannot distinguish two same-frame single casts from a group skill.

BuffManager.BuffHandlerData exposes target, stacks and handler timers but no source. The existing
observer sends null source and merges source groups. Status ownership therefore remains neutral when
unavailable; it is not guessed from the recipient. Supplied sources do select ownership rims.

Status signatures likewise carry no effective before/after attribute values. Stat strength is not
fabricated from authored intent; only resource amounts and observed shield charges have measured scale.
Slow/Time grammar diagnostics remain closed because the gameplay constructor defect is outside scope;
their authored signature prefabs are catalogued without claiming a successful application.

The older BRIEF document asks for a document-only output and no implementation; this explicit build
task supersedes that scope. The task's gameplay write ban is honoured; only the COMMON/CLAUDE mandated
Render/Spells EditMode test exception under Assets/Scripts was used. No contract was changed. The
requested 0.6-second link and 0.25-second release clocks are event-triggered cosmetic tails, never
presented as simulation state.

## VERIFY:

All Unity commands used `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`
and `-batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-dyn-spells`.

| Command / check | Exit | Result |
|---|---:|---|
| `git branch --show-current`; `test -f .git/hooks/pre-push` | 0 | Correct branch; hook present |
| Unity `-quit -executeMethod HealerLike.Render.Spells.HLSpellPrefabBuilder.Build -logFile /tmp/hl-d3-build-final.log` | 0 | 88 inventory rows, 90 style entries, 9 delivery rows; zero compiler errors |
| Unity `-quit -logFile /tmp/hl-d3-compile-final.log` | 0 | Plain compile; zero error-CS matches |
| Unity `-runTests -testPlatform EditMode -testResults /tmp/hl-d3-tests.xml -logFile /tmp/hl-d3-tests.log` | 0 | 443 total; 441 passed; 0 failed; 2 skipped; zero error-CS matches |
| `grep -c` for compiler-error marker in final compile/test logs | 1 each | 0 matches each |
| Recursive extended-regex grep of the COMMON forbidden strings over Spells and its test folder | 1 | 0 matches |
| `git diff --check` | 0 | No whitespace errors |
| Changed-path audit against base | 0 | Only owned Spells and authorised Render/Spells tests |
| `git push -u origin zfc-dyn-spells` (implementation) | 0 | Only the authorised branch pushed |

The two skipped pre-existing grass GPU tests require a graphics device (shader compilation and Metal
compute). Headless testing used nographics as required. Unity's unrelated import changes to example
metadata, absent demo metadata and ProjectSettings were restored before committing. Existing importer
and zone Update-message warnings were not repaired across ownership boundaries.

BLIND-SPOT: No rendered frame was observed; GPU-only grass tests, post-merge observer/bud/tint wiring,
visual composition, moving-anchor beam tracking and live-game outcome provenance remain unverified.
