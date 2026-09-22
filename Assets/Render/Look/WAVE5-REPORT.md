# D5-look — wave 5

Branch `zfc-dyn-look`, clone `/Users/fc/Documents/HealerLike-dyn-look`, based on
`b6b6773`. Startup confirmed the branch name and `.git/hooks/pre-push` exists.
Read COMMON, CONTRACT, DYNAMICS, wave 3 report and Look README in the requested
order; also read BRIEF and CLAUDE.md. No simulation motion or timer was added.

## Implemented

- Primitive forward shading uses projected screen coordinates with
  `_MAIN_LIGHT_SHADOWS_SCREEN`; ordinary and cascaded shadows use the per-fragment
  world-to-shadow path. The existing caster already supplies directional/punctual
  bias, near-plane clamping, instancing, depth writes and the ShadowCaster tag.
  Deep shadows now converge to the existing `ShadowTint` (default `#2B4B8F`)
  instead of mixing enough green into them to appear teal. `ShadowStrength`
  remains the blend strength at the toon boundary. Shared core signatures remain
  unchanged, so grass also receives the corrected tint response.
- HLOutlines exposes depth threshold **1 world unit**, reference distance **31**,
  distance slope **1**, normal angle **55 degrees**, density penalty **35 degrees**,
  and normal mask enabled. Depth jumps scale by `1 + max(0, eyeDepth/31 - 1)`.
  Normal variation raises the angle requirement. The depth and normal channels
  are independent; disabling normal eligibility preserves qualifying depth edges.
- Primitive `_HLNormalEdges` is a material/instanced property and supports
  MaterialPropertyBlock overrides. Both camera-normal encodings write eligibility
  to alpha. Grass already writes zero alpha, so it opts out of normal edges
  without editing grass sources. URP's forward normal formats in this installed
  package all retain alpha. Other shaders writing zero also opt out.
- `HLLookSettings.InkSpacingPixels=3.5` uses the projected world hatch-coordinate
  footprint, scaling with camera distance, FOV, surface slope and resolution.
  Zero retains legacy world spacing. Pixel mode bypasses tone-density compression.
  For 40-degree vertical FOV, 1920 portrait height, front-facing equivalent world
  spacings at 26/31/40 units are .0345/.0411/.0531. Existing serialized controllers
  need the new field set explicitly to 3.5 by the stage owner (missing fields are
  zero). Other ink controls remain available; capture diagnostics set warp and
  dash amount to zero to make spacing inspectable.
- HLLookController publishes `_HLKeyLightDir` toward the main directional light,
  with w=0; zero denotes absence. Frame/camera publication selects sun then the
  brightest enabled directional. HLOutlines supplies URP's actual culled main
  light before drawing, including zero on disabled-main-light tiers. The owning
  controller clears the global on disable; conflicting controllers cannot publish.
- Extended the existing per-class Look tests for publication, owner cleanup,
  light selection, settings validation, edge settings, shader variants and the
  opt-in graphics fixture. No new production C# class was needed.

## Read-only pipeline/renderer audit

Values below come from every `Assets/Settings/*PipelineAsset.asset`, not from
legacy built-in QualitySettings shadow fields.

| Tier | Main light / main shadows | Additional shadows | Soft shadows | Distance | Cascades | Main map | Coverage at 26–40 |
|---|---|---|---|---:|---:|---:|---|
| Very Low | off / off | off | off | 15 | 1 | 1024 | none |
| Low | off / off | off | off | 20 | 1 | 1024 | none |
| Medium | on / on | on | off | 20 | 1 | 1024 | outside range |
| High | on / on | on | on | 40 | 2 | 2048 | near/centre; fade at far edge |
| Very High | on / on | on | on | 70 | 2 | 4096 | covered |
| Ultra | on / on | on | on | 150 | 4 | 4096 | covered |

All six companion renderer assets use Forward (`m_RenderingMode=0`), permit
transparent shadow receiving, and have `DepthNormalEdges=0`. The renderer assets
have no separate opaque main-shadow enable switch; the pipeline owns it. All six
pipeline assets have required depth/opaque textures off; HLOutlines requests depth
and normals when its screen pass is enabled. No screen-space-shadow renderer
feature is installed. GraphicsSettings' default pipeline is Low, while the
serialized current quality index is 5 (Ultra); the quality override matters.
The authored stage directional light has soft shadows enabled. The existing grass
RenderParams have `receiveShadows=true`, `shadowCastingMode=Off`; its shader samples
main-light shadows in the atlas/cascade variants and has no caster, as permitted.

## Captures and observations

The Look test fixture builds a sphere, plane, upper-left directional key and 216
blade-shaped primitives in memory. It clones Very High pipeline/renderer objects
without saving assets, uses a 31-unit, pitch-50, FOV-40 camera and 1080x1920 target,
then destroys the fixtures and restores the pipeline/light. Outputs are under
`/Users/fc/Documents/healerlike-render-specs/captures/`.

- `wave5-shadow.png`: **yes, an ultramarine ellipse appears**, partially occluded
  by the slate sphere, extending toward screen right from the upper-left key.
  It has a crisp toon boundary with a narrow filtered edge. A pixel assertion
  additionally requires over 1,000 blue-dominant pixels. No visible window was used.
