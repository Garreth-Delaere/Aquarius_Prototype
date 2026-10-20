# Channel water: draw, drop, re-bend

Open Aquarius > Open Channel Water Test and press Play. The player starts beside a shallow channel along the left side of the course. Look down toward its blue surface.

- Hold LMB on the channel to draw a stream. Aim upward or sideways and use WASD to guide it.
- The leading target stays at the camera distance established when grabbing. Fast swipes increase response.
- Release LMB: drawn water loses its support, falls, and settles into low-poly patches on the ground/platforms.
- Aim at a landed patch within 6 m and hold LMB to bend nearby droplets again. No new water is created when re-bending.
- Return to the channel to draw more water. Earlier puddles remain where they landed.
- Escape/focus loss also releases water. R resets the player and releases the stream, preserving water. Backspace explicitly clears drawn water.

Inspect WaterStream on Drawn water for per-draw amount, droplet size, gravity, pickup radius, response, and maximum stream length. Inspect StreamManipulator on Player for reach and swipe threshold.

The channel is an unlimited source; each draw currently contains up to 160 droplets. There is a separate 4096-droplet performance budget (roughly 25 full draws). At that budget, new draws stop with an on-screen notice; existing water remains re-bendable. Backspace clears it. This cap is adjustable before Play Mode.

This is controlled particle motion, not a fluid simulation. A held stream follows the traced path, with small moving lanes and inertia. Released particles use gravity and sphere sweeps against static geometry. Settled particles sleep until grabbed. They do not merge into a simulated liquid surface, push the player, or collide with one another. They remain throughout the current Play session; stopping Play resets runtime water.

Freezing and solid traversal collision are still the next milestone. The previous blob scene is retained for comparison; use ChannelWaterTest for the current interaction.

Playtest:
1. Draw upward, sweep sideways onto the main floor, then release.
2. Verify the water drops rather than hanging in place or returning to the channel.
3. Walk to that puddle and lift it again. Try a different direction.
4. Draw a second stream from the channel; the first puddle should remain.
5. Repeat on a raised platform and against a wall; check for obvious tunneling.
6. Compare slow/fast swipes and walking while bending. Test Escape/resume and R.

Aquarius > Validate Channel Water runs deterministic checks of the actual simulation in a temporary scene, recording its result in Temp/ChannelWaterValidation.txt. It checks gravity, contact, persistence, re-picking, no teleport, no duplication, source replenishment, and explicit reset.

Validation result: PASS in an isolated Unity 6000.3.26f1 batch-mode project. Actual simulation checks passed for finite draw size, lift, release gravity, floor contact, persistence, targetability, no teleport on pickup, no duplicated water, source replenishment, retained earlier puddles, and explicit reset. The batch editor exited with code 0. Particle rendering and mouse feel still need interactive Play Mode feedback.

