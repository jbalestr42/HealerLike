# Stage composition and playable delivery

OpenAI lane; own sibling clone, zfc-match-stage from 4120756.

Lowered portrait pitch to 52 degrees and landscape to 46, fitting both axes with a HUD margin. Fog starts beyond the farthest playable corner. Foreground uses live aspect and smaller geometry; vegetation occupies a tighter ring. Ground and tip light use the values agreed with the grass owner. Shared primitive caches now retain foreground/ridge owners, generated environment roots hide when their components disable, and scatter uses world positions correctly.

Range preview defaults to the actual pointer, Hidden suppresses hover, and disabled preview components no longer force per-frame rescans. Public selection/drag state remains unavailable.

The render GameOver prefab had a null restart reference. Builder resolves its RestartButton, fills the reference, disables the old view Start callback and wires the render-menu destination while retaining its UIManager view identity. The explicit standalone build uses only the render menu and stage in BuildPlayerOptions; no persistent Build Settings mutation.

The smoke can only pass after three completed rounds. A third round still running at the grace deadline is now failure. Its resource listeners detach on manual play exit too.

HLEventCapture uses the render menu and actual inventory buttons/placement interactions. It selects the intended validated Character skill once, distinguishing instant group skills from targeted skills. Health and mana outcomes are logged separately; named stored listeners detach. It writes normal-speed, timestamped motion frames plus actual ScreenCapture overlay screenshots. Camera images are labelled separately. HLStageRouteSmoke exercises a real empty-roster loss and the render restart route.

VERIFY: Stage author/build exit 0; initial whole Metal suite 626 passed, two outdated default assertions failed, two existing skips. Corrected camera-aspect fixture and tip-light expectation; all 86 Stage/Environment tests passed, including new board corner/fog checks and disabled-preview scan regression. Early playable capture showed real ally launches/hits. Real-event capture completed 15 seconds at timeScale 1: 169 timed frames, 3 positive health outcomes, 41 negative health outcomes, 47 observed projectiles, 3 validated skill casts. Raw evidence at healerlike-render-specs/captures/wave9-events/portrait. Combined peer rendering, full three-round smoke, restart loop and player build results will follow in the integrated report.

BLIND-SPOT: Early images still use baseline creatures/grass. No transient hostile AoE was triggered through a real item in this recording; enemy threat readout is separate. Programmatic real buttons/raycast placement do not verify physical mouse/touch input. Positive outcome counts do not alone establish perceptible heal beauty. The integrated image comparison remains required.
