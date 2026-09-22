# GPU grass integration

T2 supplies `HLGrassField`, `HLGrassLayout`, `HLGrassBlade`, `HLGrassCone`,
`HLGrassRing`, `HLGrassBounds`, and `HLGrassBenchmark` in `HealerLike.Render.Grass`.
No scene, gameplay, registry, renderer asset or frozen contract is changed.

## Stage wiring (T7)

Add one HLGrassField and assign its GridManager, flat ground Transform and gameplay
Camera. Assign these assets explicitly so player builds retain them:

- `Assets/Render/Shaders/HLGrass.compute`
- `Assets/Render/Shaders/HLGrass.shader` (`HL/Grass/BladeAndCone`)
- `Assets/Render/Shaders/HLGrassRing.shader` (`HL/Grass/HealRing`)

Default exposed BladeBudget is 65,536. Quality choices are 32,768 / 65,536 / 98,304;
the absolute cap also applies to larger grids. Budget zero disables grass submissions.
Layout is generated on first LateUpdate with a generated grid, then only when grid
footprint, surface top, budget or seed changes. Ground Renderer.bounds.max.y supplies
the surface height; without a Renderer the explicit world surfaceY defaults to 0.5.
Root lift is 0.005. This v1 requires a flat axis-aligned battlefield.
Runtime-created meshes and materials are final procedural assets, not placeholders.

The zone registry remains the only zone buffer owner. This branch deliberately has
no compile dependency on the parallel T3 registry implementation. After the registry
publishes in LateUpdate (-1000), a stage bridge must call before grass LateUpdate (10000):

```csharp
field.SetZoneSnapshot(registry.Buffer, registry.Count);
```

Use the same snapshot that the registry publishes as `_HL_Zones` / `_HL_ZoneCount`.
Bind even when count becomes zero, and replace the borrowed buffer on owner recreation.
At shutdown call `field.SetZoneSnapshot(null, 0)` or `field.Release()` before releasing
the owner. Grass never allocates, publishes or disposes a zone buffer. Missing or
invalid borrowed buffers suspend submission. Re-enabling recreates owned resources.
`Initialize(grid, ground, camera, buffer, capacity)` is also available for injected
references; the shader assets must still be serialized. No singleton or gameplay
Generate call is made.

The two opaque indirect draws are exclusive strip/cone ID lists. A third small draw
handles rings: fixed 64 instances read the same global snapshot, with degenerate
vertices for unused/non-Heal-or-Range slots. This avoids another mutable CPU zone snapshot,
an ID map, or GPU readback. Ring width is 0.025 (clamped for small radii), surface
lift 0.020, strength/onset controls opacity, and hostile influence suppresses the ring.
The clear band lowers non-hostile grass to 0.005 at full strength and smoothly restores
it outside the band. Ring white is an albedo evaluated by the shared look, not emission.

## T1 depth and normal requirements

Enable HLOutlines Depth Normal Edges in the stage renderer. Grass has no HLOutline
or ShadowCaster pass. `UniversalForwardOnly`, `DepthOnly`, and `DepthNormalsOnly`
all use the identical indirect ID and deformation function in HLGrassData.hlsl.
Depth normals use URP world normals or `_GBUFFER_NORMALS_OCT`, matching the installed
URP DepthNormalsPass and T1 edge feature. The outline feature must request camera
depth and normals; it must not depend on enumerating Renderer components to find
these indirect draws. Verify their participation in the integrated URP frame debugger
and compare colour/depth/normal silhouettes, including the low orbit. Culling is for
one explicitly assigned camera; SceneView and reflection cameras do not reuse it.

The shader computes the exact contract illumination then calls HLEvaluateSurface
once. The current checkout's frozen look core is a pass-through placeholder until
T1 lands. Palette hex values are converted once in C# to the active working colour
space. No second fog, hatching, lighting threshold or outline implementation is added.

## Benchmark procedure

Add HLGrassBenchmark beside the field and assign its field reference. Each enable
or ResetCapture discards 120 warmup frames, captures exactly 300 steady-state frames,
then logs whole-frame average and p95 milliseconds, blade/zone count, resolution,
URP MSAA, GPU, CPU, Unity and application version/development-build status. It waits
for the field to be ready. The harness makes no scene or zone mutations.

T7 runs a 1920x1080 player build for 0/16/64 zones, full heal, full hostile and mixed
overlap at all three camera orbits. Label each scenario, reset between runs and record
v-sync/frame cap alongside the logs. Use Unity's profiler for grass GPU and CPU timing;
wall time includes waits and is not a grass-only GPU measurement. Targets remain
whole-frame p95 <=16.67 ms, grass GPU <=3.0 ms, main thread <=0.5 ms. If missed, first
select 32,768 blades. The reference target is base M1, not the test host's M2 Max.

No 300-frame staged performance measurement was possible in these EditMode runs.
Metal readback and shader compilation validate operations, not visual appearance or
rendering cost. See VERIFY.md for exact commands and results.

## Wave 5 dynamics

Zone v3 preserves the buffer stride and 65,536 default blade budget. Range gently brightens
and bends blades outward with an edge ring. Bruise flattens and darkens without cones.
Hostile cone height rises over 0.15 seconds and sinks with the pulse decay; the cone list
retains the geometry during sinking. Heal radius blooms over 0.3 seconds in HLLoadZone,
so the blade footprint and ring agree. Launch fronts travel over 0.4 seconds, bowing grass
along the heading stored in the reserved lane. See Zones/README.md for the wire encoding.

TriggerGust(Vector3 towardTarget) doubles the authored wind amplitude for 0.5 scaled seconds.
HLLaunchWave calls it from Projectile.Init; latest launch wins. Wind returns to its authored
heading/amplitude afterward. These elapsed times are cosmetic envelopes of simulation events,
not invented cooldown or cast state. Existing ambient wind remains unchanged between launches.
