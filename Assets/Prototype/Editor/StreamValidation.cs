using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    // Tests the real movement/contact code in a temporary isolated area of an additive scene.
    // Runs once per editor session, or explicitly from the menu. Never edits the user's scene.
    [InitializeOnLoad]
    public static class StreamValidation
    {
        const string SessionKey = "Aquarius.StreamValidation.v1";
        static StreamValidation() { EditorApplication.delayCall += ValidateOnce; }
        static void ValidateOnce()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true); Run();
        }
        [MenuItem("Aquarius/Validate Channel Water")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before running channel validation."); return; }
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current untitled scene before running channel validation."); return; }
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            string resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ChannelWaterValidation.txt"));
            try
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Temporary validation floor";
                floor.transform.position = new Vector3(10000, -0.1f, 10000);
                floor.transform.localScale = new Vector3(40, 0.2f, 40);
                Physics.SyncTransforms();
                var obj = new GameObject("Temporary validation water");
                var water = obj.AddComponent<WaterStream>();
                water.dropletsPerDraw = 24; water.emissionRate = 60; water.dropletBudget = 256;
                water.Initialize();
                Vector3 origin = new Vector3(10000, 0.25f, 10000);
                water.BeginSource(origin);
                water.SetTarget(origin + new Vector3(3, 3, 0), 0.6f);
                Step(water, 270);
                Require(water.Droplets.Count == 24, "A source draw must respect its finite batch size.");
                float heldHeight = AverageHeight(water);
                Require(heldHeight > 0.65f, "Holding should lift water from the source.");
                water.Release(); Step(water, 90);
                Require(AverageHeight(water) < heldHeight - 0.2f, "Released water must fall instead of retaining its guide shape.");
                Step(water, 540);
                Require(water.Droplets.Count == 24, "Released water must persist without particle expiry.");
                for (int i = 0; i < water.Droplets.Count; i++)
                    Require(water.Droplets[i].grounded && water.Droplets[i].position.y > 0 && water.Droplets[i].position.y < 0.15f, "Drops should settle above the floor.");
                Vector3 fallen = water.Droplets[0].position;
                Require(Vector3.Distance(fallen, origin) > 0.8f, "Dropped water must remain away from its source.");
                Require(water.TryPick(new Ray(fallen + Vector3.up * 2, Vector3.down), 3, out int picked, out float distance), "Settled water must remain targetable.");
                Vector3 before = water.Droplets[picked].position;
                Require(water.BeginPickup(picked), "Settled water should be selectable.");
                Require(water.Droplets[picked].position == before, "Picking up must not teleport water.");
                int count = water.Droplets.Count;
                water.SetTarget(before + new Vector3(0, 2, 2), 0.7f); Step(water, 270);
                Require(water.Droplets.Count == count, "Re-bending existing water must not create water.");
                bool lifted = false;
                for (int i = 0; i < count; i++) lifted |= water.Droplets[i].position.y > 0.5f;
                Require(lifted, "Fallen water should lift again when bent.");
                water.Release(); Step(water, 540);
                Vector3 retained = water.Droplets[0].position;
                water.BeginSource(origin); Step(water, 90);
                Require(water.Droplets.Count == 48, "The source should replenish after previous water was removed.");
                Require(Vector3.Distance(water.Droplets[0].position, retained) < 0.01f, "A fresh draw must not relocate earlier puddles.");
                water.Release(); water.ResetWater();
                Require(water.Droplets.Count == 0 && !water.IsHeld, "Explicit reset should clear water and release the grab.");
                const string success = "PASS: finite source draw, held lift, gravity on release, ground contact, persistence, picking, no teleport, re-bending without new water, replenishing source, retained old puddles, reset.";
                File.WriteAllText(resultPath, DateTime.UtcNow.ToString("O") + "\n" + success);
                Debug.Log(success);
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, "FAIL: " + exception);
                Debug.LogException(exception);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(temporary, true);
            }
        }
        static void Step(WaterStream water, int count) { for (int i = 0; i < count; i++) water.SimulateStep(1f / 90f); }
        static float AverageHeight(WaterStream water)
        {
            float total = 0; for (int i = 0; i < water.Droplets.Count; i++) total += water.Droplets[i].position.y;
            return total / Mathf.Max(1, water.Droplets.Count);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}

