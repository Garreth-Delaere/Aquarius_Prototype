# Liquid sculpting milestone

1. Stop Play Mode and let Unity import the scripts. Use Assets > Refresh if needed.
2. Choose Aquarius > Open Liquid Sculpting Test. The scene is created from the saved MovementTest scene on first import or menu use.
3. Press Play. The cyan water blob starts ahead of the player; look slightly downward.
4. Aim at its visible surface within 5 m and hold LMB. Mouse look stays active while bending.
5. Sweep the mouse slowly, then quickly. Walk backward or sideways while holding LMB to stretch it farther.
6. Release, walk around it, and grab another section. Backspace restores the blob; R resets both player and blob.

Expected: a connected shape, stronger response to fast swipes, thinner sections as overall length increases, small elastic settling and sag on release. No solid liquid collider: walking through it is intentional at this stage. Escape/focus loss releases the grab. Clicking to recapture the mouse should not immediately grab water.

Playtest checks:
- Pull an end upward into a ramp shape, then pull sideways along the floor.
- Repeat similar movements slowly and quickly; compare responsiveness.
- Hold LMB while walking: the grabbed surface target stays at the distance established on grab.
- Let go, then regrab from another angle without the mesh snapping to the camera.
- Pull farther than the volume/reach budget permits: it should resist rather than grow indefinitely.
- Test near a wall, a platform edge, and at a low frame rate. Watch for spikes, flickering, NaNs, or Console errors.
- Reset several times, release cursor, switch focus, and resume.

Inspector tuning:
- Player / WaterManipulator: grab reach and swipe speed threshold.
- Water Blob / WaterBody: volume, maximum length, elasticity, damping, sag, pull strength, and affected-point radius.
- Point count is applied on reset; other physical tuning applies while playing.

Implementation limits:
- One non-branching chain with an elliptical section; a nominal volume budget controls overall thickness.
- Shape memory makes sculpting controllable and bounds sag; it is not a physically accurate fluid.
- Liquid support uses centreline probes. The outer surface can intersect obstacles; tight folds can self-intersect.
- The visible material is opaque cyan for legibility. No transparency or fluid shader dependency.
- F and traversal collision are deliberately not implemented yet. These follow interaction feedback.

Validation: all prototype runtime/editor scripts compile against the installed Unity 6000.3.26f1 assemblies and existing Input System. Play Mode interaction remains to be verified by the user.
