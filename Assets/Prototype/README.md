# Current prototype

Current playtest: **Aquarius > Open Pillar Course**. The earlier thin stream/ice behavior is restored, with one adjustment: a free following tail instead of a source anchor. See FOLLOWING_STREAM_TAIL.md. The finite-water/fire experiment is banked outside Assets; wait for feedback before expanding this step.

Latest control update: scroll while bending to push/pull water along camera forward. Held water now has coherent physical waves and catch-up resistance. See WATER_TACTILE_CONTROLS.md for Inspector settings, independent tests, and validation results.

For the new representative level, use **Aquarius > Open Pillar Course**. See PILLAR_COURSE.md for the level and BRAINSTORMING_WORKBENCH.md for the planned SCAMPER process. The existing sandboxes below remain available.

Use **Aquarius > Open Freeze Traversal Test** for the latest draw/drop/re-bend and independent ice construction. See INDEPENDENT_ICE.md for current freeze/melt controls and test steps. See SANDBOX_POLISH.md for smoother ice, rounded droplets, and the current 1 m jump. Earlier scenes remain available for comparison.

# Movement milestone

In Unity, let scripts import, then choose Aquarius > Open Movement Test and press Play.
If the menu has not appeared, use Assets > Refresh and check the Console.
The editor builder creates Assets/Scenes/MovementTest.unity on its first successful import.
It preserves the currently open scene and does not change build settings.

Controls: WASD, mouse, Space (1 m prototype jump), R (reset), Escape (release cursor), click (resume).
Select Player to tune FirstPersonMotor and FirstPersonLook in the Inspector.
Changes made during Play Mode are temporary unless copied back after stopping.

Acceptance checks:
- Walk forward and diagonally: diagonal speed should not be faster.
- Walk into the back/left walls: no passing through them or camera clipping.
- Walk over the 0.10 m step; test the small jump on the 0.22 m block.
- The 1 m ledge should remain inaccessible from the floor.
- Ascend and descend the green 20-degree ramp without hopping or losing ground contact.
- The red 50-degree ramp should reject uphill movement and slide the player down.
- Holding Space must not repeatedly jump; pressing in midair must not double jump.
- Walk off the open floor edges: falling below -8 m resets the player.
- Escape releases the cursor and stops new movement input; click resumes without a sudden look jump.
- Check the Console for exceptions while repeating these tests.

Validation: the three scripts compile together against Unity 6000.3.26f1 and the project's Input System assembly. Interactive Play Mode testing is still required.

No water deformation, freezing, bridge course, or fall-damage system is included in this milestone.







