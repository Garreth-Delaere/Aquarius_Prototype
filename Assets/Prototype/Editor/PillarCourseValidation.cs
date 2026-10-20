using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class PillarCourseValidation
    {
        public static void RunAll() { FreezeValidation.RunAll(); Run(); }

        [MenuItem("Aquarius/Validate Pillar Course")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current scene first."); return; }
            Scene opened = default; FirstPersonMotor player = null; UnityEngine.Object clonedActions = null;
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../PillarCourseValidation.txt"));
            try
            {
                string sandboxPath = Path.Combine(Application.dataPath, "Scenes/FreezeTraversalTest.unity");
                string original = File.ReadAllText(sandboxPath);
                PillarCourseBuilder.Build();
                Require(File.ReadAllText(sandboxPath) == original, "Building the course must preserve the existing sandbox.");
                opened = EditorSceneManager.OpenScene(PillarCourseBuilder.ScenePath, Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
                PillarCourse course = null; StreamManipulator interaction = null;
                foreach (var root in opened.GetRootGameObjects())
                {
                    if (root.TryGetComponent<PillarCourse>(out var found)) course = found;
                    if (root.TryGetComponent<FirstPersonMotor>(out var motor)) { player = motor; interaction = root.GetComponent<StreamManipulator>(); }
                }
                Require(course != null && player != null && interaction != null && course.water != null, "Scene references must be assigned.");
                Require(interaction.additionalSources.Length == 3, "Four pillars need four connected water supplies.");
                Require(Mathf.Approximately(player.jumpHeight, 1) && Mathf.Approximately(player.slopeLimit, 55) && Mathf.Approximately(player.airAcceleration, 18), "Current movement must be preserved.");
                Vector3[] tops = { new Vector3(0,8,0), new Vector3(5,10,9), new Vector3(-2,9,17), new Vector3(4,12,26) };
                Physics.SyncTransforms();
                for (int i = 0; i < 4; i++)
                {
                    WaterSource expected = i == 0 ? interaction.source : interaction.additionalSources[i - 1];
                    Vector3 eye = tops[i] + new Vector3(0,1.6f,-0.8f);
                    Ray ray = new Ray(eye, (expected.transform.position - eye).normalized);
                    Require(interaction.TryFindSource(ray, out var picked, out float distance) && picked == expected && distance < 6, "Basin " + (i + 1) + " must be reachable and unobstructed from its deck.");
                }
                Require(Physics.Linecast(tops[0] + Vector3.up, tops[1] + Vector3.up, out RaycastHit blocked) && blocked.collider.name == "Route obstruction", "The direct first approach must be obstructed.");
                Vector3 bend = new Vector3(-0.8f,11.2f,6.9f);
                Require(!Physics.Linecast(tops[0] + Vector3.up * 0.3f, bend), "An alternate route around the left of the rock must remain open.");
                Require(!Physics.Linecast(bend, tops[1] + Vector3.up * 0.3f), "The curved approach should have clearance to the second pillar.");
                GameObject beacon = null; foreach(var root in opened.GetRootGameObjects()) if(root.name == "Destination beacon") beacon = root;
                Require(beacon != null, "The destination needs a beacon.");
                bool sightBlocked = Physics.Linecast(player.transform.position + Vector3.up * 1.6f, beacon.transform.position, out RaycastHit sight);
                Require(!sightBlocked || sight.collider.gameObject == beacon, "The destination beacon must be visible from the start.");
                // Cache the real spawn via the same initialization used on entering Play Mode.
                typeof(FirstPersonMotor).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                FieldInfo actionField = typeof(FirstPersonMotor).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic);
                clonedActions = (UnityEngine.Object)actionField.GetValue(player);
                Vector3 spawn = player.transform.position;
                var water = course.water; water.Initialize();
                water.BeginSource(interaction.source.transform.position); water.SetTarget(new Vector3(-2,9,5), 0.6f);
                for(int i = 0; i < 450; i++) water.SimulateStep(1f / 90f);
                Require(water.FreezeHeld(), "Water from the starting basin must produce a structure.");
                var structure = water.FrozenStructures[0];
                var controller = player.GetComponent<CharacterController>();
                controller.enabled = false; player.transform.position = new Vector3(0,0.2f,5); controller.enabled = true;
                course.CheckProgress();
                Require(course.Falls == 1 && Vector3.Distance(player.transform.position, spawn) < 0.01f && structure.IsSolid, "River contact must reset the player while preserving ice.");
                controller.enabled = false; player.transform.position = tops[1] + Vector3.up * 0.05f; controller.enabled = true; course.CheckProgress();
                Require(!course.ReachedDestination, "Intermediate pillars must not complete the course.");
                controller.enabled = false; player.transform.position = tops[3] + Vector3.up * 0.05f; controller.enabled = true; course.CheckProgress();
                Require(course.ReachedDestination, "Reaching the goal pillar should register completion.");
                string success = "PASS: preserved sandbox/movement, four accessible basins, blocked direct route, clear alternate bend, visible destination, starting-basin construction, river reset with persistent ice, distinct goal arrival.";
                File.WriteAllText(report, success); Debug.Log(success);
            }
            catch(Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if(player != null && clonedActions != null)
                {
                    typeof(FirstPersonMotor).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, null);
                    UnityEngine.Object.DestroyImmediate(clonedActions);
                }
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if(opened.IsValid() && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(opened, true);
            }
        }
        static void Require(bool condition, string message) { if(!condition) throw new InvalidOperationException(message); }
    }
}


