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
