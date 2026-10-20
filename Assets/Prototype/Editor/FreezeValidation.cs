using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class FreezeValidation
    {
        public static void RunAll() { StreamValidation.Run(); Run(); }

        [MenuItem("Aquarius/Validate Freeze Traversal")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current scene first."); return; }
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../FreezeValidation.txt"));
            try
            {
                Vector3 origin = new Vector3(1000, 0, 1000);
                Box(origin + new Vector3(0, -0.25f, -0.5f), new Vector3(12, 0.5f, 3));
                Box(origin + new Vector3(0, 0.7f, 8), new Vector3(12, 1.4f, 4.4f)); Physics.SyncTransforms();
                var obj = new GameObject("Validation water"); var water = obj.AddComponent<WaterStream>();
                water.dropletsPerDraw = 80; water.dropletBudget = 256; water.Initialize();
                Require(!water.FreezeHeld(), "An idle F press must not commit or melt arbitrary water.");
                Draw(water, origin + new Vector3(-4, 0.25f, 0), origin + new Vector3(-4, 0.25f, 0));
                water.Release(); Step(water, 360); int looseCount = water.Droplets.Count;
                Vector3 loosePosition = water.Droplets[0].position;
                Draw(water, origin + new Vector3(0, -0.08f, 0), origin + new Vector3(0, 1.45f, 6.3f));
                Require(water.FreezeHeld() && water.FrozenCount == 1, "Ramp A should commit independently.");
                var a = water.FrozenStructures[0]; var meshA = a.Surface.Collision.sharedMesh;
                Vector3[] savedVertices = meshA.vertices;
                Require(water.Droplets.Count == looseCount && water.Droplets[0].position == loosePosition, "Freezing held water must preserve unrelated loose water.");
                Require(!water.FreezeHeld() && a.IsSolid, "An idle freeze must not melt existing ice.");
                // A freely moving tail changes the ramp's starting position; inspect its actual span.
                Bounds rampBounds = a.Surface.Collision.bounds;
                for (float z = rampBounds.min.z - origin.z + 0.3f; z <= rampBounds.max.z - origin.z - 0.3f; z += 0.2f)
                    Require(a.Surface.Collision.Raycast(new Ray(origin + new Vector3(0, 4, z), Vector3.down), out RaycastHit hit, 5) && Vector3.Angle(hit.normal, Vector3.up) < 55,
                        "Ramp A has a hole or bad walking normal at z=" + z);
                var actor = new GameObject("Walker"); var controller = actor.AddComponent<CharacterController>();
                controller.height = 1.75f; controller.radius = 0.3f; controller.center = Vector3.up * 0.875f;
                controller.slopeLimit = 55; controller.stepOffset = 0.2f; controller.skinWidth = 0.025f; controller.minMoveDistance = 0;
                float entryZ = rampBounds.min.z - origin.z + 0.4f;
                Require(a.Surface.Collision.Raycast(new Ray(origin + new Vector3(0,4,entryZ),Vector3.down),out var entry,5), "A must provide a supported entry point.");
                actor.transform.position = entry.point + Vector3.up * 0.03f; Physics.SyncTransforms();
                for (int i = 0; i < 65; i++) controller.Move(new Vector3(0, -4, 2) / 60f);
                Require(actor.transform.position.z > origin.z + 2.5f && actor.transform.position.y > 0.5f, "Walker should stand on ramp A.");
                water.ignoredPlayer = actor.transform;
                Require(a.Surface.Collision.Raycast(new Ray(origin + new Vector3(0, 4, 4.3f), Vector3.down), out RaycastHit join, 5), "A must provide a joining surface.");
                Draw(water, join.point - Vector3.up * 0.08f, origin + new Vector3(0, 1.5f, 9.5f));
                Require(water.FreezeHeld() && water.FrozenCount == 2, "Bridge B should freeze while the player stands on A.");
                var b = water.FrozenStructures[1];
                Require(a.Surface.Collision.sharedMesh == meshA && a.IsSolid, "Creating B must not rebuild or disable A.");
                for (int i = 0; i < savedVertices.Length; i++) Require(meshA.vertices[i] == savedVertices[i], "A's shape changed when B was added.");
                for (int i = 0; i < 360; i++)
                {
                    controller.Move(new Vector3(0, -4, 2) / 60f);
                    if (actor.transform.position.z > origin.z + 8.5f) break;
                }
                Require(actor.transform.position.z > origin.z + 8 && actor.transform.position.y > 1.3f, "Walker should cross the A/B overlap onto the landing. position=" + (actor.transform.position - origin) + " B=" + b.Surface.Collision.bounds);
                // Test the actual interaction target, including occlusion by ordinary geometry.
                var interaction = actor.AddComponent<StreamManipulator>(); interaction.water = water;
                var finder = typeof(StreamManipulator).GetMethod("FindIce", BindingFlags.Instance | BindingFlags.NonPublic);
                Ray aim = new Ray(origin + new Vector3(0, 5, 8), Vector3.down);
                var aimed = (FrozenWaterStructure)finder.Invoke(interaction, new object[] { aim });
                string hitInfo = ""; foreach (var h in Physics.RaycastAll(aim, 6)) hitInfo += h.collider.name + "@" + h.distance + ";";
                Require(aimed == b, "Aim should select B independently. target=" + (aimed != null ? aimed.name : "none") + " hits=" + hitInfo + " bounds=" + b.Surface.Collision.bounds);
                var wall = Box(origin + new Vector3(0, 3, 8), new Vector3(2, 0.2f, 2)); Physics.SyncTransforms();
                Require(finder.Invoke(interaction, new object[] { aim }) == null, "Targeted melting must not reach through a wall.");
                UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms();
                Draw(water, origin + new Vector3(4, 2, 3), origin + new Vector3(4, 2, 5));
                Require(water.FreezeHeld() && water.FrozenCount == 3, "A third structure should commit without replacing A or B.");
                var c = water.FrozenStructures[2];
                int beforeMelt = water.Droplets.Count, restored = b.Snapshot.Count;
                Vector3[] savedB = new Vector3[restored]; for (int i = 0; i < restored; i++) savedB[i] = b.Snapshot[i].position;
                Require(b.Melt() && !b.IsSolid && a.IsSolid && c.IsSolid && water.FrozenCount == 2, "Melting B must immediately remove only B's collision.");
                Require(water.Droplets.Count == beforeMelt + restored, "Melt must restore the exact particle count.");
                for (int i = 0; i < restored; i++) Require(water.Droplets[beforeMelt + i].position == savedB[i], "Melt must restore water in place.");
                Step(water, 90); bool fell = false;
                for (int i = 0; i < restored; i++) fell |= water.Droplets[beforeMelt + i].position.y < savedB[i].y - 0.03f;
                Require(fell, "Melted water should resume falling.");
                // Move to a safe part of the landing before shaping another mass.
                controller.enabled = false; actor.transform.position = origin + new Vector3(-5, 1.45f, 8); controller.enabled = true; Physics.SyncTransforms();
                Require(water.BeginPickup(beforeMelt), "Melted water must be re-bendable.");
                int beforeRefreeze = water.Droplets.Count;
                water.SetTarget(origin + new Vector3(0, 2.5f, 8), 0.6f); Step(water, 180);
                Require(water.FreezeHeld() && water.Droplets.Count < beforeRefreeze, "Re-freezing must transfer existing liquid without duplication. " + water.FreezeMessage);
                // Reset, then accumulate enough committed masses to exceed the emission budget on melt.
                controller.enabled = false; water.ignoredPlayer = null; water.ResetWater();
                for (int i = 0; i < 4; i++)
                {
                    Draw(water, origin + new Vector3(-4 + i * 2, 2, 3), origin + new Vector3(-4 + i * 2, 2, 4));
                    Require(water.FreezeHeld(), "Committing ice should free the liquid-emission budget.");
                }
                Require(water.FrozenCount == 4 && water.Droplets.Count == 0, "Independent ice must not occupy live liquid slots.");
                var masses = new FrozenWaterStructure[water.FrozenCount]; for (int i = 0; i < masses.Length; i++) masses[i] = water.FrozenStructures[i];
                foreach (var mass in masses) Require(water.Melt(mass), "Stored structures should melt independently.");
                Require(water.Droplets.Count == 320 && water.BudgetFull, "Melting over the source budget must preserve every saved droplet.");
                Require(obj.GetComponent<ParticleSystem>().main.maxParticles >= 320, "Returned liquid must remain visible above the source budget.");
                water.ResetWater(); Require(water.FrozenCount == 0 && water.Droplets.Count == 0, "Reset should clear all liquid and committed ice.");
                string success = "PASS: independent A/B/C, held-only freeze, preserved loose water, ramp traversal, committing while standing on old ice, walkable overlapping joins, target selection/occlusion, targeted melt, exact in-place restoration, falling/re-bending/re-freezing, reusable liquid budget, lossless above-budget melt, reset.";
                File.WriteAllText(report, success); Debug.Log(success);
            }
            catch (Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(temporary, true);
            }
        }
        static void Draw(WaterStream water, Vector3 start, Vector3 target) { water.BeginSource(start); water.SetTarget(target, 0.7f); Step(water, 450); }
        static void Step(WaterStream water, int count) { for (int i = 0; i < count; i++) water.SimulateStep(1f / 90f); }
        static GameObject Box(Vector3 position, Vector3 size) { var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.position = position; obj.transform.localScale = size; return obj; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}





