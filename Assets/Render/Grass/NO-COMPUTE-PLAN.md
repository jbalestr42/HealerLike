# Grass without compute: plan, not implemented

Status: agreed idea, **not started**. Do not implement before the device benchmark below says it is worth it.

## Why

The ground simulation (`GroundSimulation`) already runs in pixel shaders. The only compute left in the grass
is the tuft pass, `Assets/Render/Shaders/Grass.compute` (`HLUpdateGrass`), dispatched from
`GrassField.Dispatch` (`GrassField.cs`). Each frame it:

1. runs once for every tuft in the field, up to the 98,304 budget, including the ones off screen;
2. reads the zones (heal, hostile, range, bruise) and the ground textures (motion, crush, state) to work out
   each tuft's lean and state;
3. frustum-culls each tuft and appends the visible ones to an append buffer;
4. `CopyCount`s that count into indirect arguments for two instanced indirect draws (tufts and socles, `GrassDraw`).

That is the part most at risk on mobile. Append buffers, `CopyCount`, and structured buffers read in vertex
shaders are poorly supported or slow on many Android GPUs, and devices without compute get no board grass
at all (`GrassField` checks `SystemInfo.supportsComputeShaders`). There is also no distance thinning today.

## The plan

Remove compute completely, with identical visual results:

- **Tuft data in a texture.** Bake each tuft's fixed data (position, yaw, height, seed: today's `TuftSeed`)
  into a texture once, when the field is built.
- **Per-tuft work stays once per tuft, in a pixel pass.** A small render texture holds one pixel per tuft. A
  fragment pass writes each tuft's lean and state into it, using the same math as `HLUpdateGrass`. This is
  the same render-texture trick `GroundSimulation` uses. The zones must reach that pass as a texture or a
  constant array, not a structured buffer, to stay GLES 3.0-safe.
- **The vertex shader reads one texel per tuft** from that texture and bends the blade as today
  (`GrassBladeMesh` / `GrassTuft` mirror the HLSL; keep them in sync).
- **Chunked draws.** Cut the field into tiles of a few hundred tufts. The CPU culls a few dozen tile bounds
  against the camera and issues one instanced draw per visible tile. There is no append buffer, `CopyCount`
  or indirect arguments.
- **Thinning with distance.** Store each tile's tufts in random order, so a far tile draws only the first part
  of its list. It gets sparser smoothly, without popping or a visible pattern.

### Do not do the naive version

Moving the per-tuft work into the vertex shader is **not** the plan. It would repeat the ground and zone
reads for every vertex of every blade (about 10 to 20 times per tuft) and can be slower on phones than today.

## Expected trade-offs

- **Better:** fewer driver-hostile features, grass on GLES 3.0 and no-compute devices, and a real saving from
  distance thinning. It should be clearly better on mid-range and low-end Android.
- **Worse:** culling is per tile, not per tuft, so a few extra tufts draw at the screen edges. There are a few
  dozen draw calls instead of 2, which is fine with instancing but costs a little CPU.
- **About the same:** high-end phones.
- **Honest summary:** equal or better on every device is the estimate, not a measurement.

## How to decide

1. Benchmark the current compute path on the target Android device with the same battle
   (the reference numbers had the grass at about 0.3 ms of CPU).
2. If it is too slow, build the no-compute path **behind a switch**, keeping the compute path.
3. Benchmark both on the device with the same battle, then **delete the loser**, so only one path stays.
4. Tests: EditMode tests for the new tile layout, thinning order and CPU mirror, as for everything else here.

Everything that moves the grass goes through `Ground` / `GroundVocabulary` and lands in the ground textures,
so producers (effects, spells, creatures) are unaffected by this change.
