# Wave 3 integration handoff

Placeholders and external work still to replace or connect:

1. Frozen `Assets/Render/Shaders/HLLookCore.hlsl` bodies are the wave-0 pass-through
   placeholder owned by T1. Integrate T1's completed include and look controller on
   the shared integration branch. Grass already calls the frozen HLEvaluateSurface
   signature; no copy of T1 code was made here.
2. Zone source is an explicit borrowed-buffer seam until the stage bridge wires
   `HLZoneRegistry.Buffer` / `.Count` to `HLGrassField.SetZoneSnapshot` after T3
   publication and before grass LateUpdate. The registry on origin/zfc-zones was read
   to align these names. There is no dummy buffer, fake persistent heal field, or
   replacement registry in this track. Null means no submissions.
3. T7 must assign the grid, ground, gameplay camera and three shader assets, and place
   the field and optional benchmark. No placeholder material or prefab from creatures
   or stones is used. Runtime-created grass materials/meshes are the intended assets.
4. T1/T7 must enable the depth/normal outline feature and verify indirect grass is in
   both requested textures, with matching deformed silhouettes. No per-blade hull.
5. Run the documented 300-frame performance matrix and visual review at all orbits,
   including heal rings, overlaps, owner replacement and scene unload/re-enable.
   Current GPU tests passed on M2 Max but no staged benchmark or visual capture exists.

## CONTRACT-CONFLICT

The older grass spec proposes HLShade and HLGrassRenderer/HLBladeLayoutGenerator.
The frozen contract's HLEvaluateSurface and the task's HLGrassField/HLGrassLayout/
HLGrassBlade/HLGrassCone names win. Frozen zone files and test assembly were already
present, so this track does not implement a second zone packer or edit the assembly.
The older spec's HLHealRing.shader path is outside this task's shader ownership:
HLGrassRing.shader is used instead. Its suggested CPU heal-ID map is replaced by a
fixed 64-instance GPU zone-index draw with invalid/non-heal slots made degenerate;
this consumes the exact published snapshot without requiring an unfrozen CPU API.
The spec's read-only/no-second-Editor constraint described its design phase. This
build task expressly authorizes batchmode in this dedicated clone; the live gameplay
checkout was not opened or modified.
