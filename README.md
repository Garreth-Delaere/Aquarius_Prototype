# Aquarius Prototype

Current feel iteration: slow guidance gathers a short water shape; quick gestures leave a stretched, curved stream that can be frozen during motion. See `Assets/Prototype/FOLLOWING_STREAM_TAIL.md` for tuning and the focused playtest.
A first-person Unity prototype exploring water sculpting and freezing as traversal tools.

## Open the project

1. Install **Unity 6000.3.26f1** through Unity Hub.
2. Clone this repository and add its root folder as a project in Unity Hub.
3. Open it and let Unity restore packages and import assets.
4. Select **Aquarius > Open Pillar Course**, or open `Assets/Scenes/PillarCourse.unity`.
5. Press Play.

The project uses the existing Universal Render Pipeline and Unity Input System. No extra fluid-simulation package is required.

## Controls

- WASD: walk
- Mouse: look / guide held water
- Space: jump (with midair steering)
- Hold LMB over a source or released water: bend water
- Mouse wheel while bending: push/pull along camera forward
- F while bending: freeze the held stream
- Aim at ice and press F: melt that structure
- Release LMB: drop liquid water
- R: reset the player
- Backspace: clear water and ice
- Escape: release the cursor

## Current playtest

The earlier thin-particle stream and ice geometry are preserved. The stream tail follows the player's guidance instead of staying anchored to its source. Independent frozen structures, selective melting and released-water reuse remain supported.

See `Assets/Prototype/FOLLOWING_STREAM_TAIL.md` for the focused playtest and `Assets/Prototype/README.md` for earlier milestones. The finite-volume/fire experiment is banked locally and is not part of this version.

## Version control

Commit `Assets` (including `.meta` files), `Packages`, and `ProjectSettings`. Unity regenerates `Library`, `Temp`, and other ignored folders on each computer. Keep assets and their `.meta` files together when moving or renaming them.

Local checkpoint archives and personal editor settings are excluded. After each small, tested change, save a commit with a short description so earlier iterations remain easy to recover.
