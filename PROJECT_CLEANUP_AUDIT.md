# Project cleanup audit — 28 October 2026

First pass removed only Unity template welcome assets: Assets/Readme.asset, Assets/TutorialInfo and accompanying metadata (14 files total). All removed GUIDs were checked against remaining Assets, ProjectSettings and Packages before removal; no external references were found. A verified recoverable ZIP is in .checkpoints/before-template-cleanup-20261028-092327.zip. Root README.md and all prototype documentation remain.

## Obsolete for current gameplay, but still connected

- Assets/Prototype/Scripts/WaterBody.cs, WaterManipulator.cs and WaterMesh.cs implement the superseded connected blob.
- Assets/Scenes/LiquidSculptingTest.unity still references these three scripts and Assets/Prototype/Materials/Liquid.mat.
- Assets/Prototype/Editor/LiquidTestSceneBuilder.cs also references the three types and automatically recreates its scene if missing. Archive the whole group together if retiring this iteration; deleting only scripts would break the old scene and builder.
- Assets/Scenes/SampleScene.unity is disabled in the global build list. It is a cleanup candidate after removing its disabled entry and confirming it is not the open scene. Its profile is NOT redundant: Assets/Settings/SampleSceneProfile.asset is referenced by both PC_RPAsset and Mobile_RPAsset.

## Keep for now

- PillarCourse and its active movement, stream, source, renderer, ice and course scripts.
- MovementTest, ChannelWaterTest and FreezeTraversalTest: focused diagnostic scenes. Their builders have dependencies on each other; remove only after reviewing those dependencies. They are not enabled in the current global build list.
- Editor validation scripts: functional regression checks. They are editor tools, not ongoing player-build gameplay work.
- All render pipeline assets: Mobile_RPAsset is referenced by QualitySettings; Mobile_Renderer by that pipeline. The PC pipeline and volume settings have user changes which this cleanup preserved.
- InputSystem_Actions.inputactions: input setup.
- Prototype materials: several are loaded by editor builders using paths as well as scene GUIDs; lack of a scene reference alone does not prove they are unused.
- Milestone markdown and validation reports: development/assignment evidence. Optional later organization, no gameplay performance benefit from removal.

## Generated/local files

Library, Temp, Logs, IDE project files and editor preferences are generated/local data, not obsolete gameplay assets. They are already ignored by Git. Cleaning caches is not an ice-runtime optimization; Unity regenerates them. .checkpoints holds recoverable experiments and backups; do not discard casually. Builds are submission artifacts, not source dependencies.

## Performance implication and verification boundary

Removing unused welcome assets reduces project clutter. It does not change ice collision, anchoring, bobbing, mesh detail or the number of paths. The runtime performance audit remains separate.

Verification: complete GUID reference scan before removal; backup archive integrity checked; remaining C# contains no references to the removed Readme type or TutorialInfo path. No Unity compilation or visual playtest was performed for this asset-only cleanup. Let Unity import, confirm there are no new Console errors, and open the pillar course as a smoke check.
