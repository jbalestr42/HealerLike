# Wave 9: playable reference correction

The first combined Game view exposed missing indirect grass even though camera-only captures showed it. Per-camera submission fixes actual gameplay. Broader blades, lifted jade stone footprints, larger authored plant bodies and segmented attacks are integrated. Placement uses a complete board overview; the real Next Wave event moves smoothly into a closer battle composition, with a visible Overview/Focus control and automatic return after a completed round. Every living allied/enemy body and the healer contribute to the frame. Spread battles widen the view.

Owned implementation reports:
- [Ground, look, stone and zone work](Grass/WAVE9-GROUND-REPORT.md)
- [Creature and spell work](Creatures/MATCH-REPORT.md)
- [Stage, environment and delivery](Stage/MATCH-REPORT.md)

Verification includes pure perspective bounds tests, actual Game view captures with HUD, real menu/play/loss/restart routing, three completed combat rounds, an explicit two-scene standalone build and a player launch check. Recording uses actual inventory placement and validated skill buttons, logs health separately from mana, observes status starts, checks normal speed throughout, verifies each current-run frame and encodes its measured timestamp. Final record details and paths are in the Stage report.

The image comparison, not test counts, remains the visual acceptance measure. The focus view materially enlarges actors, restores visible gameplay grass and makes attacks and healing legible. It does not establish equality with the reference: grass remains denser and noisier, and creature surface variation remains flatter. The recorded clip includes real health damage, allied projectile launches, healing and status events. No transient hostile-area item event was exercised; that remains unverified. Programmatic actual buttons do not verify physical mouse/touch input. Full loop checks run in Editor play mode; standalone verification covers building and startup.
