# HealerLike

## Testing rule (mandatory)

Whenever a change adds new gameplay logic - a new `AttributeModifier`, `AConsumer`, `AValue`,
buff, skill, utility class, or any other testable feature/behavior - it MUST come with EditMode
unit tests covering it, in the same change. This applies to new files and to significant new
logic added to an existing file. Don't defer "add tests later" - write them alongside the feature.

Tests live in `Assets/Scripts/Tests/EditMode/`, one test file per class being tested (e.g. a new
`Assets/Scripts/Attributes/Modifiers/PoisonModifierFactory.cs` gets
`Assets/Scripts/Tests/EditMode/Modifiers/PoisonModifierTests.cs`, in namespace
`Attributes.Modifiers` - see the namespacing note below). The test assembly is
`HealerLike.Tests.EditMode` (see its `.asmdef`); it references `HealerLike.Runtime` only, so a new
class under `Assets/Scripts/` is automatically visible to tests without extra wiring, as long as it
isn't Editor-only code (that belongs in an `Editor/` subfolder instead, which is NOT visible to the
runtime or test assemblies).

**A modifier implementing `IStackableBuff` (`Stack(source, target)`/`Unstack(source, target)`) MUST
have tests for both**, not just `ApplyModifier()`. At minimum: one test proving `Stack()` changes the
applied value in the expected direction, and one proving `Unstack()` reverses it (see
`FlatModifierTests`/`UpgradeModifierTests`/`SlowModifierTests`/`TimeModifierTests` for the pattern -
each modifier's stacking formula is different, so don't assume simple accumulation without checking
the actual implementation first).

**Namespacing:** group related test classes under a namespace matching the production folder they
cover (e.g. `Attributes` for `AttributeTests`/`ResourceAttributeTests`, `Attributes.Modifiers` for
everything under `Tests/EditMode/Modifiers/`) - Unity's Test Runner window groups by
assembly > namespace, not by disk folder, so this is what actually organizes the tree in the UI.
Pick the plural form when a production class shares the singular name (e.g. namespace `Attributes`,
never `Attribute`) - a namespace with the exact same name as a type you reference unqualified inside
it causes `CS0118 'X' is a namespace but is used like a type`.

**Reuse `Assets/Scripts/Tests/EditMode/TestHelpers.cs`** rather than re-inventing scaffolding:
- `WithLoggingDisabled(Action)` - suppress expected `Debug.LogError`/exception logs (both the
  managed and Unity's internally-caught-and-logged kind) so the Console stays clean during a
  passing test run, without changing production log behavior.
- `InvokePrivate(obj, methodName)` / `SetPrivateField(obj, fieldName, value)` - EditMode tests run
  outside the normal player loop, so `Awake()`/`Start()`/`Update()` are NOT invoked automatically
  by `AddComponent`, while Editor-only messages like `Reset()` ARE invoked synchronously by
  `AddComponent` and will NRE on any MonoBehaviour whose fields are normally wired by a heavier
  `Init()` you're intentionally not calling in a test. Use these two helpers to work around both.
- `CreateAttributeManager(go[, type, value])` / `CreateResourceAttribute(go, maxType, maxValue)` -
  build a working `AttributeManager`/`ResourceAttribute` without the full `Entity`/`Character`
  `Init()` chain.

**Do not trigger `Singleton<T>.instance`** (e.g. `GameManager.instance`) from a test - it can create
a stray `DontDestroyOnLoad` GameObject in whatever scene happens to be open in the Editor, or return
a live object from the user's actual project, making the test both unsafe and non-deterministic.
If a class's logic is gated behind a singleton lookup, test the independent, deterministic pieces
around it instead (see `CurrentWaveModifierTests.cs` for an example of this trade-off).

Run tests via Unity's Test Runner window (Window > General > Test Runner > EditMode), or ask
Claude to do it via the `mcp__unity-mcp__` bridge - `Unity.exe -batchmode -runTests` only works
when no Editor instance already has the project open.
