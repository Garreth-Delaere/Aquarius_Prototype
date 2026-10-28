using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class WaterAbsorptionValidation
    {
        [MenuItem("Aquarius/Validate Water Absorption")]
        public static void Run()
        {
            var previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            string report = Path.GetFullPath("WaterAbsorptionValidation.txt");
            try
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.position = new Vector3(2000, -0.5f, 2000);
                ground.transform.localScale = new Vector3(30, 1, 30);
                var parent = new GameObject("Absorbing ground parent");
                ground.transform.SetParent(parent.transform, true);
                var marker = parent.AddComponent<WaterAbsorbingSurface>();
                Physics.SyncTransforms();
                var water = new GameObject("Absorption test water").AddComponent<WaterStream>();
                water.wobbleAmplitude = 0; water.restingRadius = 0;
                var drops = (List<WaterStream.Droplet>)water.Droplets;
                Add(drops, new Vector3(2000, 2, 2000), 0);
                Step(water, 180);
                Require(drops.Count == 0, "Released water must disappear on marked child collider.");

                marker.enabled = false;
                Add(drops, new Vector3(2000, 2, 2000), 0); Step(water, 180);
                Require(drops.Count == 24 && drops[0].grounded, "Unmarked/disabled ground must retain reusable puddles.");
                Require(water.BeginPickup(0), "Ordinary puddles must remain pickable.");
                marker.enabled = true; water.SetTarget(new Vector3(2000, 0.08f, 2000), 0);
                Step(water, 90);
                Require(drops.Count == 24 && water.SelectedCount == 24, "Held water must survive brushing marked ground.");
                water.Release(); Step(water, 180);
                Require(drops.Count == 0, "Protected water must be consumed after release.");

                Add(drops, new Vector3(2000, 0.1f, 2000), 1);
                Add(drops, new Vector3(2000, 6, 2000), 2);
                Require(water.BeginPickup(24), "Upper independent mass must be held.");
                water.SetTarget(new Vector3(2000, 6, 2000), 0); Step(water, 180);
                Require(drops.Count == 24 && water.SelectedCount == 24, "Removing earlier loose drops must preserve held selection.");
                Require(water.FreezeHeld() && water.FrozenCount == 1 && drops.Count == 0, "Remapped selection must still freeze correctly.");
                Require(water.Melt(water.FrozenStructures[0]), "Frozen water must melt.");
                Step(water, 240);
                Require(drops.Count == 0, "Melted water must also absorb after falling to ground.");
                water.GetComponent<WaterDropletRenderer>().Show(water);
                Require(water.GetComponent<ParticleSystem>().particleCount == 0, "Consumed droplets must disappear from rendering.");
                string pass = "PASS: marked parent/child collider absorption, ordinary puddle retention/pickup, disabled marker, held contact protection, absorption on release, selection remapping while another mass is held, freeze/melt after compaction, melted-water absorption, empty rendered particle buffer.";
                File.WriteAllText(report, pass); Debug.Log(pass);
            }
            catch (Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
        static void Add(List<WaterStream.Droplet> drops, Vector3 p, int shape)
        { for (int i = 0; i < 24; i++) drops.Add(new WaterStream.Droplet { position = p + Vector3.right * (i * 0.01f), shapeId = shape }); }
        static void Step(WaterStream water, int steps) { for (int i = 0; i < steps; i++) water.SimulateStep(1f / 90f); }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
