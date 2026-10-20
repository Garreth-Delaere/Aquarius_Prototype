# Pillar course — following stream tail

Open **Aquarius > Open Pillar Course** (or `Assets/Scenes/PillarCourse.unity`) and press Play.

This is a deliberately small rollback-and-adjustment milestone. The previous thin-particle water rendering, emission/pickup behavior, spring/drag/swipe response, physical waves, scroll depth, and ice mesh/collision code are restored from the checkpoint immediately before the finite-volume/fire experiment. The pillar course and movement are unchanged.

The one gameplay change is in `WaterStream.UpdateGuide`: its original gesture trail is no longer fixed to the source. Earlier points drift behind the guided head; at the existing maximum length, the oldest trail segment gives way so the head can keep moving. Newly emitted droplets join the moving tail. Broad/curved guidance therefore leaves a curved following stream rather than a permanently anchored tether. Particle size remains the original 0.075 m radius; no compact large-volume blob or volume-based thickness system is active.

`WaterStream.tailFollowFraction` is the only new tuning parameter (default 0.2). Higher values carry more of the previous curve along with head movement; lower values leave a longer gesture trail before the 14 m rolling limit brings the tail along. Existing responsiveness, wave and scroll settings remain available. The HUD text at full stream length now describes the following tail.

Playtest only these points before changing anything else:

1. Draw water from the first pillar basin. Compare its size and responsiveness with the earlier accepted version.
2. Make a broad S gesture, then move along the deck while holding LMB. Look for a curved stream following your guidance and a tail that leaves the basin.
3. Scroll in/out, freeze a curved bridge, and check its walking surface. Create another structure, melt one selectively, then release and re-grab water.

The moving tail also changes where a bridge's start ends up. Placing and freezing a bridge onto the pillar edge needs manual playtesting; the old source anchor no longer holds that end in place. Automated traversal tests inspect the actual new frozen location rather than assuming its start stays at the old source.

The finite-water/fire code, tests and notes are preserved in `.checkpoints/banked-finite-water-and-fire`, outside Unity's active Assets. The fire station was removed by restoring the earlier sandbox scene. The full prior iteration also has a `before-stream-tail-rollback-*.zip` checkpoint. Nothing further is being developed in the fire experiment during this step.

Verification: Unity 6000.3.26f1 isolated batch checks cover a measurable curved trail, whole-stream transport over 40 m, continued head following beyond the original source reach, bounded length, exact freeze snapshots, walking on the actual frozen mesh, independent ice, targeted melt, falling/re-grabbing, previous scroll controls, continuous physical waves, source/release/settling behavior, movement/air steering and the pillar course. Subjective feel and visual comparison in the normal URP scene remain for Play Mode.

Further work waits for this playtest. Subsequent iterations should change one behavior at a time.
