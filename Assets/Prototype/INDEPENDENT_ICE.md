# Independent ice structures

Open Aquarius > Open Freeze Traversal Test, then Play. Use the existing sandbox; this milestone does not add the canyon level.

Controls:
- LMB + mouse/WASD: draw or re-bend water, as before.
- F while holding LMB and bending: freeze the entire currently held mass into a new independent ice structure. Other loose water stays liquid.
- Release LMB, aim at a visible ice structure within 6 m, and press F: melt only that structure. Ordinary geometry blocks the targeting ray.
- Earlier ice remains solid when drawing or freezing another mass. There is no structure-count limit.
- R resets the player and releases held liquid, preserving all constructions. Backspace clears all liquid and ice.

Acceptance playtest:
1. Draw a gentle ramp from the channel onto the first ledge and freeze it.
2. Walk onto that ramp. Draw another stream from a reachable part of the channel, or re-bend loose water you already placed nearby.
3. Extend the second stream into a bridge, with a small overlap at the join, and freeze it. Check that the first ramp stays exactly as it was.
4. Walk across the join between the two constructions.
5. From a safe platform, aim at the second structure and press F. Only it should disappear; its saved water returns at the same location, falls, and can be bent again.
6. Repeat several times, then use R and Backspace to verify their different reset behaviors.

Implementation:
- WaterStream owns live liquid and a list of committed structures. Each FrozenWaterStructure owns a saved mass and its own WaterIceSurface mesh/collider.
- Freezing transfers held droplets out of the live simulation. It copies the existing ice shape settings; it does not rebuild previous meshes.
- Structures can overlap and do not need simulated supports. Liquid collides with committed ice and slides along contact surfaces.
- Melting disables that collider immediately and restores every saved droplet, with zero initial velocity and gravity/contact behavior. Sleeping liquid resting on ice is awakened when a structure melts.
- The 4096 live-droplet budget limits new source emission, not the number of structures. Freezing frees live slots. Melting can temporarily exceed that budget; all returned water remains visible and reusable, while new drawing waits for available slots.
- Frozen structures persist during the current Play session. This milestone does not implement save/load across Play sessions or scene changes.

Limitations: overlapping constructions remain separate colliders rather than a welded mesh; tight turns, large height mismatches at joins, very thin sections, and self-intersections still need playtesting. All existing movement settings remain unchanged. The pillar level, hazards, structural breakage, and ledge climbing are later milestones.

Validation: Aquarius > Validate Freeze Traversal exercises independent commits, held-only freezing, preservation of old ice and loose water, overlapping capsule traversal, targeted-melt occlusion, in-place particle restoration, re-bending, budget reuse, and reset. Results are written to FreezeValidation.txt at the project root.

Rollback checkpoint: the .checkpoints folder contains the saved project immediately before this milestone. It includes Assets (and .meta files), Packages, and ProjectSettings, with a checksum manifest. Unity caches and unsaved editor state are excluded. Restore only after stopping Play Mode/closing Unity; restoring also needs removal of files introduced after that snapshot, rather than simply overwriting older files. Ask to revert to before independent ice and we can inspect the intervening changes and restore that checkpoint.

Validation result: PASS in isolated Unity 6000.3.26f1. The final combined run passed both liquid regression and independent construction suites; exit code 0. Verified held-only commits, unchanged earlier ice/loose water, standing on A while committing B, walking their join, occluded target selection, targeted melting, exact in-place restoration, falling/re-bending/re-freezing, reusable source budget, lossless over-budget melting, and reset. Interactive input and the feel of user-sculpted joins still need a Play Mode test.

