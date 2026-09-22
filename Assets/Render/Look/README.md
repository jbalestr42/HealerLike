# HL look integration

Use `HL/Look/Primitive` with `HLLook_Default.mat` (ally green, instancing enabled).
Override only `_BaseColor` through a MaterialPropertyBlock for per-instance colours.
The core and material input declarations live in `Assets/Render/Shaders/`.

Add one HLLookController to publish the complete look at beginFrameRendering.
Settings colours are authored in sRGB and explicitly converted when uploaded.
With no owner, or after the owner is disabled, shaders use the same baked defaults.
A conflicting controller must be disabled and re-enabled after the owner releases
ownership. `ApplyGlobals()` publishes immediately for an active owner; the static
`UploadGlobals(in settings)` entry point is for explicit setup/tooling.

T7 must add HLOutlines to each intended renderer asset. The feature is deliberately
not installed in any renderer or scene here. Assign its serialized Edge Shader to
`Hidden/HL/Look/DepthNormalOutline` and save that reference for build retention.
Create also resolves the shader by name when authoring the feature.

The feature draws opaque `HLOutline` passes after opaque fills, respecting its
object Layer Mask. It then optionally blends depth/normal edges into camera colour.
The screen pass covers all geometry represented by camera depth/normals; its
coverage is not restricted by the hull Layer Mask. Both passes use the shared ink
colour, pixel width and banded fog. Disable Depth Normal Edges for hull-only use.
This feature implements the URP RenderGraph path.

Indirect grass must provide matching deformed camera depth and normals through
its submission path. This feature requests both textures, but does not discover
or replay indirect draws. Primitive depth and normal passes are supplied. Normal
textures support both ordinary world normals and the octahedral encoding.

For hard-edged stone, supply smooth object-space outline normals in TEXCOORD3;
lighting continues to use face normals. A missing/zero outline normal falls back
to the lighting normal. Pixel width is approximate at silhouettes and needs a
visual check with the integrated camera, grass density and LODs. Fog distances
20/60 and one-pixel outlines are initial art values.

EditMode tests cover settings/default parity, validation, controller ownership,
publication ordering, colour conversion, fallback, material and feature lifecycle.
The shader tests also synchronously compile 52 primitive pass/keyword combinations
and both edge normal encodings when a graphics device is available. The required
nographics run validates asset import and C# behavior; run with force-metal to
exercise shader compilation. Compilation does not replace a rendered frame capture.

## Wave 5: shadows, portrait hatch and grass-safe screen edges

Primitive shadow sampling uses projected screen coordinates for screen-space main
shadows, and world-to-shadow coordinates for atlas/cascade variants. Its existing
caster uses light-space bias and near-plane clamping. Cast shadows enter the same
ultramarine toon tint as unlit facets; no black shadow multiplication is added. Fully occluded shadows converge to the
authored tint; ShadowStrength controls the blend at the toon boundary.
The pipeline and light still have to enable shadows and cover the camera range.

`HLLookSettings.InkSpacingPixels` defaults to 3.5. The projected derivative of the
world hatch coordinate scales spacing with camera distance, FOV, surface slope and
render resolution. This mode bypasses the old shadow-tone density compression.
Set zero to retain the legacy `InkScale`/`InkDistStart`/`InkFarSpacing` controls.
At 40-degree vertical FOV and 1920-pixel portrait height, 3.5 pixels correspond to
about .0345/.0411/.0531 world units on a camera-facing surface at 26/31/40 units.
These are projected spacings, not constant world distances on a tilted ground.
Older serialized settings lacking the new field deserialize as zero; the stage
owner should explicitly set `InkSpacingPixels=3.5` on its existing controller.

`HLOutlines` exposes `DepthThresholdWorld=1`, `ReferenceDistance=31`,
`DistanceScale=1`, `NormalAngleDegrees=55`, `NormalDensityDegrees=35`, and
`UseNormalEdgeMask=true`. Depth edges require a positive eye-depth jump above
`1 * (1 + max(0, eyeDepth/31 - 1))` world units. Normal edges require the angle
threshold; neighboring normal variation raises it by up to 35 degrees. These are
independent edge channels: the mask suppresses normal edges, never depth edges.
The existing `LayerMask` continues to select hulls only.

Normal-edge eligibility is camera-normal alpha. Primitive `_HLNormalEdges`
defaults to one, can be overridden per material or via MaterialPropertyBlock
(including instancing), and writes into both normal encodings. Set it to zero
for dense blade geometry. The existing grass normal pass already writes zero,
so grass opts out without any grass-owned source edit. Other shaders that write
zero also opt out. If a renderer repurposes normal alpha or supplies RGB-only
normals, disable this mask and integrate a separate mask before enabling the pass.
Indirect grass still needs matching camera depth/normals submitted by its owner.
All six renderer assets currently leave `DepthNormalEdges` off; D5 does not own
those assets. Enable the feature after integration and inspect the full grass field.

`_HLKeyLightDir.xyz` points **toward** the main directional light in world space;
`w=0`, and zero means no light. HLLookController publishes the sun (or brightest
active directional) on frame/camera callbacks. HLOutlines supplies URP's actual
culled main-light selection before rendering, so disabled-main-light quality
tiers publish zero. Disabling the owning controller clears the direction.
This is render state; no animation clock or simulated event was introduced.

The opt-in `HLLookShaderTests.CapturePortraitShadowsAndMaskedEdges` fixture builds
an in-memory scene inside the Look test folder's test code. Run the Look tests
with `HL_D5_CAPTURE=1 -batchmode -force-metal`. It clones a Very High pipeline and
renderer in memory, without saving assets. Outputs go to the shared captures
folder: shadow, edge-off, edge-on and side-by-side (off left, on right). The dense
blades are masked primitive fixtures, not an indirect grass performance test.
