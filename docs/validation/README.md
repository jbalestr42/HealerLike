# UI Toolkit validation

Validated in the isolated project copy with Unity **6000.6.0f1**.

- Runtime EditMode assembly: **273 passed, 0 failed, 0 skipped**. See `EditMode-results.xml`.
- Final scene smoke rerun: **2 passed, 0 failed** (`Scene-smoke-results.xml`), including native EventSystem UI hits, battlefield click-through, and detach/reattach restoration of the original UI.
- Scene smoke tests enter Play mode, focus and submit the Toolkit start button, verify menu navigation to `MainToolkit`, start the run, and verify party cards, spell cards and the wave action.
- Inventory tests verify transfers preserve slot identity and produce exactly 3×/2×/1× equipment applications and matching removals through the real Entity handlers.
- Icon tests cover deterministic pixels, semantic variants, missing/unknown data, factory/runtime descriptor consistency and catalog artwork overrides.
- Batch asset validation passed: both UXML templates instantiate, both demo scene assets exist, and catalog coverage matches **all 169 game data assets**.
- **169 generated PNGs** and a source-linked icon catalog are included. Discovery covers project-owned runtime assemblies, including rendering data.
- `Toolkit-Gameplay.png` captures the UI layer from a started gameplay scene at 1600×900. The battlefield is transparent in this offscreen panel capture; it is not a full camera screenshot.

Reproduce the runtime test suite with the Unity Test Runner's `HealerLike.Tests.EditMode` assembly, or:

```sh
Unity -batchmode -projectPath /path/to/project -runTests -testPlatform EditMode \
  -assemblyNames HealerLike.Tests.EditMode -testResults /tmp/healer-tests.xml \
  -logFile /tmp/healer-tests.log
```

The original player initialization still logs `Not Enough inputs` for the existing demo data. The smoke test explicitly expects this one known diagnostic; it does not ignore other errors or change the player code.

The existing project also emits legacy asset import warnings, an A* editor update-check HTTP warning, and optional AI-package service messages. These are separate from the passing runtime test results. The render-specific test assembly and a full multi-wave manual gameplay session were not run for this UI change.

## Plug-in isolation

All original C# and assembly-definition files are byte-for-byte identical to the copied baseline. See `Legacy-source-integrity.json` for the baseline commit and checked file count. State reads that lack a public accessor are isolated in `LegacyUiReader`; no private fields are written. Public gameplay actions/events remain authoritative. `Integration/link.xml` preserves the read bindings for stripping; an IL2CPP player build was not run.

Inventory visual reconciliation is implemented in our own `LegacyInventoryAdapter`. Unity's native UI Toolkit/EventSystem interoperability replaces the earlier change to `InteractionManager`. Original Build Settings are also restored; the added editor launcher loads copied scenes by asset path. The previously recorded test runs predate this final navigation adjustment. The updated code compiled and the Open Demo command completed in the visible editor; the complete suite has not been rerun after that adjustment.
