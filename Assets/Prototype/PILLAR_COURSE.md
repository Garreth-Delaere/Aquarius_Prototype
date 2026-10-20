# Pillar course — representative traversal greybox

Open Aquarius > Open Pillar Course and press Play. Let Unity import first; use Assets > Refresh if the menu is missing. This is a separate scene: the earlier sandboxes are retained.

Four wooden pillar decks sit at heights 8, 10, 9, and 12 metres above the river. Each has a shallow blue water basin. The final deck is green and marked by a gold beacon. A rock blocks the direct approach to the second pillar. There are no prebuilt bridges or invisible path constraints: find a way around it, or try another construction strategy.

Controls are unchanged: WASD/mouse, Space jump, LMB sculpt, F while bending commits the held mass, release LMB and aim at ice + F melts that structure. R returns to the start. Backspace clears all water and ice.

Blue basins are unlimited sources with finite drawn masses. You can draw from any basin within the existing 6 m reach. The river itself is deliberately inaccessible from the high decks. Use your structures to reach a position from which a basin or nearby loose water is accessible. When extending a route, keep your next supply in mind.

Dropping to river height resets the player to the starting pillar and releases held water. Frozen constructions remain, so a failed attempt does not erase the route. There are no automatic intermediate checkpoints; R and river resets return to the original start. Destination arrival displays a completion message and leaves the route available to inspect/rebuild.

Current movement is retained: 1 m jump, 55-degree climb limit, air steering, and 20 cm stepping. The river reset is the only new hazard. Creatures, ice melting/breaking hazards, wind, ledge climbing, and detailed art are not implemented.

Playtest sequence:
1. Look down toward the blue basin on the start deck. Draw water and begin an ice ramp/walkway.
2. Freeze it, walk onto it, and extend with another mass while keeping water within reach.
3. Work around the rock and toward the higher pillar. Test joining ice pieces at manageable heights.
4. Continue toward the lower pillar, then the beacon pillar. Other routes are allowed.
5. Fall deliberately: check that the player resets and the ice remains.
6. Reach the green destination deck. Try a different route afterward.

Questions to record before SCAMPER:
- Where did you make a satisfying decision, rather than fight the controls?
- Did the rock prompt a useful curve? Was going straight still the obvious solution elsewhere?
- Did you feel stranded by source distance? Was preparing another water mass interesting or tedious?
- Were joins readable and comfortable to walk across?
- Did a failed crossing invite a different plan, or only repetition?
- Which action felt specific to this game, and which felt like a familiar elemental-power trope?

Automated validation is available under Aquarius > Validate Pillar Course. It checks scene references, current movement, accessible basin selection, obstruction/alternate clearance, beacon sightline, construction from the starting basin, river reset without lost ice, and distinct goal arrival. It does not claim to solve or assess the fun of the whole player-created course; those are playtest tasks.

A saved project checkpoint before this milestone is in .checkpoints, named before-pillar-course with its checksum manifest.

Level checks passed in isolated Unity 6000.3.26f1 for source access, obstacle/alternate clearance, beacon visibility, basin construction, river reset retaining ice, movement settings, distinct goal arrival, and preservation of the earlier sandbox. Whole-course expressive traversal and visual feel remain user playtests.

