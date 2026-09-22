# Wave 3 integration

Re-run `HLStageBuilder.Build` after the other tracks land. No other track source is copied into this branch.

- Assets/Render/Stage/Materials/HLAllyPlaceholder.mat: replace URP Lit with HL/Look/Primitive; toon, hatch and banded fog are unavailable in this wave.
- Assets/Render/Stage/Materials/HLStonePlaceholder.mat: replace URP Lit with HL/Look/Primitive; toon, hatch and banded fog are unavailable in this wave.
- Assets/Render/Stage/Prefabs/HLModelChainLightningEntity.prefab -> Assets/Render/Creatures/Prefabs/HLChainLightning.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelChannelingEntity.prefab -> Assets/Render/Creatures/Prefabs/HLChanneling.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelFastShootEntity.prefab -> Assets/Render/Creatures/Prefabs/HLFastShoot.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelHitArmorBufferEntity.prefab -> Assets/Render/Stones/Prefabs/HLStoneCairnModel.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelMultiShotEntity.prefab -> Assets/Render/Creatures/Prefabs/HLMultiShot.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelNormalEntity.prefab -> Assets/Render/Creatures/Prefabs/HLNormal.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelRandomShootEntity.prefab -> Assets/Render/Creatures/Prefabs/HLRandomShoot.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelSoldierEntity.prefab -> Assets/Render/Stones/Prefabs/HLStoneSoldierModel.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelSwarmEntity.prefab -> Assets/Render/Creatures/Prefabs/HLSwarm.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelTestEntity.prefab -> Assets/Render/Creatures/Prefabs/HLTest.prefab (preserves original model sockets and HUD; primitive placeholder).
- Assets/Render/Stage/Prefabs/HLModelTripleShootEntity.prefab -> Assets/Render/Creatures/Prefabs/HLTripleShoot.prefab (preserves original model sockets and HUD; primitive placeholder).
- BulletSpeed: missing HLProjectileVisualObserver; marker has no runtime effects.
- BulletSpeed: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- ChainLightning: missing HLProjectileVisualObserver; marker has no runtime effects.
- ChainLightning: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- ChannelingLightning: missing HLProjectileVisualObserver; marker has no runtime effects.
- ChannelingLightning: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- CurveBullet: missing HLProjectileVisualObserver; marker has no runtime effects.
- CurveBullet: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- CurveBullet2: missing HLProjectileVisualObserver; marker has no runtime effects.
- CurveBullet2: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- CurveSphereBullet: missing HLProjectileVisualObserver; marker has no runtime effects.
- CurveSphereBullet: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- LaserBullet: missing HLProjectileVisualObserver; marker has no runtime effects.
- LaserBullet: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- StraightLaserBullet: missing HLProjectileVisualObserver; marker has no runtime effects.
- StraightLaserBullet: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- SwarmBullet: missing HLProjectileVisualObserver; marker has no runtime effects.
- SwarmBullet: missing HLStoneProjectileImpactBridge; marker has no runtime effects.
- HLArea1: missing HLAreaPulse; marker has no runtime effects.
- HLArea2: missing HLAreaPulse; marker has no runtime effects.
- HLArea3: missing HLAreaPulse; marker has no runtime effects.
- HLRenderStage: missing HLLookController; marker has no runtime effects.
- HLRenderStage: missing HLZoneRegistry; marker has no runtime effects.
- HLRenderStage: missing HLSpellVisualSink; marker has no runtime effects.
- HLRenderStage: missing HLStoneGridEntry; marker has no runtime effects.
- Stone generation: replace marker with HLStoneGrid.prefab generator/entry; preserve both block-system recipes (20..50, size 5..15; 0..10, size 4..15) and final completion fence. Main generator list is empty; no fallback terrain generation runs.
- HLHealerAnchor: missing HLCharacterView; marker has no runtime effects.
- HLGrassFieldPlaceholder / HLGrassPlaceholder.asset: static 4096-blade coverage mesh; replace with T2 HLGrassField and HLStageZoneBridge (after zone publication, before grass submission), bound to board [-8,8] squared at y=.505. No wind, zone response or indirect rendering is claimed.
- Assets/Settings/High_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.
- Assets/Settings/Low_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.
- Assets/Settings/Medium_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.
- Assets/Settings/Ultra_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.
- Assets/Settings/Very High_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.
- Assets/Settings/Very Low_PipelineAsset_ForwardRenderer.asset: replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.

# Calibration

Source: Main (menu loads Main; Build Settings instead enables TestHealer). Board 16 x 16, cell 1; roots y=.505. Camera 50 degrees, FOV 40, distance 31, position (0.00, 24.25, -19.93). 1080p hatch spacing 0.08358; fog 26.570/39.707, six bands, pale #BFD2E0, 1px outline. Numerical calibration awaits final shader/grass captures.

# CONTRACT-CONFLICT

The older look spec proposes stock RenderObjects, but the frozen contract requires T1 HLOutlines. The wave-2 fallback is labelled HLOutlines_PLACEHOLDER and must be replaced by the real feature. Main is the menu target but absent from enabled Build Settings; stage copies Main without changing the source scene list. The Ultra quality slot references missing pipeline GUID a0da25f9ff8de264189edd30d9654c37; Graphics Settings falls back to Low. All six existing pipeline assets and their six renderers are covered. The copied scene hides the 100-unit debug ground (its top .51 obscures the board at .5), middle line and debug sphere renderers; their colliders remain unchanged.
