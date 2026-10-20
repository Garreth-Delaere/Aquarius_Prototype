# Sculpting depth and tactile water motion

Open Aquarius > Open Pillar Course or Open Freeze Traversal Test. Stop Play Mode and let Unity import the new scripts before testing. The existing scenes and controls are retained.

## 1. Mouse-wheel depth

While holding LMB on a source or loose water, scroll forward to push the sculpting target away, and backward to pull it closer. The target is camera.position + camera.forward * heldDistance: looking up/down changes the displacement axis with your eyesight. Walking moves the target with you.

The adjusted distance persists until releasing that grab. A new grab starts from its hit distance, bounded by the same limits. Source selection and targeted melting still use the existing 6 m reach; sculpting depth defaults to 1-10 m. Stream-length and collision constraints still apply, so the target distance is not a guarantee the water can reach through an obstacle or beyond the mass's maximum length.

Mouse Input System 1.20 defaults to normalized wheel steps. Legacy Windows scroll settings are normalized too; no project-wide input setting is changed. Smooth-wheel/trackpad deltas accumulate proportionally, without multiplying wheel input by frame time.

On Player > StreamManipulator:
- Scroll Sensitivity: 0.6 metres per normalized wheel step.
- Minimum Manipulation Distance: 1 m.
- Maximum Manipulation Distance: 10 m.

## 2. Continuous physical motion and resistance

The requested target passes through a bounded, damped catch-up stage before extending the sculpting guide. Individual droplets continue to follow through the existing spring/drag forces. Both mouse input and walking therefore produce a visible physical lag. Fast swipes still increase pull strength and slightly quicken guide response, while retaining inertia.

A smooth travelling wave bends the stream sideways and vertically. It is coherent over neighbouring particles, with gentle endpoint weighting, rather than independent random jitter. It changes the actual simulated droplet positions. Freezing stores these current positions, and the existing smoothed ice mesh/collider are built from that snapshot. Frozen structures do not continue to wobble.

No additional particles or mesh detail are added. Target smoothing is evaluated at the existing 90 Hz simulation step, and wave forces apply only to the held selection. Released water keeps its current velocity, falls, and settles using the previous contact logic.

On Drawn water > WaterStream:

| Parameter | Default | Effect |
| --- | --- | --- |
| Follow Responsiveness | 4.5 | Lower values give a slower, heavier catch-up; higher values respond sooner. |
| Maximum Follow Speed | 12 m/s | Caps guide movement during large target jumps. |
| Wobble Amplitude | 0.22 m | Strength of broad physical undulation. Set to 0 to isolate depth/resistance. |
| Wobble Speed | 0.6 cycles/s | Wave timing. Lower values feel slower. |
| Wobble Cycles | 1.1 | Broad bends along the held stream; more cycles make tighter undulations. |
| Gentle Response / Fast Response | 28 / 65 | Existing droplet pull strengths; these retain swipe-speed differences. |
| Held Drag | 7 | Existing droplet velocity damping; higher values suppress overshoot and increase resistance. |

Inspector edits made while playing are temporary. Very large amplitudes or weak following can make paths hard to shape; tune one parameter at a time. Contact with floors/ice can constrain a wave, so the free-air motion test is the clearest comparison.

## Independent playtests

Depth only: set Wobble Amplitude to 0, grab water, and keep the mouse still while scrolling both directions. Tilt the camera and repeat. Stop scrolling, then walk/turn: the displayed depth should remain stable. Test both limits. Idle scrolling and scrolling while aiming at committed ice should not move it.

Resistance only: keep Wobble Amplitude at 0. Swipe, walk sideways, or make a large depth change, then stop. The water should visibly catch up. Compare Follow Responsiveness at 2, 4.5, and 10. Faster swipes should still produce stronger stretching.

Physical motion: restore Wobble Amplitude to 0.22 and hold an elevated stretch still after it fills. Watch broad changes of shape with no mouse or body movement. Freeze at different points of the wave and compare the ice shapes. Thaw and re-bend; existing independent ice must stay fixed.

Regression: freeze a ramp and bridge, cross their join, melt only one, re-bend its water, release it to settle, and complete the pillar-course source/reset checks. Check the Console for exceptions.

## Automated checks

Aquarius > Validate Water Depth tests direction, camera axes, depth persistence, camera movement, min/max limits, and idle/frozen isolation.

Aquarius > Validate Water Motion tests actual target lag, physical catch-up, retained swipe-speed differences, actual frozen mesh changes one second apart, exact droplet snapshots, and stationary committed ice. Its fixture measured approximately 0.233 m RMS frozen-surface change, compared with 0.044 m when the coherent wave was disabled; this is not a universal motion amount for all shapes/contact situations.

The combined suite also runs liquid, independent ice/join, pillar-course, and player-movement validations. Hardware wheel feel, visual fluidity, and subjective controllability remain interactive Unity playtests.

A saved rollback archive and checksum manifest named before-water-depth-motion are in .checkpoints. Future movable, throwable, breakable, or temporary ice systems are not part of this milestone.

Final combined validation: PASS in isolated Unity 6000.3.26f1, exit code 0. Depth, physical wave/lag, release/settling, independent ice and join traversal, selective melting/restoration, source budgeting, pillar course/reset/goal, and existing jump/slope/air-control suites all passed with the new defaults. No future ice-interaction phase was implemented.

