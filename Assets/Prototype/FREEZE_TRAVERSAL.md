# Freeze traversal sandbox

The current freeze/melt interaction is documented in INDEPENDENT_ICE.md.

Open Aquarius > Open Freeze Traversal Test. Hold LMB to sculpt; F commits the held water as a new structure. To melt one, release LMB, aim at that structure within 6 m, and press F. Previous structures remain solid. Backspace clears the sandbox; R resets only the player.

The scene contains a ramp ledge, a bridge gap, and an elevated descent platform. Use overlapping ice ends to connect constructions. The current controller has a 1 m jump, a 55-degree slope limit, midair steering, and 0.20 m automatic stepping.

Each construction uses the existing smoothed, beveled ice surface and a matching static non-convex MeshCollider. Geometry is generated on freeze, and collision is removed immediately on melt. Simple ramps and bridges remain the intended test shapes; tight folds and awkward collider seams are known limitations.

Historical note: earlier versions used one global F toggle for all water. That behavior is replaced by held-mass freezing and aimed-structure melting. The earlier saved version is available in .checkpoints.
