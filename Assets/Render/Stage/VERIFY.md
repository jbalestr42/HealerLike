# T7 stage verification

Branch `zfc-stage`, base `075b8be`. `.git/hooks/pre-push` exists. Origin fetched at start and before the final commit. No merge, cherry-pick, rebase, or other-track source import.

Scene: `Assets/Render/Stage/HLRenderLook.unity`, copied from the menu's actual target, `Main`. The menu and original Build Settings remain untouched. Open this scene directly, or run the capture entry below.

145 copied data assets, 11 render model variants, nine projectile variants with observer/impact placeholders, three area variants with pulse placeholders, 35 copied infrastructure prefabs (49 prefabs total). Data references, including component file IDs in area/projectile variants, are remapped. All six pipeline assets' referenced renderers carry one active outline feature placeholder. The builder replaces placeholders with available track components/assets when rerun after integration.

Bootstrap publishes the registry, binds the available spell sink, and controls look/zone/grass component lifetimes. Grass borrows the zone buffer through HLStageZoneBridge at execution order 0, between zone publication (-1000) and grass drawing (10000); disabling the bootstrap clears the borrow before releasing the zone owner. Stone generation waits for the existing gameplay grid cells and invokes the configured entry with seed 1707. No second grid generation or gameplay singleton is used by the stage.

Camera: perspective, 50-degree pitch, FOV 40, distance 31, position (0,24.247381,-19.926414). Board 16x16, size 1, grass roots .505. Calibrated fog 26.570/39.707, six bands, pale #BFD2E0; 1080p hatch spacing .08358, half-width .00334, distance start 26.570, far spacing .10030, outline 1px. These settings are applied to the real look controller when present; the current material is explicitly a plain placeholder, without toon/hatch/banded fog.

## VERIFY

Editor executable for every Unity command:
`/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`

Project path for every Unity command:
`/Users/fc/Documents/HealerLike-stage`

| Command (arguments after executable) | Final result |
| --- | --- |
| `-batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-stage -executeMethod HealerLike.Render.Stage.HLStageBuilder.Build -quit -logFile /tmp/hl-stage-build.log` | Exit 0; scene and assets authored; 0 C# errors. This also performs the plain compile/import. |
| `-batchmode -nographics -projectPath /Users/fc/Documents/HealerLike-stage -runTests -testPlatform EditMode -testResults /tmp/hl-stage-tests.xml -logFile /tmp/hl-stage-tests.log` | Exit 0; 208 passed, 0 failed, 0 skipped; 10 stage tests, 198 pre-existing tests; 0 C# errors. |
| `-batchmode -force-metal -screen-width 1920 -screen-height 1080 -projectPath /Users/fc/Documents/HealerLike-stage -executeMethod HealerLike.Render.Stage.HLStageCapture.Run -logFile /tmp/hl-stage-capture.log` | Exit 0; Metal Apple M2 Max; three 1920x1080 PNGs, sampled after 1/2/3 seconds of play. |
| `grep -n 'error CS' /tmp/hl-stage-build.log /tmp/hl-stage-tests.log /tmp/hl-stage-capture.log` | Exit 1, empty output: zero C# errors. |
| `grep 'test-run id' /tmp/hl-stage-tests.xml` | Exit 0; total 208, passed 208, failed 0. |
| Required forbidden-origin content and pathname scans over all of `Assets/Render/` | Both exit 1, empty output: zero matches. The literal scan is pasted in the handoff rather than introducing its search terms into this directory. |

Capture paths:

- `/Users/fc/Documents/healerlike-render-specs/captures/HLRenderLook-1.png`
- `/Users/fc/Documents/healerlike-render-specs/captures/HLRenderLook-2.png`
- `/Users/fc/Documents/healerlike-render-specs/captures/HLRenderLook-3.png`

The capture script opens the stage, enters play, presses the existing Start button, submits URP StandardRequest renders of the gameplay camera to a Metal render texture, writes PNGs, leaves play, and exits. This is necessary because ordinary Game view end-frame callbacks do not render in this batchmode environment. Images are camera captures, without screen-overlay UI. Visual inspection confirms the full board, grass coverage, healer placeholder and enemy placeholders. The stage hides the source's oversized debug-ground renderer, middle line and debug sphere, preserving their colliders; the debug ground otherwise obscures the battlefield.

The Metal log also contains two unrelated editor exceptions: A* update checking rejects an insecure HTTP connection, and Unity Search's startup indexing throws an index-range exception. Neither originates in stage code; they are not represented as a clean runtime log. An initial capture attempt relying on Game view callbacks was stopped after it produced no frames; the final render-request capture succeeds.

## CONTRACT-CONFLICT

- The older look specification proposes stock RenderObjects; the frozen contract requires T1 HLOutlines. This branch uses a clearly named HLOutlines_PLACEHOLDER hull-tag feature until that class is present. It does not claim depth/normal outlines are available yet.
- MainMenu loads Main, while enabled Build Settings contain MenuScene and TestHealer. The stage uses Main and does not alter the original list.
- The existing Ultra quality slot refers to absent pipeline GUID `a0da25f9ff8de264189edd30d9654c37`; the Graphics Settings fallback is Low. All six existing pipeline assets, including the separate Ultra asset, have their renderer references covered.
- Parallel implementations are available on remote branches but deliberately not imported. Actual spell visuals, zone ownership, look, creatures, stones and grass remain the owning tracks' code. See WAVE3-TODO.md for every placeholder and replacement.

BLIND-SPOT: These captures validate stage layout and placeholder rendering only. Final toon/hatch/fog, depth/normal outlines, GPU grass/wind/zone response, creature IK, spell feedback, and deterministic stone generation require the wave-3 integration and a new Metal capture. Player stripping and long-run performance were not exercised.
