# Wave 3 integration

Re-run `HLStageBuilder.Build` after the other tracks land. No other track source is copied into this branch.



# Calibration

Source: Main (menu loads Main; Build Settings instead enables TestHealer). Board 16 x 16, cell 1; roots y=.505. Portrait camera 52 degrees, FOV 40, position (0.00, 36.93, -27.62). 1920-high hatch spacing 0.06933; fog 53.573/87.573, six bands, pale #BFD2E0, 1px outline. Numerical calibration awaits final shader/grass captures.

# CONTRACT-CONFLICT

The older look spec proposes stock RenderObjects, but the frozen contract requires T1 HLOutlines. The wave-2 fallback is labelled HLOutlines_PLACEHOLDER and must be replaced by the real feature. Main is the menu target but absent from enabled Build Settings; stage copies Main without changing the source scene list. The Ultra quality slot references missing pipeline GUID a0da25f9ff8de264189edd30d9654c37; Graphics Settings falls back to Low. All six existing pipeline assets and their six renderers are covered. The copied scene hides the 100-unit debug ground (its top .51 obscures the board at .5), middle line and debug sphere renderers; their colliders remain unchanged.
