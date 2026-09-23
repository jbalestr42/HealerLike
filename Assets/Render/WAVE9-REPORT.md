# Wave 9: playable reference correction

The actual Game view now draws the grass field, larger authored plant bodies, corrected healer bowl/hip/root geometry, lighter stone faces and segmented attacks. Short board grass exposes root knees. The public Next Wave button brings the battle into focus; a visible Focus/Overview button and automatic round-end restore full-board placement. The render layer preserves Main’s gameplay grid authority and adds no demo blockers.

Owned implementation and evidence reports:

- [Ground, look, stone and zones](Grass/WAVE9-GROUND-REPORT.md)
- [Creatures and spells](Creatures/MATCH-REPORT.md)
- [Stage, environment, gameplay captures and playable delivery](Stage/MATCH-REPORT.md)

The fifteen-second normal encounter records actual health damage, projectile attacks, healing and status events at normal speed. A separate demonstration uses the existing debug inventory’s real ExplodeOnHit item through its inventory UI. Actual Soldier attacks create radius-one areas and their hostile grass footprint. The old filled explosion output is silenced through exposed colours while its particles and natural destruction remain active; measured masked/unmasked controls verify comparable particle counts and lifetime. Each capture has a unique folder and complete timestamped frame manifest.

Verification includes perspective bounds cases, actual camera containment and toggle behavior, three fully completed rounds, the menu/play/loss/restart loop, an explicit two-scene standalone build and startup check. Stage/MATCH-REPORT.md carries exact paths and counts. The final app is `/Users/fc/Documents/healerlike-render-specs/builds/HealerLikeRender.app`. In Editor: **HealerLike > Render > Open Stage Menu**, Play, then Start.

Rendered evidence supports a procedural interpretation of the brief. Roots and hostile grass are readable, and focus improves actor scale, but the composition remains more open and the plant volumes flatter than reference05. This is not a claim of image equivalence or that every historical audit finding is closed. Actual UI callbacks/raycasts are verified; physical mouse/touch behavior is not. Standalone verification covers building and startup; the full route runs in Editor play mode. Source ownership gates exclude gameplay/vendor changes. References to UnityEngine.VFX and Visual Effect Graph are official Unity APIs, not imported assets or code from another project.
