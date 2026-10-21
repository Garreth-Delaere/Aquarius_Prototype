# Water feel — three small polish changes

Open **Aquarius > Open Pillar Course** and press Play. These three adjustments build on the accepted gesture-driven stream. The level, movement, scroll depth, particle count and per-particle size are unchanged.

## 1. Magic ice bobbing

Frozen structures gently oscillate vertically as a solid unit. The root moves both the original rendered mesh and its matching collider; the mesh itself is never rebuilt or deformed. Default travel is 3 cm above/below the freeze position, at 0.35 cycles/second (one cycle about every 2.9 seconds). Structures start at zero offset, so freezing does not jump the surface.

In **Drawn water - persistent droplets > WaterIceSurface > Magic ice hover**, tune **Hover Amplitude** and **Hover Frequency**. These template settings are copied when new ice is frozen; existing structures can be tuned through their own WaterIceSurface component during Play. Amplitude 0 disables bobbing. Keep amplitudes small for comfortable traversal and overlapping joins.

Melting returns saved water at the structure's current displaced position. Creating/melting other ice remains independent. Bobbing is a simple transform motion updated with physics steps; there is no new buoyancy simulation or throwable-ice behavior.

**Test:** freeze a platform, watch a full cycle, stand on it, walk across overlapping structures, then melt one while it is displaced. Water should emerge where the ice is visible.

## 2. Small, round resting water

The resting guide shortens from 1 m to 0.25 m. As the guide gathers, the existing droplets smoothly blend into a sphere-shaped cloud instead of retaining a short ribbon. Fast/long shapes retain the original ribbon layout. Particle size and number do not increase.

In **WaterStream > Gesture stretching**, tune **Compact Trail Length** (0.25 m) and **Resting Radius** (0.22 m). The latter is the radius of the particle centres; with the existing particle size, the visible cloud is about 0.59 m across. Resting Radius 0 disables the spherical layout for comparisons. Continuous motion remains physical; compact waves mostly translate the cloud together rather than flattening it.

**Test:** guide water, stop, and let it gather. Check it from above and the sides. Then sweep quickly and freeze: the rounded cloud should still stretch into a curved traversable stream.

## 3. Faster tracking near the crosshair

During fast gestures, the guided head can follow more quickly. The leading portion of the droplets also receives some guide velocity and a higher travel-speed allowance. The tail retains its original spring/drag lag, preserving the stretched snake shape. Slow guidance retains its previous resistance.

In **WaterStream > Held water motion**, tune **Fast Follow Boost** (3). A value of 1 restores the earlier fast-follow response for comparison; higher values keep the leading water closer to the intended aim point. The same camera-forward target and scroll distance are used. This reduces lag rather than attaching water rigidly to the camera or shortening every stream.

**Test:** compare quick left/right sweeps at boost 1 and 3. Watch the leading water near the crosshair, then stop and check that it gathers smoothly. Repeat while walking and scrolling. Actual aim feel and peripheral visibility still need manual Play Mode testing.

## What changed and what was checked

Runtime changes are limited to WaterStream.cs (resting cloud, fast head response and displaced melt position), FrozenWaterStructure.cs (bounded rigid bobbing), and WaterIceSurface.cs (hover tuning copied into each structure). New WaterFeelValidation.cs checks each adjustment separately. The historical FreezeValidation shape fixture disables new shaping/aiming forces while creating its predefined geometry; defaults are restored afterward. A separate live-sculpted test uses the actual default behavior to cross two independently bobbing platforms with the player's 0.2 m step limit.

Unity 6000.3.26f1 isolated batch checks passed. Measured results:

- Bobbing: 0.060 m peak-to-peak; a real CharacterController crossed two live-sculpted platforms at different hover phases, remained supported over multiple cycles, and melting restored the current displaced position.
- Resting cloud: about (0.59, 0.59, 0.59) m, including particle size; original particle count/size preserved; fast stretching and freezing still worked.
- Fast visible-head targeting error on the same 8 m/s target motion: 2.14 m at boost 1 versus 0.71 m at boost 3. Slow-guidance resistance remained unchanged.
- Gesture speed distinction, gathering, curved shapes, actual ice traversal, independent freezing/melting, scroll controls, physical waves, released-water physics/reuse, movement and pillar-course checks passed.

See WATER_FEEL_POLISH_VALIDATION.txt for reports. Visual URP rendering, subjective gesture feel, camera comfort while standing on moving ice, and actual edge-to-edge pillar placement still require playtesting.

Changes remain local and uncommitted for review/playtesting. The prior accepted version is Git commit d63e69a. No further mechanic expansion is included in this step.
