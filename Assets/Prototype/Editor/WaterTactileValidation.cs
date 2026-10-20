using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class WaterTactileValidation
    {
        public static void RunAll() { RunDepth(); RunMotion(); PillarCourseValidation.RunAll(); MovementValidation.Run(); }
        [MenuItem("Aquarius/Validate Water Depth")]
        public static void RunDepth()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current scene first."); return; }
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../WaterDepthValidation.txt"));
            try
            {
                var liquidObject = new GameObject("Depth validation water");
                var water = liquidObject.AddComponent<WaterStream>(); water.dropletsPerDraw = 24; water.Initialize();
                var player = new GameObject("Depth input"); var input = player.AddComponent<StreamManipulator>();
                var camera = new GameObject("Depth camera").AddComponent<Camera>(); input.water = water; input.view = camera;
                camera.transform.SetPositionAndRotation(new Vector3(1000,5,1000), Quaternion.Euler(30,70,25));
                input.SetHoldDistance(3.5f);
                water.BeginSource(input.ManipulationTarget);
                Vector3 before = input.ManipulationTarget; input.ApplyScrollDepth(1);
                Require(Mathf.Abs(input.CurrentHoldDistance - 4.1f) < 0.001f, "Forward scroll should increase held distance.");
                Require(Vector3.Distance(input.ManipulationTarget - before, camera.transform.forward * 0.6f) < 0.001f, "Depth displacement must follow pitched/yawed camera forward.");
                input.ApplyScrollDepth(-2); Require(Mathf.Abs(input.CurrentHoldDistance - 2.9f) < 0.001f, "Backward scroll should pull closer.");
                for(int i = 0; i < 120; i++) input.ApplyScrollDepth(0);
                Require(Mathf.Abs(input.CurrentHoldDistance - 2.9f) < 0.001f, "Adjusted distance must persist without scrolling.");
                camera.transform.SetPositionAndRotation(new Vector3(1002,7,1003), Quaternion.Euler(-35,-50,0));
                Require(Vector3.Distance(input.ManipulationTarget, camera.transform.position + camera.transform.forward * 2.9f) < 0.001f, "Depth must remain camera-relative during walking/look changes.");
                input.ApplyScrollDepth(10000); Require(Mathf.Approximately(input.CurrentHoldDistance, input.maximumManipulationDistance), "Depth should clamp at its maximum.");
                input.ApplyScrollDepth(-10000); Require(Mathf.Approximately(input.CurrentHoldDistance, input.minimumManipulationDistance), "Depth should clamp at its minimum.");
                water.Release(); float idle = input.CurrentHoldDistance; input.ApplyScrollDepth(2);
                Require(Mathf.Approximately(input.CurrentHoldDistance, idle), "Idle scroll must not alter the held distance.");
                water.BeginSource(camera.transform.position + camera.transform.forward * 3); input.SetHoldDistance(3); input.ApplyScrollDepth(1);
                water.SetTarget(input.ManipulationTarget, 0.5f);
                for(int i = 0; i < 270; i++) water.SimulateStep(1f / 90f);
                Require(water.FreezeHeld(), "Depth-adjusted water must still freeze.");
                var ice = water.FrozenStructures[0]; Vector3 centre = ice.Surface.Collision.bounds.center;
                input.ApplyScrollDepth(2);
                Require(ice.IsSolid && ice.Surface.Collision.bounds.center == centre, "Wheel input must not move committed ice.");
                string success = "PASS: forward/backward depth, camera-axis displacement, persistence, camera movement, min/max clamps, idle/frozen isolation, freezing adjusted water.";
                File.WriteAllText(report, success); Debug.Log(success);
            }
            catch(Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if(SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(temporary, true);
            }
        }
        [MenuItem("Aquarius/Validate Water Motion")]
        public static void RunMotion()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current scene first."); return; }
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../WaterMotionValidation.txt"));
            try
            {
                var water = CreateWater(new Vector3(1000,3,1000), 0);
                Vector3 initialTarget = water.SmoothedTarget;
                var before = Positions(water);
                Vector3 requested = initialTarget + Vector3.right * 3;
                water.SetTarget(requested, 0);
                water.SimulateStep(1f / 90f);
                Require(Vector3.Distance(water.SmoothedTarget, requested) > 2.5f, "The sculpting guide must lag rather than jump to the requested target.");
                Require(MaxMovement(before, water) < 0.12f, "Water must not snap after a target step.");
                Step(water, 26);
                Require(Vector3.Distance(water.SmoothedTarget, requested) < 1.5f, "Water must visibly catch up instead of remaining stuck.");
                Step(water, 270);
                Require(Vector3.Distance(water.SmoothedTarget, requested) < 0.02f && MaxMovement(before, water) > 0.5f, "Target lag must settle while physical water follows it.");
                UnityEngine.Object.DestroyImmediate(water.gameObject);
                float slow = SwipeMovement(0), fast = SwipeMovement(1);
                Require(fast > slow + 0.03f, "Fast swipes must still create a stronger physical response: slow=" + slow + " fast=" + fast);
                float control = FrozenDifference(0, out float controlParticles);
                float physical = FrozenDifference(0.22f, out float movedParticles);
                Require(physical > 0.06f && physical > control + 0.025f && movedParticles > 0.08f,
                    "Still-input waves must meaningfully survive freeze smoothing: mesh=" + physical + " baseline=" + control + " particles=" + movedParticles);
                string success = "PASS: target lag/no snap, catch-up and settling, stronger fast swipes, coherent still-input motion survives actual freezing, exact physical snapshots, committed ice remains still. Frozen-surface RMS change over one second=" + physical.ToString("F3") + " m; baseline=" + control.ToString("F3") + " m; particle RMS=" + movedParticles.ToString("F3") + " m.";
                File.WriteAllText(report, success); Debug.Log(success);
            }
            catch(Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if(SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(temporary, true);
            }
        }
        static WaterStream CreateWater(Vector3 start, float amplitude)
        {
            var obj = new GameObject("Motion validation water"); var water = obj.AddComponent<WaterStream>();
            water.wobbleAmplitude = amplitude; water.Initialize(); water.BeginSource(start);
            water.SetTarget(start + Vector3.forward * 6, 0); Step(water, 540); return water;
        }
        static float SwipeMovement(float swipe)
        {
            var water = CreateWater(new Vector3(1100 + swipe * 30,3,1000), 0);
            Vector3[] before = Positions(water);
            water.SetTarget(water.SmoothedTarget + Vector3.right * 3, swipe); Step(water, 22);
            float movement = MaxMovement(before, water); UnityEngine.Object.DestroyImmediate(water.gameObject); return movement;
        }
        static float FrozenDifference(float amplitude, out float particleRms)
        {
            var water = CreateWater(new Vector3(1200 + amplitude * 100,3,1000), amplitude);
            Vector3[] first = Positions(water);
            var preview = new GameObject("First instant freeze").AddComponent<WaterIceSurface>();
            preview.CopySettingsFrom(water.GetComponent<WaterIceSurface>());
            Require(preview.Build(water, out string reason), "First freeze failed: " + reason);
            Vector3[] meshA = preview.Collision.sharedMesh.vertices;
            preview.Clear(); UnityEngine.Object.DestroyImmediate(preview.gameObject);
            Step(water, 90); // One second, with no target/camera/mouse changes.
            Vector3[] second = Positions(water); float sum = 0;
            for(int i = 0; i < first.Length; i++) sum += (first[i] - second[i]).sqrMagnitude;
            particleRms = Mathf.Sqrt(sum / first.Length);
            Require(water.FreezeHeld(), "Second instant freeze failed: " + water.FreezeMessage);
            var frozen = water.FrozenStructures[0];
            for(int i = 0; i < second.Length; i++) Require(frozen.Snapshot[i].position == second[i], "Freezing must store actual undulating droplet positions.");
            Vector3[] meshB = frozen.Surface.Collision.sharedMesh.vertices;
            // Nearest-vertex distance tolerates changes in section count while measuring real mesh shape.
            sum = 0; int samples = 0;
            for(int i = 0; i < meshA.Length; i += 8)
            {
                float nearest = float.PositiveInfinity;
                for(int j = 0; j < meshB.Length; j++) nearest = Mathf.Min(nearest, (meshA[i] - meshB[j]).sqrMagnitude);
                sum += nearest; samples++;
            }
            Step(water, 180);
            var stationary = frozen.Surface.Collision.sharedMesh.vertices;
            for(int i = 0; i < stationary.Length; i++) Require(stationary[i] == meshB[i], "Already-frozen geometry must not keep wobbling.");
            float result = Mathf.Sqrt(sum / samples); UnityEngine.Object.DestroyImmediate(water.gameObject); return result;
        }
        static Vector3[] Positions(WaterStream water)
        {
            var values = new Vector3[water.Droplets.Count];
            for(int i = 0; i < values.Length; i++) values[i] = water.Droplets[i].position;
            return values;
        }
        static float MaxMovement(Vector3[] before, WaterStream water)
        {
            float value = 0; for(int i = 0; i < before.Length; i++) value = Mathf.Max(value, Vector3.Distance(before[i], water.Droplets[i].position)); return value;
        }
        static void Step(WaterStream water, int count) { for(int i = 0; i < count; i++) water.SimulateStep(1f / 90f); }
        static void Require(bool condition, string message) { if(!condition) throw new InvalidOperationException(message); }
    }
}


