# B6-look beauty report

Branch `zfc-b-look`, clone `/Users/fc/Documents/HealerLike-b-look`, starting at
`b490ff2`. `git branch --show-current` confirmed the requested branch. Read COMMON,
CONTRACT, DYNAMICS, BEAUTY, BRIEF, CLAUDE, Look README/WAVE5-REPORT and the shared
WAVE4-REPORT. There is no Look/FIX-REPORT.md in this checkout (other tracks have
one). Work stays in Look, HLLook shaders and the permitted Look test folder.

## Delivered

- Faint pale battlefield grid in the primitive ground shader, enabled only by the
  material's `_HLGroundGrid=1`. `_HLGridOrigin.xyz` is the minimum board corner;
  `_HLGridCell` is square cell size; `_HLGridExtent.xz` is full width/depth;
  `_HLGridStrength` is opacity. Nonpositive cell or extent disables it. Analytic
  antialiasing keeps thin lines; unresolved cells fade. Distance fade uses the
  look fog interval, and ordinary banded fog follows it. It never enters the
  shared surface function, so rocks/plants do not get grid lines.
- Static smooth world-space noise perturbs fog thresholds by at most .32 of a
  band. Six bands remain the default, near/far endpoints remain clear/full, and
  no time or simulated state is fabricated.
- Pixel-normalized outline projection, including perspective, aspect ratio,
  off-axis position and orthographic projection. Primitive hulls now use a direct
  clip-space helper; the frozen HLOutlineExtrude signature remains available.
  `_HLOutlineWidthMultiplier` defaults to 1, zero disables a material's hull.
- Fixed an existing outline-normal ambiguity exposed by the new captures:
  missing TEXCOORD3 streams cannot be detected by testing their value for zero
  on Metal. The built-in sphere previously inked only one side. Materials now
  default to actual lighting normals; `_HLSmoothOutlineNormals=1` explicitly
  enables authored TEXCOORD3 normals for meshes that really supply them.
- Retained the existing derivative-based 3.5-pixel hatch mode and verified it at
  42.8, 43.837 and 47.8 units with the 73.7-degree portrait camera. No world-unit
  retuning or camera-distance outline inflation is needed.
- `_HLTipLight` and `HLApplyTipLight(color, bladeHeight01, illum)` are additive
  grass integration seams. At .12 the top 35 percent of lit blades receives a
  subtle pale yellow-green lift. Roots and shadow hatching stay unchanged.
  Zero disables it. The helper is shipped here; Grass owns its eventual call.
- Removed per-frame/per-camera allocating light discovery from HLLookController
  and cached upload delegates. Frame/camera fallback uses the active sun; the
  existing HLOutlines renderer feature still supplies URP's actual culled main
  light (including no-main-light quality tiers). Without that feature, fallback
  is sun-only. A warmed 128-frame-plus-camera test asserts zero managed bytes,
  verifies the camera mask, and verifies disabled-sun clearing.

The frozen globals and core signatures were not renamed or removed. No package,
texture input, gameplay state, motion or runtime mesh was added. PNGs are evidence
captures, not shader inputs. The diagnostic BeautyProbe shader samples no texture.

## Exact integration for Stage and Grass

Use the existing HLLookController component and HLOutlines feature already shipped
by Look. No additional behaviour is needed for a static ground shader feature.
Stage must publish these values after its board exists, and whenever bounds change.
For the existing `grid`, `groundMaterial` and `lookController` references:

```csharp
var board = new Bounds(grid.transform.position,
    new Vector3(grid.width * grid.size, 0, grid.height * grid.size));
Shader.SetGlobalVector("_HLGridOrigin", board.min);
Shader.SetGlobalFloat("_HLGridCell", grid.size);
Shader.SetGlobalVector("_HLGridExtent", board.size);
Shader.SetGlobalFloat("_HLGridStrength", .12f);
Shader.SetGlobalFloat("_HLTipLight", .12f);
// Use a Stage-owned material instance, not the shared Environment material asset.
groundMaterial.SetFloat("_HLGroundGrid", 1f);
var look = (HealerLike.Render.Look.HLLookController)lookController;
var settings = look.Settings;
settings.InkSpacingPixels = 3.5f;
settings.OutlineWidthPixels = 1f;
look.Settings = settings;
look.ApplyGlobals();
```

Stage teardown must call `Shader.SetGlobalFloat("_HLGridStrength", 0f);` and
`Shader.SetGlobalFloat("_HLTipLight", 0f);` to avoid stale board controls. These
additive globals intentionally are not overwritten by HLLookController.
The Stage builder currently applies distance-derived outline inflation; replace
that assignment with `settings.FindPropertyRelative("OutlineWidthPixels").floatValue = 1f;`
and add `settings.FindPropertyRelative("InkSpacingPixels").floatValue = 3.5f;`.
Only enable `_HLSmoothOutlineNormals` on materials whose meshes explicitly supply
normalized smooth TEXCOORD3 vectors. A material needing a quieter hull can use
`material.SetFloat("_HLOutlineWidthMultiplier", .75f);`.

