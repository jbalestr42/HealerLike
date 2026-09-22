# B5 creature beauty

Branch: `zfc-b-creatures`, confirmed before changes. Only Creatures and its allowed EditMode test folder are delivered.

## Delivered

- Five authored silhouettes: curled fern, open arch with hanging bright pods, offset sphere stack, radial blade rosette, and healer cone under an oval torus. The crown rotates at 18 degrees/second from the supplied clock. The rosette cones now actually point radially outward.
- Six roots for healer, arch and rosette; seven for fern; eight for stack. Each gripping leg has two cone segments, the distal segment thinner. Feet remain projected onto the configured ground plane; the root footprint stays within the existing cell bound.
- Rounded joint collars on capsule stems; recipe part budget bounded at 40 to accommodate them. Existing four-root generic defaults remain compatible.
- Stable per-instance hue variation within six degrees and value within eight percent, including roots and arms, without consuming gameplay randomness. The seed combines recipe seed and the presentation parent's entity ID; it is stable during that instance's lifetime, not across sessions.
- Clock-driven sway and breathing layered onto simulation-driven aim, hit response and health droop. Charge gives up to 24 percent head swell and brightens authored buds. Low health suppresses brightness and shifts the body toward jade; healer mana still controls its buds. Cooldown readiness is not an invented cast event.
- Existing active tube mesh retained. Five alternating cone leaves, five spherical joint collars and one sphere tip use two instanced submissions per active arm, with cached meshes and reusable matrices. Resting arms submit neither tube nor details. Swarm detail scales down with its tube. Existing Direct, Arc, Rigid, Swarm, Bounce and ChainSync behavior and lease ownership remain intact.
- Removed the existing per-frame allocating healer resource search. Loaded scenes are traversed with reusable root/component lists; the Character's persistent scene is included when needed. New resources are observed on the next discovery Update. Steady-state managed allocation tests cover discovery, body ticks and active tube/detail matrix updates.
- Recipe-only editor refresh (`HLCreatureAssetAuthoring.AuthorBeautyRecipes`) avoids rewriting prefab overrides. No packages, textures, gameplay state, sockets or frozen contracts changed.

## Stage mapping and exact wiring

| Entity type | Owned presentation prefab | Recipe |
| --- | --- | --- |
| Character healer | HLHealerCharacter | HLHealer |
| Normal, FastShoot, RandomShoot | matching HL-prefixed variant | HLSpiralFern |
| Swarm, TripleShoot, MultiShot | matching HL-prefixed variant | HLHangingArch |
| Test, ChainLightning, Channeling | matching HL-prefixed variant | HLSphereStack |
| HitArmorBuffer | HLHitArmorBuffer | HLBladeRosette |
| Soldier | retain Stage's stone enemy | HLSoldier sphere-stack prefab remains a fallback only |

The current Stage builder classifies HitArmorBuffer as a stone. In `HLStageBuilder.Models()`, insert this exact line after assigning `real` and before loading `replacement`:

```csharp
if (name.Contains("HitArmor")) real = "Assets/Render/Creatures/Prefabs/HLHitArmorBuffer.prefab";
```

This selects the plant presentation while retaining the existing enemy/range classification. Rebuild the Stage-owned model variant and copied EntityData through the Stage workflow. Other mappings already point to the owned presentation variants; their recipe GUIDs are unchanged.

Use an instancing-enabled shared primitive material (the shipped placeholder and Stage green material support this). When injecting another material, the exact required setup is:

```csharp
sharedMaterial.enableInstancing = true;
builder.Configure(registry, cellSize, groundOrigin, groundNormal);
```

For an explicitly anchored healer, the existing binding remains:

```csharp
view.Bind(character, healerRecipe, visualAnchor, sharedMaterial, registry, cellSize);
```

## Not done and why

- Stage, Environment and Settings wiring are outside this track. The buffer model override above is required to expose the fifth recipe in Stage.
- No screen capture or visual judgment of resemblance, shadows, outlines, overlapping chains or composition is claimed. Headless mode skips GPU draw submission; actual instanced rendering still needs a graphics-device check.
- No instanced-detail fallback is added for unsupported hardware or a material with instancing disabled; the original tube still renders.
- Resource discovery remains a scene traversal each frame. Its CPU cost scales with scene size. List growth, rig creation, first-time delivery pool expansion and event subscription can allocate; the zero-allocation guarantee tested here is steady state. Resources processed before the next discovery pass retain the existing first-event limitation. Persistent resources in a different hidden scene from the Character are not discovered by this traversal.
- Existing outcome routing, gameplay reach limits and non-attacking-ally diagnosis are unchanged. No gameplay state is invented to make motion happen.

VERIFY:
- `git branch --show-current`: exit 0, `zfc-b-creatures`.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-creatures -quit -executeMethod HealerLike.Render.Creatures.HLCreatureAssetAuthoring.AuthorBeautyRecipes -logFile /tmp/hl-b5-author-success.log`: exit 0; five recipe assets regenerated.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-creatures -quit -logFile /tmp/hl-b5-compile-verified.log`: exit 0; zero `error CS`.
- `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-b-creatures -runTests -testPlatform EditMode -testResults /tmp/hl-b5-editmode-verified.xml -logFile /tmp/hl-b5-editmode-verified.log`: exit 0; 537 total, 532 passed, failed=0, inconclusive=0, five skipped; zero `error CS`.
- The five skips are two grass resource-lifetime GPU checks, two grass GPU/shader checks, and the opt-in portrait capture. No tests were disabled by this change.
- Warm allocation assertions: 0 managed bytes for 100 rig ticks, 20 active liana ticks, and 20 healer discovery updates. Instanced GPU submission is unavailable under `-nographics` and is not covered by those measurements.
- Forbidden-string scan across both owned folders: zero matching files. `git diff --check`: exit 0. Six unrelated Unity-generated importer/ProjectSettings changes were restored after verification.
- Implementation plus tests: `c0f8827`. `git push -u origin zfc-b-creatures`: exit 0, only the authorized branch pushed. This report follows in its own documentation commit.

Preliminary runs caught the obsolete instance-ID API, the old six-root/24-part authoring limits, and one existing test expecting the generic four-root default. These were corrected; the generic default was preserved. The final commands above test the delivered code, including the instanced colour arrays and healer-cone charge swell.

BLIND-SPOT: Nothing was rendered on screen. Headless checks cannot establish final beauty, gameplay feel, GPU instancing/outline behavior or frame time in the integrated Stage.
