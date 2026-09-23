# Wave 9 ground and stone correction

OpenAI lane, 2026-09-22. Work started from `4120756` in the isolated `HealerLike-match-ground` clone on `zfc-match-ground`. The reference read used artwork 05 and the actual wave7-4 frame, alongside the original brief and review 2.

The grass now uses broad lanceolate blades with a wider fan and a shared leaning clump. Roots are jade, with muted lime tips. The heal ramp retains a root-to-tip gradient and partial seeded hue variation. The thin white rim has more clearance from the wider leaves and sits 0.10 m above the surface. Its width is 0.018 m; the clearance scales down with tiny blooming radii so the centre is not flattened prematurely. Trample leaves 0.055 m of stubble with an irregular feathered edge instead of a shaved circle. Offscreen blades exit compute before the zone loop, within the same conservative envelope.

The black circular footing was partly a separate stock Lit disc, receiving physically dark cast shadows outside the authored toon system. It is now an irregular, muted jade Unlit footprint. Terrain clumps use varied angular silhouettes with lower polygon counts; lit slate faces are lifted and hatching cannot completely replace the shadow fill. Disabling stone visuals hides owned geometry. Re-enabling an enemy preserves the hidden state of already shed pieces.

The registry explicitly reserves space for non-Trample feedback before admitting decorative footprints. Surviving records retain registration order. The frozen packer still admits its input in order and the 32-byte layout is unchanged. A test registers 80 permanent footprints followed by heal and hostile pulses, checks both feedback records are visible, and checks footprints return as the pulses expire. More than 64 simultaneous non-Trample records still overflow in registration order.

`HLStoneAssembly.ApplyFracture` accepts an optional working-space status tint. It composes that tint with the existing health colour inside the same property block; white or null retains the original result. Integration must pass `HLBodyTintState.Read(entity.gameObject)` from `HLStoneEnemyVisual.CompleteHealthBatch`, once the creature/spell branch supplies that type. That branch's `HLResourceOutcomeObserver.Ensure(owner)` must also be called in the stone Init path. Those cross-track calls are left to integration so this branch remains independently compilable.

Recommended Stage values: ground sRGB(78,126,87), tip light 0.035, shadow tint sRGB(63,91,148), hatch spacing 3.5 px and strength 0.75. Stage owns the final camera and fog tuning. No Stage assets or Julien gameplay sources were edited.

The diagnostic `wave9-ground-fixture.png` uses a small 8x8 board, three seeded stone clumps, and explicit heal/hostile/trample zones. It is a visual-only fixture, not evidence of a gameplay cast. The first inspected Metal frame shows continuous broad grass clumps and legible slate stone faces. Bright heal grass and a separate rigid slate hostile patch are visible. The first frame's rim was occluded, which prompted the clearance/lift correction. The corrected image was inspected again: the thin white edge is visible and the heal blades retain darker roots.

VERIFY: Full Metal EditMode results and capture are under `/Users/fc/Documents/healerlike-render-specs/logs9/ground-tests4.*` and `captures/wave9-ground-fixture.png`. Final full suite: 630 passed, zero failed, two opt-in Look captures skipped (632 total). This includes GPU readback and shader compilation, the corrected appearance fixture, shed-part preservation and health/status composition regressions. Explicit paths only are committed; Unity importer changes in third-party metadata and ProjectSettings remain uncommitted.

BLIND-SPOT: This branch does not prove the final camera composition, natural gameplay heal/poison timing, a player build, the complete menu loop or combined-track performance. The fixture sets its zones explicitly and has no actors casting skills. The fixed-capacity policy protects feedback from Trample only; feedback itself can still exceed 64 entries. The unlit footprint deliberately omits fog and real lighting, so Stage should keep it within the clear battlefield. Final gameplay captures and cross-track observer/tint wiring remain integration work.

## Actual GameView correction

Combined gameplay exposed a failure missed by manual camera fixtures: the real
GameView contained the UI and actors but no indirect grass. Diagnostics ruled
out a wrong camera assignment; the actual camera matched the configured camera,
and all fields had prepared buffers (65,536 board blades). Submitting once from
LateUpdate was insufficient for the render that produced the composed GameView.
Grass now prepares buffers in LateUpdate and submits them at the assigned camera's
SRP beginCameraRendering callback. Disable/destroy removes the callback, and a
revoked or disposed borrowed zone buffer prevents submission. There is no
screenshot-specific runtime path.

An actual normal-speed ScreenCapture sequence from menu through gameplay,
`wave9-events/portrait-20260922-232732`, recorded 170 frames over 15.3 seconds,
47 projectiles, three positive health outcomes and two observed statuses.
`motion-00012.png` was inspected and shows the dense carpet with the composed HUD
and inventory. The full merged Metal suite passed 639 tests, zero failed, with
two opt-in Look captures skipped. The appearance regression renders a second
camera frame without another grass LateUpdate, then revokes the borrowed
snapshot and verifies the next camera frame stops drawing grass.

This establishes the actual GameView correction under the Editor recorder. An
interactive player has not been visually inspected by this track; Stage owns
the final player rebuild and both final gameplay captures.

Stage can reduce only the battlefield strip height through serialized
`bladeHeightScale` (public `BladeHeightScale`). Its default is 1, with finite
values clamped to 0.25..1. Stage requested 0.8 on the board to keep actors
readable; environment strips retain 1. Width, density and hostile cone height
are unchanged. The focused Grass suite passed all 24 tests after the camera
submission and revoked-snapshot correction.

Final actor color defect: Metal HLForward readback confirmed MPB.SetColor converts artist colors to linear, so passing an already-linear stone palette converted it twice. Artist RGB (.4,.6,.8) reached the GPU as (.01590,.08276,.32303) through the old path, instead of (.13287,.31855,.60383). Four stone assignments now use SetVector for existing working-space values. Fracture tests inspect raw GetVector values because GetColor roundtrip concealed the bug. New HLForward GPU regression compares all three upload paths and verifies correct linear readback. Full current clone suite: 639 passed, zero failed, three skipped; exact XML/log /tmp/w9-colour-final.*. Actual focused GameView acceptance remains with Stage integration. Plant input colors were already correct; its uniform lit fill remains the authored binary toon behavior, not this conversion defect.
