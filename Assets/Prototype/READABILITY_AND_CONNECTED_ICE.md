# Water trail readability and connected ice

Open **Aquarius > Open Pillar Course** and press Play.

## Gradient reverted after playtest

The gradient iteration produced a black plane and stray lines during the user's playtest. Its particle shader/material and explicit vertex-stream changes are suspected, but the exact GPU rendering cause is unconfirmed. The water renderer has been restored byte-for-byte from accepted commit ecb7b86, and the new gradient material has been removed from Assets. The attempted implementation is preserved outside Assets in .checkpoints/gradient-rendering-regression-*.

The earlier headless particle-buffer/shader-import checks did not catch this visual defect. They must not be treated as evidence that the gradient renders correctly. The gradient is deferred until it can be developed and visually verified in a smaller step.

## Stronger bobbing, stopped by physical connections

In **WaterIceSurface > Magic ice hover**, default **Hover Amplitude** is now **0.06 m** rather than 0.03 m: 6 cm above/below the hover centre, 12 cm peak-to-peak. Frequency stays 0.35 cycles/second.

Newly frozen ice checks its actual mesh surface for world contact. Touching a static/kinematic floor, wall or other world surface anchors the whole structure at its current position. Connections through other grounded ice also anchor it. Airborne ice connected only to other airborne ice can still hover. The player and ordinary dynamic objects do not count as floor/wall support.

- **Anchor Contact Tolerance**: 0.02 m. Small probes around actual frozen vertices catch flush surface contact; primitive intersections use the mesh directly. The broad bounding box only finds candidates, avoiding false anchoring across empty space within a curve.
- Contact is rechecked roughly five times per second. If support disappears, hovering resumes from the current position without snapping to the original freeze height.
- Visible mesh and traversal collider still move together. Melting still restores water at the currently displaced ice position.

Contact is a lightweight geometry approximation, not welding or structural physics. Very small terrain/mesh contacts between samples may need tolerance tuning. Keep amplitudes modest when constructing narrow or closely overlapping platforms.

**Playtest:** freeze one structure in open air, one touching the ground, and one touching a wall. Only the airborne one should bob. Extend grounded ice with another touching structure and confirm the connection stays still. Stand on floating ice to confirm the player does not switch it into an anchored state. Check joins and actual pillar-edge placements in Play Mode.

## Implementation and verification

Remaining runtime changes: FrozenWaterStructure.cs (contact-aware hovering) and WaterIceSurface.cs (larger amplitude/contact tuning). WaterDropletRenderer.cs matches the accepted baseline. The validation menu is now Aquarius > Validate Ice Anchoring; the failed gradient assertions were removed from the active test.

After Unity reloads scripts, stop and restart Play Mode to discard the previous runtime particle material. Open Aquarius > Open Pillar Course and check that the black plane and stray lines are gone. Draw, release, re-grab, freeze and melt water, then compare airborne versus floor/wall-connected ice. This requires visual confirmation in the Unity Editor.

Changes remain local/uncommitted. No movement, water simulation or level geometry changes were made as part of this rollback.

Post-rollback verification: Unity compilation and all 11 existing mechanics reports passed in the isolated headless project. This verifies the remaining ice behavior and existing water/movement checks, not the appearance of the game view.