- `wave5-edges-off.png` and `wave5-edges-on.png`: sphere silhouette is inked with
  the pass on; the shallow blade fixtures do not acquire the former all-blade
  wire outline. `wave5-edges-side-by-side.png` places off left, on right.
- `wave5-edges-unmasked.png`: disabling the mask adds normal ink to the blades.
  In the blade rectangle x=180..919, y=1080..1509 (318,200 pixels), the masked
  on/off comparison differs at **zero** pixels; unmasked versus masked differs
  at **34,964**. Sphere edges elsewhere remain visible with masking enabled.
- `wave5-hatch-{26,31,40}.png`: visible diagonal hatching confined to shadow.
  Local ground-shadow minima give approximate perpendicular stroke periods of
  **3.68 / 3.74 / 3.75 pixels**, respectively. Measured horizontal/vertical periods
  were 4.59/6.14, 4.71/6.14 and 4.81/6.00; combining reciprocal frequencies gives
  the perpendicular period. This meets 3–4 pixels in these portrait samples,
  not a guarantee for every surface orientation or warp setting.

## CONTRACT-CONFLICT

- The task asks for shadows at the stage distance, but COMMON permits only the
  stage track to edit renderer/pipeline assets. D5 therefore audits and reports
  the disabled/short-range tiers instead of changing them. Stage integration must
  enable the main light and main shadows, choose suitable shadow distance (70 is
  the tested configuration), and opt into DepthNormalEdges on the intended tiers.
- The task says nothing under Assets/Scripts changes while also requiring Julien's
  test rule. COMMON explicitly grants the track's Render/Look EditMode test folder;
  only that test exception is used. No gameplay script changed.
- BRIEF contains earlier design-only instructions to write one spec file; this
  explicit build task supersedes them. Frozen core function signatures and both
  new delivery contract files remain untouched.
- Grass's screen-space-shadow variant still passes atlas coordinates from
  `HLGrassData.hlsl`, which is outside D5 ownership. Its owner should apply the
  analogous screen-coordinate branch before enabling screen-space shadows.
  Current renderer assets use atlas/cascades, for which receiving is enabled.
- The pre-existing HLZoneRegistry Update message-name warning remains. D5 makes
  no calls to it and does not edit Zones, Stage or Environment; wave 4 owns the fix.

## Not done and limits

No stage scene or pipeline asset was edited or persisted. No full-stage gameplay
capture or indirect grass density/performance measurement was made: the dense
edge fixture uses masked primitive blades to exercise the shared normals-alpha
seam. Indirect grass still requires its owner to submit matching camera depth and
normals; requesting the textures cannot replay indirect draws. The screen-space
shadow shader variant is compilation-tested, not captured with a screen-shadow
renderer feature. Full cast-shadow softness remains stylized by toon quantization.

VERIFY:

All commands used this clone and Unity
`/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`.
For reproduction, set `U` to that executable and `P` to this clone path.

| Command (logs/results in /tmp) | Exit | Compiler errors | Tests |
|---|---:|---:|---|
| `"$U" -batchmode -nographics -projectPath "$P" -quit -logFile /tmp/hl-d5-compile-final.log` | 0 | 0 | compile |
| `"$U" -batchmode -nographics -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-d5-editmode.xml -logFile /tmp/hl-d5-editmode.log` | 0 | 0 | 421 passed, 0 failed, 3 graphics skips |
| `HL_D5_CAPTURE=1 "$U" -batchmode -force-metal -projectPath "$P" -runTests -testPlatform EditMode -testFilter HealerLike.Render.Look -testResults /tmp/hl-d5-metal-final.xml -logFile /tmp/hl-d5-metal-final.log` | 0 | 0 | 24 passed, 0 failed, 0 skipped |
| `HL_D5_CAPTURE=1 "$U" -batchmode -force-metal -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-d5-metal-full.xml -logFile /tmp/hl-d5-metal-full.log` | 0 | 0 | **424 passed, 0 failed, 0 skipped** |

`grep -c 'error CS'` on all listed logs prints 0 (grep exit 1 means no matches).
XML root `failed` is zero in all final runs. The full Metal suite also exercises
both previously skipped grass GPU tests. The Look shader test compiles 52 primitive
pass/keyword combinations and both edge normal encodings. Initial Metal fixture
attempts failed on Unity's EditMode scene-creation restrictions (3/4 and 22/23
passed); corrected to temporary test-scene objects, after which every final run
passes. All initial/final compile logs also contain zero C# compiler errors.

The forbidden-name grep over Look, HLLook shader files and Look tests returned
exit 1 with zero matches. `git diff --check` returned 0. Unity import/session
changes to unrelated metadata and ProjectSettings were restored before committing;
only D5-owned source/docs and the allowed Look test folder are included.
Implementation and tests: commit `966c33b`.
`git push -u origin zfc-dyn-look` returned exit 0; only that branch was pushed.
The report is a subsequent documentation commit on the same branch.

BLIND-SPOT: GPU captures were inspected from files, not a visible Editor window;
full-stage indirect grass and the parallel wave-4 camera/assets were not rendered.
