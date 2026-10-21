# Pillar course — gesture-driven stretching

Open **Aquarius > Open Pillar Course** (or `Assets/Scenes/PillarCourse.unity`) and press Play.

This iteration changes one behavior: how quickly the liquid tail catches the guided head. It uses the existing thin droplets, mouse/body guidance, scroll depth, spring resistance, physical waves and independent freezing/melting systems. Particle size, ice geometry, player movement and level geometry are unchanged.

The tail now travels along the recorded curve every simulation step, including when the player stops. Slow guidance cannot leave a permanently extended trail: the tail keeps up and the water gathers into a short shape. Fast guidance outruns the tail and leaves a longer curved wake. Freezing during that motion captures the real current shape. Stopping allows it to gather again; ice that has already been frozen stays unchanged.

This replaces the previous movement-fraction rule, which let a long trail remain even after very slow construction. No volume/thickness or fire changes are included.

## Tune only these settings first

Select **Drawn water - persistent droplets > WaterStream > Gesture stretching**:

| Setting | Default | Purpose |
| --- | --- | --- |
| Tail Catch Up Speed | 3.5 m/s | Tail travel along the curve while guiding. Increase to require stronger motion; decrease to make long gestures easier. |
| Gathering Speed | 2.5 m/s | Tail travel once the head settles. Increase for a shorter freezing window; decrease to allow more time. |
| Compact Trail Length | 1 m | Short resting trail. This changes length, not droplet size. |

Speed is measured from the actual smoothed head movement, not solely from mouse speed. Walking and scrolling can also move the water. The transition between moving/stationary catch-up is blended. Existing follow resistance remains, so the visible water responds gradually. The previous Tail Follow Fraction field is retained as hidden legacy scene data and is no longer active.

The existing Maximum Stream Length remains a safety bound. Newly drawn droplets join the moving tail instead of staying at the source. Continuous waves still affect real positions and the frozen mesh.

## Focused playtest

1. Draw water, then guide it slowly across the deck. It should follow as a compact shape, without growing into a long slowly drawn bridge.
2. Make a quick broad sweep or S gesture. It should stretch into a longer curved stream. Press **F during the gesture or shortly afterward** to catch that shape.
3. Repeat the quick gesture, then hold completely still. The tail should progressively gather, with no sudden snap and no increase in droplet size.
4. Walk on the frozen stream; create another, selectively melt one, then release and re-grab liquid. Scroll should still push/pull along eyesight.

Watch the platform's start as well as its head: gathering changes both the stream length and where its tail ends. Landing a fast-drawn bridge on both pillar edges remains a manual Play Mode check. This step is for tuning gesture feel, not redesigning the course.

Inspector edits during Play Mode are temporary unless copied back after stopping. Start by changing one setting at a time. Further changes wait for playtest feedback.

## Recovery

The prior following-tail version is Git commit `5dc47b1`. This iteration is left as a reviewable local diff until playtested. The earlier finite-water/fire experiment remains banked locally outside Assets.

See FOLLOWING_STREAM_VALIDATION.txt for the latest measured checks and their limits.

Latest automated result: the same 12 m path produced physical spans of 1.01 m slowly and 5.61 m quickly. The quick shape gathered to 2.59 m after two seconds, then settled compactly. A fast curved gesture retained a 1.78 m bend. The platform frozen during the quick motion supported an actual CharacterController. Scroll, physical waves, release/reuse, targeted melt, movement, pillar-course checks and isolated overlapping-ice collision checks passed. URP rendering and subjective gesture feel remain manual checks.