Grass should replace its final combined surface evaluation with this sequence,
using its already-computed blade height and illumination (adapter variable names
must match Grass's source):

```hlsl
float3 color = HLShadeSurface(positionWS, illum, baseColor);
color = HLApplyTipLight(color, bladeHeight01, illum);
return HLApplyBandedFog(positionWS, color);
```

## Captures and measurements

All evidence is under `Assets/Render/Look/captures/beauty-look-*.png` (1080x1920).
The opt-in `HL_B6_CAPTURE=1` fixture clones a pipeline/renderer in memory, uses
primitive ground/sphere geometry and an upper-left light, and restores state.

- `grid-off`, `grid`: 54,340 changed pixels, zero outside the 16x16 battlefield
  rectangle (ray/ground intersection assertion, .04-world-unit raster tolerance,
  strength .16 for the diagnostic).
  The sphere has no grid. `grid-fade` uses the portrait fog interval as well.
- `fog`: six visibly nonconcentric, gently warped bands at 43.837/50.356.
- `hatch-{42.8,43.837,47.8}`: measured perpendicular stroke periods
  **3.541 / 3.551 / 3.536 px**. Measurements use local luminance minima in the
  blue ground-shadow patch x=602..636, y=810..849 (PNG top-left coordinates),
  combining horizontal and vertical periods as 1/sqrt(1/h² + 1/v²). Warp/dashes
  are disabled for this spacing diagnostic; both stay available in production.
- `outline-{distance}-{off,1px,2px}`: perspective distances 26, 42.8 and 47.8;
  also a 43.837-unit orthographic view. Assertions inspect both silhouette edges
  at sphere-centre height for the requested widths with one-pixel raster tolerance.
  All four views measured exactly **1/1 pixels left/right**, then **2/2 pixels**
  when the material multiplier doubled.
- `tip`: direct helper probe, shadow on left/lit on right, root at bottom/tip at
  top. Lit tips brighten; shadow tips equal roots exactly (pixel assertions).
  This isolates the option; it is not an integrated grass capture.

## Not done and why

Stage, Environment, Grass, scenes and pipeline assets were not edited. Grid bounds,
ground material opt-in, legacy serialized hatch/outline settings, and the grass tip
call need the owning tracks' wiring above. The full playable stage, dense indirect
grass and actual faceted rock smooth normals were not rendered here. Ground shadow
receiving remains intact and is visible in the captures; pipeline shadow coverage
continues to be Stage's responsibility. This work does not certify every silhouette
mesh, device, render-scale or gameplay interaction. The allocation assertion covers
Look publication, not all URP or other-track rendering allocations.

VERIFY:

Implementation and tests committed together as `644c8dd`. Captures and this report
are a subsequent documentation/evidence commit. All 20 PNGs were generated by the
final passing Metal fixture. No unowned source/asset changes remain.

Commands use `U=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity`
and `P=/Users/fc/Documents/HealerLike-b-look`:

| Command | Exit | C# errors | Shader errors | Result |
|---|---:|---:|---:|---|
| `"$U" -batchmode -nographics -projectPath "$P" -quit -logFile /tmp/hl-b6-compile-final.log` | 0 | 0 | 0 | Compile passed |
| `"$U" -batchmode -nographics -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-b6-editmode-final.xml -logFile /tmp/hl-b6-editmode-final.log` | 0 | 0 | 0 | 524 passed, 0 failed, 6 skipped |
| `HL_B6_CAPTURE=1 "$U" -batchmode -force-metal -projectPath "$P" -runTests -testPlatform EditMode -testResults /tmp/hl-b6-metal-full.xml -logFile /tmp/hl-b6-metal-full.log` | 0 | 0 | 0 | **529 passed, 0 failed, 1 skipped** |

The sole Metal skip is the older wave-5 opt-in capture. All existing ordinary tests,
including the grass GPU tests, pass. The new allocation test passes on both paths.
The primitive shader's keyword/pass compilation and both screen-edge encodings pass.
The first Metal attempt rejected a reserved HLSL variable name; it was corrected
before every final run. Subsequent silhouette inspection caught the missing-normal
stream issue and led to the explicit opt-in and two-sided pixel assertions above.

`git diff --check`: exit 0. Forbidden-name scan of owned sources/docs/tests: zero
matches (grep exit 1). Original contract declarations/signatures retained; only
additive controls/helpers/material properties introduced. Unity's incidental
ProjectSettings and unrelated importer metadata changes were restored.
Only `git push -u origin zfc-b-look` is used for publication.

BLIND-SPOT: Files from batchmode Metal were inspected; no visible Editor, device,
full-stage gameplay or integrated indirect-grass tip rendering was observed.
