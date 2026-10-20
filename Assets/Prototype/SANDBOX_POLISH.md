# Sandbox polish

Use Aquarius > Open Freeze Traversal Test. Stop Play Mode, let Unity import, and reopen the scene so the saved jump tuning is loaded.

Changes:
- Jump height is now 1 metre, replacing the 0.15 m elderly-character jump. Speed, gravity, air control, stay the same. The current climb limit is 55 degrees and air steering acceleration is 18 m/s². The lower 0.8 m ledges can now be jumped onto intentionally while we focus on water/ice feel.
- Frozen ice has two passes of endpoint-preserving path/width smoothing, plus an eight-sided beveled cross-section with a flat walking surface. Collision is generated from the same mesh. Separate water patches remain separate.
- Droplets use a rounded, smoothly lit 32-triangle mesh rather than the original 8-triangle octahedron. The number and simulation of droplets are unchanged.

Performance controls:
- WaterIceSurface: Smoothing Passes (0-2) and Edge Bevel. Changes apply on the next freeze.
- WaterDropletRenderer: Mesh Detail (0 = 8 triangles, 1 = 32 triangles). Set before Play Mode.
- A typical uninterrupted 160-droplet draw uses approximately 2,000 ice triangles with default sampling, built only on freeze. The existing 4096-droplet budget caps visible liquid geometry at about 131,000 triangles. These are geometry budgets, not an FPS guarantee; low-end hardware still needs profiling.

Test: draw a gently curving ramp, freeze it, walk up/down, and jump on/off. Compare the edges and small changes of direction to the previous version. Repeat thaw/re-bend/freeze and try the bridge gap.

Validation: Unity 6000.3.26f1 automated checks passed for actual capsule ramp ascent/descent, bridge crossing, continuity of the climbable surface, whole-water freeze, immediate thaw collision removal, resumed falling, player overlap rejection, repeated toggles/reset, and no artificial connection between separate puddles. All prototype scripts also compile against the installed Unity and Input System assemblies. Visual feel and performance on the intended low-end machine remain playtest checks.

Later experiments (recorded only; not implemented):
- Slippery ice and deliberate control of sliding.
- Encapsulating objects in water.
- Using streams to push or bring objects toward the player.
- Independently frozen and liquid streams are now implemented in INDEPENDENT_ICE.md. Using liquid to push objects onto/off ice remains a later experiment.
- Return to SCAMPER after the user's itch.io game analysis.

Movement update: press WASD after a stationary takeoff to steer in the air. Supported steep surfaces now allow a jump, suppressing the sliding impulse at takeoff. No extra midair jump is granted. Defaults and all existing prototype scenes use a 55-degree climb limit and 18 m/s² air acceleration; both remain Inspector settings on FirstPersonMotor.


The automatic step height is now 0.20 m (previously 0.12 m), to handle small lips at steep ramp/platform transitions. To playtest, jump from rest, press W/A/S/D only after takeoff, reverse direction in the air, and jump away from a steep supporting ice face. The movement test room's 50-degree ramp is now intended to be walkable.


- Ledge climbing / mantling onto player-created ice ledges: banked for a later milestone; not implemented. The updated direction explicitly retains the current responsive movement rather than restoring elderly-character restrictions.


