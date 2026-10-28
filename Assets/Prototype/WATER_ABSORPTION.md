# Desert water absorption

The PillarCourse Ground object now has WaterAbsorbingSurface. This marker consumes released liquid touching its solid collider. A marker on a parent also applies to child colliders. Add it only to surfaces intended to destroy water; avoid placing it on a common parent containing rocks or pillars.

Held water is protected. Freezing is unchanged; ice itself is not consumed. Melted water is loose liquid and disappears if it subsequently reaches marked ground. Unmarked surfaces retain reusable puddles. Existing sleeping puddles are not polled for newly added markers during Play Mode: configure the marker before playing, or re-grab/release that water.

Implementation: WaterStream uses existing nearest sweep/support hits to identify absorbing surfaces; no extra physics casts were added. Consumed indices are compacted in one pass after each simulation step that removes water, preserving liquid order and held selection indices. Removal frees the liquid drawing budget and the renderer reflects the updated list on its next update. This does not optimize the separate ice anchoring system.

Playtest after stopping Play Mode and letting Unity import: drop water onto Ground and confirm it vanishes; brush Ground while holding water and confirm it survives; release that same water and confirm it disappears. Drop water on an unmarked pillar/rock or ice and confirm it remains reusable. Hold a second mass while older water falls into the desert, then sculpt/freeze/melt the held mass to check selection stability.

Aquarius > Validate Water Absorption exercises these rules in an isolated temporary scene. It also checks parent markers, disabled markers, selection compaction and the final rendered particle count. Automated tests do not replace the visual course playtest.

## Verification

Isolated Unity 6000.3.26f1 compilation and absorption validation passed. The test caught motionless held drops retaining their sleeping flag on release; release now wakes selected water to recheck support.

PASS: marked parent/child collider absorption, ordinary puddle retention/pickup, disabled marker, held contact protection, absorption on release, selection remapping while another mass is held, freeze/melt after compaction, melted-water absorption, empty rendered particle buffer.

Normal Unity Editor visual playtesting remains required.
